using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using static Unordinal.Editor.External.UnordinalApi;
using static Unordinal.Editor.External.AnalyticsApi;
using UnityEngine.UIElements;
using Microsoft.Extensions.Logging;
using Unity;
using System.Threading;
using Unordinal.Editor.External;
using Unordinal.Editor.Services;
using Unordinal.Editor.UI;
using Unordinal.Editor.Utils;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.UIElements;

namespace Unordinal.Editor
{
    public partial class GuiHosting : ConfigurableWindow, IActiveBuildTargetChanged
    {
        #region Fields

        // Identifiers for dashboard
        private PluginData pluginData;

        // ProgressText is not visible in new GUI design,
        // kept since this might change in future.
#pragma warning disable 0414 // This removes warning "0414" regarding this filed from console.
        private string progressText = null;
#pragma warning restore 0414

        // variables to handle auth process
        private string deviceCode;

        // Cancellation token sources.
        private CancellationTokenSource loginFlow;
        private CancellationTokenSource deploymentFlow;

        private ITokenStorage tokenStorage;
        private IUserInfoHolder userInfoHolder;
        private PluginDataFactory pluginDataFactory;
        private Auth0Client auth0Client;
        private RefreshTokenHttpMessageHandler refreshHandler;
        private UnordinalApi unordinalApi;
        private AnalyticsApi analyticsApi;
        private ServerBundler bundler;
        private ClientBundler clientBundler;
        private Archiver archiver;
        private FileUploader fileUploader;
        private ILogger<GuiHosting> logger;
        private AzureRegionPinger regionalDeploymentService;
        private PortFinder portFinder;

        // Debug stuff
        private bool addDebugButtons = false;

        // Deployment
        private DeploymentStep activeStep;
        bool nonAwaitedTaskFailed;
        string failMessage = string.Empty;
        BuildTargetGroup originalGroup;
        BuildTarget originalTarget;
        string serverOutputPath = null;
        string clientOutputPath = null;
        Guid deploymentGuid = Guid.Empty;
        bool playmodeRequested = false; // When true, unity will enter play mode upon canceling deployment.
        DeploymentWillCancelPopup warningPopup; // This popup is used to warn user that deployment will be canceled if they are to enter play mode.

        // Analytics
        string userID => userInfoHolder?.UserInfo.sub;

        #endregion

        #region Initialization

        [InjectionMethod]
        public void Initialize(
            ITokenStorage tokenStorage,
            IUserInfoHolder userInfoHolder,
            PluginDataFactory pluginDataFactory,
            Auth0Client auth0Client,
            RefreshTokenHttpMessageHandler refreshHandler,
            UnordinalApi unordinalApi,
            AnalyticsApi analyticsApi,
            ServerBundler bundler,
            ClientBundler clientBundler,
            Archiver archiver,
            FileUploader fileUploader,
            ILogger<GuiHosting> logger,
            AzureRegionPinger regionalDeploymentService,
            PortFinder portFinder)
        {
            logger.LogDebug("Inside injection method");
            this.tokenStorage = tokenStorage;
            this.userInfoHolder = userInfoHolder;
            this.pluginDataFactory = pluginDataFactory;
            this.auth0Client = auth0Client;
            this.refreshHandler = refreshHandler;
            this.unordinalApi = unordinalApi;
            this.analyticsApi = analyticsApi;
            this.bundler = bundler;
            this.clientBundler = clientBundler;
            this.archiver = archiver;
            this.fileUploader = fileUploader;
            this.logger = logger;
            this.regionalDeploymentService = regionalDeploymentService;
            this.portFinder = portFinder;

            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        protected override void DoCreateGUI()
        {
            var commonStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unordinal.hosting/Editor/Stylesheets/Extensions/common.uss");

            // Each editor window contains a root VisualElement object
            rootVisualElement.styleSheets.Add(commonStyleSheet);

            if (EditorGUIUtility.isProSkin)
            {
                // Add dark theme on top of common.
                var darkStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unordinal.hosting/Editor/Stylesheets/Extensions/dark.uss");
                rootVisualElement.styleSheets.Add(darkStyleSheet);
            }
            else
            {
                // Add light theme on top of common.
                var lightStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unordinal.hosting/Editor/Stylesheets/Extensions/light.uss");
                rootVisualElement.styleSheets.Add(lightStyleSheet);
            }

            rootVisualElement.AddToClassList("root-container");
            rootVisualElement.style.minHeight = minSize.y;

            var scrollView = new ScrollView();
            scrollView.style.minHeight = minSize.y;
            rootVisualElement.Add(scrollView);

            var pageContainer = CreatePageContainer();
            scrollView.Add(pageContainer);

            var signInPage = CreateSignInPage();
            AddPage(DeploymentPages.SignIn, signInPage);

            var signInActivePage = CreateSignInActivePage();
            AddPage(DeploymentPages.SignInActive, signInActivePage);

            var signInSuccessPage = CreateSignInSuccessfulPage();
            AddPage(DeploymentPages.SignInSuccess, signInSuccessPage);

            var linuxBuildSupportPage = CreateAddLinuxPage();
            AddPage(DeploymentPages.LinuxBuildSupportRequired, linuxBuildSupportPage);

            var restartRequiredPage = CreateRestartunityRequired();
            AddPage(DeploymentPages.RestartUnityRequired, restartRequiredPage);

            var startPage = CreateStartPage();
            PortsChanged += OnPortsChanged;
            AddPage(DeploymentPages.Start, startPage);

            var waitPage = CreateWaitPage();
            AddPage(DeploymentPages.Wait, waitPage);

            var deployingPage = CreateDeployingPage();
            AddPage(DeploymentPages.Deploying, deployingPage);

            var deployFinishedPage = CreateDeployFinishedPage();
            AddPage(DeploymentPages.Finished, deployFinishedPage);

            var deployingErrorPage = CreateDeployingErrorPage();
            AddPage(DeploymentPages.Error, deployingErrorPage);

            ShowPage(ActivePage);

#if DEBUG
            if (addDebugButtons)
            {
                var debugContainer = CreateDebugContainer();
                AnalyticsAction = (() =>
                {
                    Task.Run(async () => await analyticsApi.SendAnalyticsGA4("DebugUser", "DebugEvent"));
                });
                scrollView.Add(debugContainer);
            }
#endif

            warningPopup = new DeploymentWillCancelPopup(rootVisualElement, (() =>
            {
                deploymentFlow?.Cancel();
                playmodeRequested = true;
                warningPopup.ClosePopupInPlugin();
            }));

            // Evaluate the version once the GUI has been created.
            EvaluateVersion();
        }

        #endregion

        #region Lifecycle callbacks

        protected override void Enable()
        {
            pluginData = pluginDataFactory.LoadPluginData();
            EvaluateSignIn();
        }

        protected override void OnFocused()
        {
            EvaluateSignIn();
        }

        private async void Update()
        {
            CheckUnityBuildSupport();
            await RunPortFinding();
            TryContinueDeploy();
            HandleProgressBarFeedback();
            warningPopup?.TryAnimateToGetAttention();
        }

        private void CheckUnityBuildSupport()
        {
            if (ActivePage == DeploymentPages.Start)
            {
                var missingLinuxBuildSupport = !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
                if (missingLinuxBuildSupport)
                {
                    ShowPage(DeploymentPages.LinuxBuildSupportRequired);
                }
            }
            if (ActivePage == DeploymentPages.LinuxBuildSupportRequired && !addDebugButtons)
            {
                var hasLinuxBuildSupport = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
                if (hasLinuxBuildSupport)
                {
                    // User has added Linux build support, but not yet restarted Unity.
                    ShowPage(DeploymentPages.RestartUnityRequired);
                }
            }
        }

        private void PlayModeStateChanged(PlayModeStateChange obj)
        {
            var playPressedAndDeploying = 
                obj == PlayModeStateChange.ExitingEditMode && 
                (ActivePage == DeploymentPages.Wait || ActivePage == DeploymentPages.Deploying);
            if (playPressedAndDeploying)
            {
                // The plugin will prevent the user from starting game (pressing play)
                // Because of this we realy need to get the attention of the user.
                // One of these steps is that the Hosting plugin must be visible.
                // Focus() makes sure the plugin is visible.
                this.Focus();

                warningPopup.ShowWarningPopup();
                EditorApplication.isPlaying = false;
            }
        }

        #endregion

        private void TryContinueDeploy()
        {
            // When deploying we build a server (Linux) and a client (Win/Mac/Linux/any).
            // This requires us to swap platform when building server, client and when swapping back to original.
            // And when changing platform, local values are lost.
            // Hence we use EditorPrefs instead.
            var shouldDeploy = EditorPrefs.GetBool(UnordinalKeys.deployInNextUpdateKey, false);
            if (shouldDeploy)
            {
                // On this update, we know that a server (and maybe client) have been built are ready to be deployed.

                // Clean up so we only enter once per deploy.
                EditorPrefs.SetBool(UnordinalKeys.deployInNextUpdateKey, false);

                OnDeploy(playWithFriendsToggled);
            }
        }

        private void EvaluateSignIn()
        {
            if (tokenStorage.HasValidToken)
            {
                InitializeUserInfo().ContinueWith(userFetched =>
                {
                    if (!userFetched.Result || ActivePage != DeploymentPages.SignIn)
                    {
                        return;
                    }
                    ShowPage(DeploymentPages.Start);
                },
                TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        #region Event handlers

        private void OnPageChanged(DeploymentPages oldPage, DeploymentPages newPage)
        {
            if (oldPage == DeploymentPages.Deploying && newPage == DeploymentPages.Error)
            {
                Task.Delay(10).ContinueWith(task =>
                {
                    ErrorProgress = Progress;
                },
                TaskScheduler.FromCurrentSynchronizationContext());
            }

            if (newPage == DeploymentPages.Start)
            {
                StartFindingPorts();
            }

            // Analytics, send event for the loaded page.
            Task.Run(async () => await analyticsApi.SendAnalyticsGA4(userID, GetEventName(newPage), isPageViewEvent: true));
        }

        private async void OnSignIn(Action<string> urlAction)
        {
            try
            {
                loginFlow = new CancellationTokenSource();
                var signInURL = await GetSignInURL(loginFlow.Token);
                urlAction(signInURL);
                ShowPage(DeploymentPages.SignInActive);
                await WaitForBrowserAuthentication(loginFlow.Token);
                userInfoHolder.Save();
                ShowPage(DeploymentPages.SignInSuccess);
                await Task.Delay(1000); // Show the success page for this long.
                ShowPage(DeploymentPages.Start);
            }
            catch (Exception e)
            {
                if(!loginFlow.IsCancellationRequested)
                {
                    logger.LogError(e, "Error while processing login");
                }
                ShowPage(DeploymentPages.SignIn);
            }
            finally
            {
                loginFlow.Dispose();
            }
        }

        private async void OnCancelSignIn()
        {
            // Analytics
            await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Cancel_Sign_In);

            loginFlow?.Cancel();
            ShowPage(DeploymentPages.SignIn);
        }

        private async void OnPortsChanged(PortChange change)
        {
            // Analytics 
            if (change == PortChange.PortAdded)
            {
                await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Add_Port);
            }
            else if (change == PortChange.PortRemoved)
            {
                await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Remove_Port);
            }
        }

        private async Task DoDeploy(bool withClient)
        {
            // Analytics
            await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Deploy);

            try
            {
                ResetDeployment();
                var result = await Deploy(deploymentFlow.Token, withClient);
                if (result != null)
                {
                    // Set the values that is shown on the result page.
                    IpResult = result.ip;
                    PlayWithFriendsResult = result.downloadUrl;
                    DeployPort = result.Ports;
                    // BuildPortContainer();
                    ShowPage(DeploymentPages.Finished);
                    DeployData deployData = new DeployData(IpResult, DeployPort);
                    deployData.Save();
                }
                else
                {
                    ShowPage(DeploymentPages.Start);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                // Analytics
                await analyticsApi.SendAnalyticsGA4(userID, GetEventFailedName(activeStep), details: e.Message);

                ShortErrorMessage = e.Message;
                LongErrorMessage = e.StackTrace;
                IpResult = string.Empty;
                ShowPage(DeploymentPages.Error);
            }
            finally
            {
                deploymentFlow.Dispose(); // Clean up token source.
                if(playmodeRequested)
                {
                    await Task.Delay(50); // Wait for correct page to show.
                    EditorApplication.isPlaying = true;
                }
            }
        }

        private async void OnDeploy(bool withClient)
        {
            await DoDeploy(withClient);
        }

        private void ResetDeployment()
        {
            ShowPage(DeploymentPages.Deploying);
            Progress = stepToProgressDictionary[DeploymentStep.ShowStarted];
            IpResult = string.Empty;
            nonAwaitedTaskFailed = false;
            failMessage = "Something went wrong.";
            ResetError();
            deploymentFlow = new CancellationTokenSource(); // "Reset" the cancellation token source.
        }

        private async void OnCancelDeploy()
        {
            // Analytics
            await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Cancel_Deploy);

            // Cancel the deployment.
            if (deploymentFlow != null)
            {
                deploymentFlow?.Cancel();

                // Wait until cancelation was successful.
                while (ActivePage == DeploymentPages.Deploying)
                {
                    await Task.Delay(5);
                }
            }
            else
            {
                ShowPage(DeploymentPages.Start);
            }
        }

        private async void OnDashboard()
        {
            Help.BrowseURL("https://app.unordinal.com/");

            // Analytics
            await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Dashboard);
        }

        private async void OnHome()
        {
            // Analytics
            await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Home);

            ShowPage(DeploymentPages.Start);
        }

        private async Task<string> GetSignInURL(CancellationToken cancelToken)
        {
            var response = await auth0Client.getDeviceCode(cancelToken);
            deviceCode = response.device_code;
            VerificationCode = response.user_code;
            return response.verification_uri_complete;
        }

        private async Task WaitForBrowserAuthentication(CancellationToken cancelToken)
        {
            logger.LogDebug("Authorize loop start");
            _ = await auth0Client.getToken(deviceCode, cancelToken);
            logger.LogDebug("Authorize loop finished, reading user info");
            await InitializeUserInfo();
        }

        private async Task<bool> InitializeUserInfo()
        {
            var response = await auth0Client.isTokenValid();
            if (response.valid)
            {
                userInfoHolder.UserInfo = response.user ?? new UserInfo();
            }
            return response.valid;
        }

        private async Task SetActiveStep(DeploymentStep step)
        {
            activeStep = step;

            // Analytics
            await analyticsApi.SendAnalyticsGA4(userID, GetEventName(step));
        }

        private async void PreDeploy()
        {
            ShowPage(DeploymentPages.Wait);
            await Task.Delay(50); // Make time for page to change.

            // Check if we have internet access,
            // Once we have, it will automatically continue.
            await ShowNoInternetPopupUntilWeHaveInternet();

            // Make one call to backend to be sure we dont get any errors
            // (Sometimes it has been the case that, server/client is building for several minutes
            //  just to figure out that one need to sign in again..)
            // Better detect this as early as possible.
            var guid = await RegisterProject();
            if(guid == Guid.Empty)
            {
                // Something went wrong when registering project.
                return;
            }

            BuildServerClient(playWithFriendsToggled);
        }

        private async Task<Guid> RegisterProject()
        {
            deploymentFlow = new CancellationTokenSource();

            try
            {
                progressText = "Registering project...";
                if (pluginData.ProjectID == default)
                {
                    // first time project deployment

                    // Add project
                    await SetActiveStep(DeploymentStep.AddProject);
                    pluginData.ProjectID = await unordinalApi.addProject(pluginData.ProjectName);
                    pluginDataFactory.SavePluginData(pluginData);
                    refreshHandler.ForceRefresh();
                }

                // Building docker image
                // (Actually: Just writes down data to HostingRuns DB, and returns guid (key) for item in DB)
                await SetActiveStep(DeploymentStep.BuildingDockerImage);
                deploymentGuid = Guid.Empty;
                progressText = "Building docker image...";
                deploymentGuid = await unordinalApi.startProcess(pluginData.ProjectID, Ports, deploymentFlow.Token);

                if (deploymentGuid == Guid.Empty) { throw (new Exception("Ops, something went wrong.")); } // Shows error view.

                // UI, might reloaded and stuff which might cause the value to clear.
                // Save it for loading later.
                EditorPrefs.SetString(UnordinalKeys.deploymentGuidKey, deploymentGuid.ToString());
            }
            catch(Exception e)
            {
                ShortErrorMessage = "Something went wrong, try closing the plugin and opening it again.";
                LongErrorMessage = "Most likely, the plugin is no longer detecting that you are signed in.";
                ShowPage(DeploymentPages.Error);
                deploymentGuid = Guid.Empty;

                var invalidOperation = e.Message == "Operation is not valid due to the current state of the object.";
                if (invalidOperation || e.Message.ToLower().Contains("forbidden"))
                {
                    if (!addDebugButtons)
                    {
                        // This is a customer

                        // User should sign in again.
                        // Clear sign in information.
                        UnordinalKeys.ClearSignInInformation();
                        tokenStorage.Clear();

                        // Show sign in page, so user can sign in again.
                        ShowPage(DeploymentPages.SignIn);
                    }
                    else
                    {
                        // This is a developer

                        // We dont want to clear the sign in information, since we might want to debug it.
                        ShortErrorMessage = "You are having trouble with your Auth0 token, debug it." +
                            "Or clear your plugin data by using the red debug button and restart plugin.";
                        LongErrorMessage = e.StackTrace;
                    }
                }
            }
            finally
            {
                deploymentFlow.Dispose();
            }

            return deploymentGuid;
        }

        private void BuildServerClient(bool withClient)
        {
            bool shouldDeploy = true;
            try
            {
                if (withClient)
                // Client Scope - BEWARE we expect the user's BuildTarget to be unmodified here, hence building before server which switches
                // Client is being built before server, otherwise we have sometimes seens some errors while building client.
                {
                    // Build client
                    HandleScenesInBuildSettings();
                    clientOutputPath = clientBundler.Bundle(originalGroup, originalTarget);
                    ResetScenesInBuildSettings();
                }

                // Server scope
                // Server is built second since client is already on correct platform
                // (IMPORTANT NOTE: We have seen some errors when client is built second while linux is the temporary target platform,
                //                  hence build client first.)
                {
                    // Build server
                    HandleScenesInBuildSettings();
                    serverOutputPath = bundler.Bundle(BundleArchitectures.Intel64);
                    ResetScenesInBuildSettings();
                }
            }
            catch (Exception e)
            {
                shouldDeploy = false;
                
                ShortErrorMessage = e.Message;
                LongErrorMessage = e.StackTrace;
                ShowPage(DeploymentPages.Error);
            }
            finally
            {
                // Server/client has finished building, swap back to original target.

                // When swapping build target, properties/fields loses ther values.
                // Hence we need to store in EditorPrefs.
                EditorPrefs.SetBool(UnordinalKeys.shouldDoADeployKey, shouldDeploy);

                if (EditorUserBuildSettings.activeBuildTarget == originalTarget)
                {
                    // We are already on original target.
                    EvaluateReadyForDeploy();
                }
                else
                {
                    // Swap back target to original target.
                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(originalGroup, originalTarget);
                }
            }
        }

        public int callbackOrder { get { return 0; } } // TODO: Check if this can be removed.
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            EvaluateReadyForDeploy();
        }

        private void EvaluateReadyForDeploy()
        {
            var shouldDoADeploy = EditorPrefs.GetBool(UnordinalKeys.shouldDoADeployKey, false);
            if (shouldDoADeploy)
            {
                // Clean up, so that it wont do a deploy next time platform is changed.
                EditorPrefs.SetBool(UnordinalKeys.shouldDoADeployKey, false); 

                if (shouldDoADeploy)
                {
                    EditorPrefs.SetBool(UnordinalKeys.deployInNextUpdateKey, shouldDoADeploy);
                }
            }
        }

        private class DeploymentResult
        {
            public string ip;
            public string downloadUrl;
            public List<DeployPort> Ports { get; set; }
        }
        
        private async Task<DeploymentResult> Deploy(CancellationToken token, bool withClient)
        {
            // UI, might have reloaded and stuff which might have cleared the value.
            // It was saved when obtained, now load it again.
            deploymentGuid = Guid.Parse(EditorPrefs.GetString(UnordinalKeys.deploymentGuidKey));

            LoadEstimatedTimes();

            // Attempt at making the deployment page visible before UI is locked during server build.
            // This is to get the correct progress visible while building the server.
            await Task.Delay(50, token);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            string serverTarFilename = Path.Combine(Constants.buildFolder, "server.tar.gz");

            // Server scope
            {
                // Zipping server
                await SetActiveStep(DeploymentStep.ZippingServer);
                progressText = "Zipping server...";
                Func<Task> zippingFunc = (async () =>
                    await archiver.CreateTarGZAsync(serverTarFilename, serverOutputPath.Replace("\\", "/"), token));
                await InterpolateProgressBarBetweenSteps(zippingFunc, DeploymentStep.ZippingServer);
            }

            if (deploymentFlow.IsCancellationRequested) { return null; }

            var clientZipFileName = Path.Combine(Constants.buildFolder, "client.zip");

            if (withClient)
            // Client scope
            {
                // Zipping client
                await SetActiveStep(DeploymentStep.ZippingClient);
                progressText = "Zipping client...";
                //TODO: .zip would probably be friendlier to most end users
                Func<Task> zippingFunc = (async () =>
                    await archiver.CreateZipAsync(clientZipFileName, clientOutputPath.Replace("\\", "/"), token));
                await InterpolateProgressBarBetweenSteps(zippingFunc, DeploymentStep.ZippingClient);
            }

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Server Upload
            {
                // Get upload URL
                await SetActiveStep(DeploymentStep.GetServerUploadURL);
                progressText = "GetUploadUrl...";
                var uploadUrl = string.Empty;
                Func<Task> getUploadUrlFunc = (async () => uploadUrl = await unordinalApi.getGameServerUploadUrl(deploymentGuid, token));
                await InterpolateProgressBarBetweenSteps(getUploadUrlFunc, DeploymentStep.GetServerUploadURL);

                if (deploymentFlow.IsCancellationRequested) { return null; }

                // Upload file
                await SetActiveStep(DeploymentStep.UploadServer);
                progressText = "Upload file!...";
                Func<Task> uploadFileFunc = (async () => await fileUploader.UploadFile(uploadUrl, serverTarFilename, token));
                await InterpolateProgressBarBetweenSteps(uploadFileFunc, DeploymentStep.UploadServer);
            }

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Client upload
            GameClientUrlsResponse uploadClientResponse = null;
            if (withClient)
            {
                // Get upload url of client
                await SetActiveStep(DeploymentStep.GetClientUploadURL);
                progressText = "GetUploadUrl...";
                var uploadClientUrl = string.Empty;
                Func<Task> getUploadClientUrlFunc = (async () => uploadClientResponse = await unordinalApi.getGameClientUrls(deploymentGuid, token));
                await InterpolateProgressBarBetweenSteps(getUploadClientUrlFunc, DeploymentStep.GetClientUploadURL);

                if (deploymentFlow.IsCancellationRequested) { return null; }

                // Ready to upload client file
                await SetActiveStep(DeploymentStep.UploadClient);
                progressText = "Upload file!...";
                Func<Task> uploadGameClientFileFunc = (async () => await fileUploader.UploadFile(uploadClientResponse.uploadUrl, clientZipFileName, token));
                await InterpolateProgressBarBetweenSteps(uploadGameClientFileFunc, DeploymentStep.UploadClient);
            }

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Build image
            await SetActiveStep(DeploymentStep.BuildImage);
            progressText = "Build image!...";
            await unordinalApi.buildImage(deploymentGuid, token);
            Func<Task> checkBuildStatusFunc = (async () =>
                await TaskHelpers.RetryWithCondition(
                    () => unordinalApi.checkBuildStatus(deploymentGuid),
                    (result) => result.Status == "Succeeded",
                    token,
                    maxRetries: int.MaxValue,
                    delay: 5000));
            await InterpolateProgressBarBetweenSteps(checkBuildStatusFunc, DeploymentStep.BuildImage);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Find suitable region
            await SetActiveStep(DeploymentStep.PingRegions);
            progressText = "Evaluate region!...";
            Dictionary<string, long> regions = new Dictionary<string, long>(); // If failing to evaluate good region, it will still be able to deploy.
            Func<Task> regionFunc = (async () =>
                regions = await regionalDeploymentService.Ping(regionalDeploymentService.ListOfRegions(), token));
            await InterpolateProgressBarBetweenSteps(regionFunc, DeploymentStep.PingRegions);
#if DEBUG
            if (addDebugButtons && regions.Count == 0)
            {
                Debug.Log("Failed to evaluate good region.");
            }
#endif

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Deploy game
            await SetActiveStep(DeploymentStep.Deploying);
            progressText = "Deploy game!...";
#pragma warning disable CS4014 // Disable warning regarding async task not being awaited.

            // Don't await this, since we are pulling for the status.
            // The REST-api has an await in it in order to be able to return the result.
            // Hence, we continue without awaiting and obtain the result later.
            unordinalApi.deploy(deploymentGuid, regions, token).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // The task failed.

                    // Since we are not awaiting this async task, exceptions are not catched.
                    // We therefore need a way to indicate that it failed.
                    // This bool is therefore used to mark failure and used later.
                    nonAwaitedTaskFailed = true;
                    failMessage = "Deployment step failed: " + t.Exception.InnerException.Message;
                }
            });

#pragma warning restore CS4014 
            DeployStatusMessage deployStatusMessage = default;
            Func<Task> deploymentFunc = (async () =>
                deployStatusMessage = await TaskHelpers.RetryWithConditionAndBreakCondition(
                    () => unordinalApi.checkDeployStatus(deploymentGuid),
                    result => !string.IsNullOrEmpty(result.Ip),
                    result => result.Status == "Error",
                    token,
                    maxRetries: int.MaxValue,
                    delay: 5000));
            await InterpolateProgressBarBetweenSteps(deploymentFunc, DeploymentStep.Deploying);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Deployment-process finished
            await SetActiveStep(DeploymentStep.ShowFinished);
            Func<Task> delayFunc = (async () => await Task.Delay(500, token)); // Go from 94% to 100 % over 500ms
            await InterpolateProgressBarBetweenSteps(delayFunc, DeploymentStep.ShowFinished);

            await Task.Delay(500, token); // Show 100% for this long (ms)

            progressText = "Publish okay!";

            return new DeploymentResult()
            {
                ip = deployStatusMessage.Ip,
                downloadUrl = uploadClientResponse != null ? uploadClientResponse.downloadUrl : string.Empty,
                Ports = deployStatusMessage.Ports
            };
        }

        #endregion
    }
}
