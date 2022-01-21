using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using static Unordinal.Hosting.UnordinalApi;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Microsoft.Extensions.Logging;
using Unity;
using System.Threading;

namespace Unordinal.Hosting
{
    public partial class GuiHosting : ConfigurableWindow<GuiHosting>
    {
        #region Fields

        // Identifiers for dashboard
        private PluginData pluginData;

        private const string VerificationCodeSubtitle =
            "Ensure that the verification code matches the one in your browser.";

        // ProgressText is not visible in new GUI design,
        // kept since this might change in future.
#pragma warning disable 0414 // This removes warning "0414" regarding this filed from console.
        private string progressText = null;
#pragma warning restore 0414

        // Ports
        private List<Port> ports = new List<Port>();
        private int amountOfPorts = 1;

        // Should sign in page be shown on plugin start.
        // This will change to false, if token is valid,
        // resulting in the start-page being shown instead.
        private bool showSignIn = true; 

        // Page variables.
        private Dictionary<DeploymentPage, VisualElement> pages; // Contains all the available pages.
        private DeploymentPage activePage = DeploymentPage.Start;
        private DeploymentPage oldPage = DeploymentPage.Start;

        // Visual elements that gets updated
        private VisualElement firstRemoveButton;
        private TextField resultMessage;
        private TextField shortErrorMessage;
        private ScrollView detailedErrorScrollView;
        private TextField longErrorMessage;
        private Label signInActiveSubtitle;
        private Box progressForeground;
        private Label percentLabel;
        private Box errorProgressForeground;
        private Label errorPercentLabel;

        // Deployment variables
        private float progress = 0.0f;

        // Widths
        private int progressBarWidth = 360;
        private int bigButtonWidth = 280;
        private int copyResultButtonWidth = 50;

        // variables to handle auth process
        private Label verificationCodeLabel;
        private string deviceCode = null;

        // Enabling this will show debug buttons,
        // these buttons can then be used to 
        // toggle between different pages.
        private bool addDebugButtons = false;

        // Cancellation token sources.
        private CancellationTokenSource loginFlow = new CancellationTokenSource();
        private CancellationTokenSource deploymentFlow = new CancellationTokenSource();

        private ITokenStorage tokenStorage;
        private IUserInfoHolder userInfoHolder;
        private Auth0Client auth0Client;
        private RefreshTokenHttpMessageHandler refreshHandler;
        private UnordinalApi unordinalApi;
        private ServerBundler bundler;
        private TarGzArchiver archiver;
        private FileUploader fileUploader;
        private ILogger<GuiHosting> logger;
        private RegionalDeploymentService regionalDeploymentService;

        #endregion

        #region Initialization

        [MenuItem("Tools/Hosting")]
        public new static GuiHosting Initialize()
        {
            var result = ConfigurableWindow<GuiHosting>.Initialize();
            result.minSize = new Vector2(445, 50);
            result.titleContent = new GUIContent("Unordinal Hosting");
            return result;
        }

        [InjectionMethod]
        public void Initialize(
            ITokenStorage tokenStorage,
            IUserInfoHolder userInfoHolder,
            Auth0Client auth0Client,
            RefreshTokenHttpMessageHandler refreshHandler,
            UnordinalApi unordinalApi,
            ServerBundler bundler,
            TarGzArchiver archiver,
            FileUploader fileUploader,
            ILogger<GuiHosting> logger,
            RegionalDeploymentService regionalDeploymentService)
        {
            logger.LogDebug("Inside injection method");
            this.tokenStorage = tokenStorage;
            this.userInfoHolder = userInfoHolder;
            this.auth0Client = auth0Client;
            this.refreshHandler = refreshHandler;
            this.unordinalApi = unordinalApi;
            this.bundler = bundler;
            this.archiver = archiver;
            this.fileUploader = fileUploader;
            this.logger = logger;
            this.regionalDeploymentService = regionalDeploymentService;
        }

        #endregion

        protected override void AfterEnabled()
        {
            pluginData = PluginData.LoadPluginData();
            EvaluateSignIn();
            LoadPorts();
            GenerateGUI();
        }

        private void GenerateGUI()
        {
            var pluginStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unordinal.hosting/Editor Default Resources/GuiHosting.uss");
            rootVisualElement.styleSheets.Add(pluginStyleSheet);

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            root.AddToClassList("root-container");

            var scrollView = new ScrollView();

            var pluginContainer = new VisualElement();
            pluginContainer.AddToClassList("plugin-container");
            pluginContainer.style.minHeight = 550;

            root.Add(scrollView);
            scrollView.Add(pluginContainer);

            // Containers, each one holds a different page "step" in the deployment.
            var signInPage = new VisualElement();
            var signInActivePage = new VisualElement();
            var signInSuccessPage = new VisualElement();
            var startPage = new VisualElement();
            var deployingPage = new VisualElement();
            var deployFinishedPage = new VisualElement();
            var deployingErrorPage = new VisualElement();

            // Add class to make each one of them center in window.
            signInPage.AddToClassList("plugin-container");
            signInActivePage.AddToClassList("plugin-container");
            signInSuccessPage.AddToClassList("plugin-container");
            startPage.AddToClassList("plugin-container");
            deployingPage.AddToClassList("plugin-container");
            deployFinishedPage.AddToClassList("plugin-container");
            deployingErrorPage.AddToClassList("plugin-container");

            // Generate the different pages.
            CreateSignIn(signInPage);
            CreateSignInActive(signInActivePage);
            CreateSignInSucessfull(signInSuccessPage);
            CreateStart(startPage);
            CreateDeploying(deployingPage);
            CreateDeployFinished(deployFinishedPage);
            CreateDeployingError(deployingErrorPage);

            // Add pages to dictionary
            pages = new Dictionary<DeploymentPage, VisualElement>();
            pages.Add(DeploymentPage.SignIn, signInPage);
            pages.Add(DeploymentPage.SignInActive, signInActivePage);
            pages.Add(DeploymentPage.SignInSucess, signInSuccessPage);
            pages.Add(DeploymentPage.Start, startPage);
            pages.Add(DeploymentPage.Deploying, deployingPage);
            pages.Add(DeploymentPage.Finished, deployFinishedPage);
            pages.Add(DeploymentPage.Error, deployingErrorPage);

            // Add them to hierarchy so they can be shown.
            pluginContainer.Add(signInPage);
            pluginContainer.Add(signInActivePage);
            pluginContainer.Add(signInSuccessPage);
            pluginContainer.Add(startPage);
            pluginContainer.Add(deployingPage);
            pluginContainer.Add(deployFinishedPage);
            pluginContainer.Add(deployingErrorPage);

            // Store visual elements that will change.
            signInActiveSubtitle = pages[DeploymentPage.SignInActive].Q<Label>("subtitle");
            progressForeground = pages[DeploymentPage.Deploying].Q<Box>("progress");
            percentLabel = pages[DeploymentPage.Deploying].Q<Label>("percent-label");
            errorProgressForeground = pages[DeploymentPage.Error].Q<Box>("progress-error");
            errorPercentLabel = pages[DeploymentPage.Error].Q<Label>("percent-error-label");

            HideAllPages(); // Hide all pages, the active one will turn visible in OnGUI

            if (showSignIn)
            {
                activePage = DeploymentPage.SignIn;
            }

            ShowPage(activePage);

            if (addDebugButtons)
            {
                AddDebugButtons(root);
            }

            showSignIn = false;
        }

        private void OnGUI()
        {
            if (activePage != oldPage)
            {
                // Change page.

                HideAllPages();
                ShowPage(activePage);
                oldPage = activePage;
            }
            
            switch (activePage)
            {
                case DeploymentPage.Deploying:
                    UpdateProgressBar(progressForeground, percentLabel);
                    break;
                case DeploymentPage.Error:
                    UpdateProgressBar(errorProgressForeground, errorPercentLabel);
                    break;
                default:
                    // Empty
                    break;
            }
        }

        #region Page creation

        void CreateSignIn(VisualElement parent)
        {
            CreateTop(parent,
                "This is where it all starts!",
                "Clicking the button below will open Unordinal's sign in page in your browser. Once signed in, hosting a server and sharing it will be a piece of cake."
                );

            // Sign in button
            Button signInButton = CreateBigButton("Log in to Unordinal");
            signInButton.clicked += (() => 
            {
                signInActiveSubtitle.text = "A sign in page has been opened in your browser." + VerificationCodeSubtitle;
                OnSignIn(true); 
            });

            // Copy sign in url
            Button copySignInURLButton = new Button();
            copySignInURLButton.AddToClassList("only-text-button");
            copySignInURLButton.text = "Copy sign in URL to open in other browser";
            copySignInURLButton.clicked += (() => 
            {
                signInActiveSubtitle.text = "A sign in URL has been copied to your clipboard. Paste it in a browser and complete the sign in." + VerificationCodeSubtitle;
                OnSignIn(false); 
            });

            // Layout
            {
                parent.Add(signInButton);
                parent.Add(copySignInURLButton);
            }
        }

        void CreateSignInActive(VisualElement parent)
        {
            CreateTop(parent,
                "Get login token",
                VerificationCodeSubtitle
                );

            // Verification code
            verificationCodeLabel = new Label("232-F2F");
            verificationCodeLabel.AddToClassList("verification-code-label");

            Button cancelButton = new Button();
            cancelButton.AddToClassList("only-text-button");
            cancelButton.text = "Cancel sign in";
            cancelButton.clicked += OnCancelSignIn;

            // Layout
            {
                parent.Add(verificationCodeLabel);
                parent.Add(cancelButton);
            }
        }

        void CreateSignInSucessfull(VisualElement parent)
        {
            CreateTop(parent,
                "Get login token",
                "A sign in page has been opened in your browser. Ensure that the verification code matches the one in your browser.");

            Texture2D successTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unordinal.hosting/Editor/SignInSuccessful.png", typeof(Texture2D));
            var successImage = new Image() { image = successTexture };
            successImage.AddToClassList("success-image");

            Label successLabel = new Label("LOGIN SUCCESSFUL");

            // Layout
            {
                parent.Add(successImage);
                parent.Add(successLabel);
            }
        }

        void CreateStart(VisualElement parent)
        {
            CreateTop(parent,
                "This is where it all starts!",
                "Your game is specified to run on a specific port, make sure to add the same port below."
                );

            var settingsContainer = new VisualElement();
            foreach (var port in ports)
            {
                CreateProcessSettings(settingsContainer, port);
            }

            // Process button
            Button deployButton = CreateBigButton("Deploy now");
            deployButton.clicked += OnDeploy;
            deployButton.style.marginTop = 40;

            // Dashboard button
            Button dashBoardButton = CreateBigButtonWithoutBackground("Go to dashboard");
            dashBoardButton.clicked += OnDashboard;

            // Layout
            {
                parent.Add(settingsContainer);
                parent.Add(deployButton);
                parent.Add(dashBoardButton);
            }
        }

        void CreateDeploying(VisualElement parent)
        {

            CreateTop(parent,
                "Deploying game to server",
                "Server is now being built and uploaded to Unordinal's servers.\n" +
                "Sit back and relax, this might take a couple of minutes depending on your server size."
                );

            CreateProgressBar(parent, "Vacuuming X-Wings");

            var cancelBtn = new Button();
            cancelBtn.text = "Cancel deploying";
            cancelBtn.RegisterCallback<MouseUpEvent>(evt => OnCancelDeploy());
            cancelBtn.AddToClassList("only-text-button");

            // Layout
            {
                parent.Add(cancelBtn);
            }
            
        }

        void CreateDeployingError(VisualElement parent)
        {
            CreateTop(parent,
                "Deploying game to server",
                "Something went wrong, see below for details.");

            CreateProgressBar(parent, "Error occurred", true);

            var errorBox = new Box();
            errorBox.AddToClassList("error-box");
            errorBox.style.width = progressBarWidth;

            var errorHeaderContainer = new VisualElement();
            errorHeaderContainer.style.flexDirection = FlexDirection.Row;

            int textWidthPercent = 70;

            shortErrorMessage = new TextField();
            shortErrorMessage.AddToClassList("short-error-message-label");
            shortErrorMessage.name = "ErrorMessageInputField";
            shortErrorMessage.value = "Basic info about what went wrong with a link to see details";
            shortErrorMessage.style.width = new StyleLength(new Length(textWidthPercent, LengthUnit.Percent));

            var errorButtonContainer = new VisualElement();
            errorButtonContainer.style.width = new StyleLength(new Length(100 - textWidthPercent, LengthUnit.Percent));
            errorButtonContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            errorButtonContainer.style.alignItems = Align.FlexEnd;
            errorButtonContainer.style.justifyContent = Justify.Center;

            var errorDetailsButton = new Button();
            errorDetailsButton.text = "Details";
            errorDetailsButton.AddToClassList("error-details-button");
            errorDetailsButton.clicked += OnToggleDetailedErrorMessage;

            detailedErrorScrollView = new ScrollView();
            detailedErrorScrollView.AddToClassList("detailed-error-scrollview");

            longErrorMessage = new TextField();
            longErrorMessage.AddToClassList("long-error-message-label");
            longErrorMessage.name = "ErrorMessageInputField";
            longErrorMessage.value = "An error occured, please try again later";
            longErrorMessage.isReadOnly = true;

            Button homeButton = CreateBigButtonWithoutBackground("Start new deploy");
            homeButton.clicked += OnHome;
            homeButton.style.marginTop = 10;

            // Layout
            {
                parent.Add(errorBox);
                {
                    errorBox.Add(errorHeaderContainer);
                    {
                        errorHeaderContainer.Add(shortErrorMessage);

                        errorHeaderContainer.Add(errorButtonContainer);
                        {
                            errorButtonContainer.Add(errorDetailsButton);
                        }
                    }

                    errorBox.Add(detailedErrorScrollView);
                    {
                        detailedErrorScrollView.Add(longErrorMessage);
                    }
                }
                parent.Add(homeButton);
            }
        }

        void CreateDeployFinished(VisualElement parent)
        {
            CreateTop(parent,
                "Game deployed successfully!",
                "Choose what you want to do next, you can go to the dashboard for an overview, or share the IP with your friends right away!");


            CreateShowDeployResult(parent);

            Button dashboardButton = CreateBigButton("Go to dashboard");
            dashboardButton.clicked += OnDashboard;
            dashboardButton.style.marginTop = 25;
            dashboardButton.style.width = bigButtonWidth;
            dashboardButton.style.marginLeft = 0;
            dashboardButton.style.marginRight = 0;

            Button homeButton = CreateBigButtonWithoutBackground("Start new deploy");
            homeButton.clicked += OnHome;
            homeButton.style.marginTop = 10;

            // Layout
            {
                parent.Add(dashboardButton);
                parent.Add(homeButton);
            }
        }

        #endregion

        #region Section creation

        private void CreateTop(VisualElement parent, string title, string info)
        {
            Texture2D logoTextuer = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unordinal.hosting/Editor/Icon-Silver.png", typeof(Texture2D));
            var logoImage = new Image()
            {
                image = logoTextuer,
            };
            logoImage.AddToClassList("logo");

            var titleLabel = new Label(title.ToUpper());
            titleLabel.AddToClassList("Title");

            var subTitleLabel = new Label(info);
            subTitleLabel.AddToClassList("Subtitle");
            subTitleLabel.name = "subtitle";

            // Layout
            {
                parent.Add(logoImage);
                parent.Add(titleLabel);
                parent.Add(subTitleLabel);
            }
            
        }

        private static Button CreateBigButton(string text)
        {
            var button = new Button();
            button.AddToClassList("big-button");
            button.text = text.ToUpper();

            return button;
        }

        private static Button CreateBigButtonWithoutBackground(string text)
        {
            var button = new Button();
            button.AddToClassList("big-button-no-background");
            button.text = text.ToUpper();
            return button;
        }

        void CreateProcessSettings(VisualElement parent, Port port)
        {
            var row = CreatePortAndProtocol(parent, port);
            Button addBtn = (Button)CreateAddRemovePortButton(true);
            addBtn.clicked += OnAddPort(parent);
            row.Add(addBtn);
            row.style.marginBottom = 10; // Override unity style sheet

            Button removeBtn = (Button)CreateAddRemovePortButton(false);
            removeBtn.name = "remove-button";
            removeBtn.clicked += OnRemovePort(parent, port, row);

            if (parent.childCount == 0)
            {
                firstRemoveButton = removeBtn;
            }

            // Layout
            parent.Add(row);
            {
                row.Add(removeBtn);
            }

            EvaluateRemoveButonVisibility(firstRemoveButton, parent);
        }

        private void EvaluateRemoveButonVisibility(VisualElement btn, VisualElement parent)
        {
            firstRemoveButton.visible = parent.childCount > 1 ? true : false;
        }

        VisualElement CreatePortAndProtocol(VisualElement parent, Port port)
        {
            VisualElement rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;

            var portTooltip = "The port number can be found inside the transport added to the NetworkManager component";
            var portLabel = new Label("Port");
            portLabel.tooltip = portTooltip;
            var portInput = new IntegerField();
            portInput.tooltip = portTooltip;
            portInput.RegisterValueChangedCallback(x =>
            {
                port.Number = x.newValue;
                SavePorts();
            });
            portInput.AddToClassList("port-input");
            portInput.name = "port-field";
            portInput.value = port.Number;

            var protocolLabel = new Label("Protocol");
            var protocolComboBox = new EnumField(port.Protocol);
            protocolComboBox.RegisterValueChangedCallback(x =>
            {
                port.Protocol = (Protocol)(x.newValue);
                SavePorts();
            });
            protocolComboBox.AddToClassList("port-input");
            protocolComboBox.name = "protocol-field";

            // Layout
            {
                // Port label
                rowContainer.Add(portLabel);
                // Port input
                rowContainer.Add(portInput);

                // Protocol label
                rowContainer.Add(protocolLabel);
                // Protocol input
                rowContainer.Add(protocolComboBox);
            }

            return rowContainer;
        }

        VisualElement CreateAddRemovePortButton(bool isAddButton)
        {
            var addPortButton = new Button();
            addPortButton.AddToClassList("add-remove-port-button");
            addPortButton.text = isAddButton ? "+" : "-";

            return addPortButton;
        }

        void CreateProgressBar(VisualElement parent, string bigMessage, bool isErrorProgressBar = false)
        {
            var progresBarContainer = new VisualElement();

            var progressBar = new Box();
            progressBar.AddToClassList("progress-bar-background");
            progressBar.style.width = progressBarWidth; // Style changes in c# will override the value in .uss file.

            var progressForeground = new Box();
            progressForeground.AddToClassList("progress-bar-foreground");
            progressForeground.name = isErrorProgressBar ? "progress-error" : "progress";
            progressForeground.style.width = progressBar.style.width.value.value * progress;
            if (isErrorProgressBar)
            {
                progressForeground.style.backgroundColor = new Color(134.0f / 255.0f, 6.0f / 255.0f, 6.0f / 255.0f);
            }

            var progressBarTextContainer = new VisualElement();
            progressBarTextContainer.AddToClassList("progress-bar-text-container");

            var bigProgressBarLabel = new Label();
            bigProgressBarLabel.text = bigMessage;
            bigProgressBarLabel.AddToClassList("progress-bar-funny-message");

            var smallMessageContainer = new VisualElement();
            smallMessageContainer.AddToClassList("container-row-centered");

            var percentLabel = new Label();
            percentLabel.AddToClassList("percent-label");
            percentLabel.AddToClassList("progress-label");
            percentLabel.name = isErrorProgressBar ? "percent-error-label" : "percent-label";
            percentLabel.text = ((int)progress * 100.0f).ToString();

            var percentExplenationLabel = new Label();
            percentExplenationLabel.AddToClassList("progress-label");
            percentExplenationLabel.text = "% of process completed";

            // Layout.
            {
                // ProgressBar
                parent.Add(progresBarContainer);
                {
                    // ProgressBar (Background)
                    progresBarContainer.Add(progressBar);
                    {
                        // Progress bar (Actuall progress)
                        progressBar.Add(progressForeground);
                    }

                    // Progress bar text container
                    progresBarContainer.Add(progressBarTextContainer);
                    {
                        // Funny message
                        progressBarTextContainer.Add(bigProgressBarLabel);

                        // Serious message
                        progressBarTextContainer.Add(smallMessageContainer);
                        {
                            smallMessageContainer.Add(percentLabel);
                            smallMessageContainer.Add(percentExplenationLabel);
                        }
                    }
                }
            }
        }

        void CreateShowDeployResult(VisualElement parent)
        {
            var infoLabel = new Label("Your IP number");
            infoLabel.style.marginTop = -20;
            infoLabel.style.marginBottom = 10;

            var resultContainer = new VisualElement();
            resultContainer.style.flexDirection = FlexDirection.Row;

            resultMessage = new TextField();
            resultMessage.AddToClassList("big-result-field");
            resultMessage.name = "BigResultInputField";
            resultMessage.value = GetOldResult();
            resultMessage.isReadOnly = true;
            resultMessage.style.width = bigButtonWidth - copyResultButtonWidth;

            var copyButton = new Button();
            copyButton.clicked += OnCopyResult;
            copyButton.AddToClassList("copy-button");
            copyButton.style.width = copyResultButtonWidth;

            var buttonLabel = new Label("Copy");
            buttonLabel.AddToClassList("button-label-centered");


            // Layout
            {
                // Info
                parent.Add(infoLabel);

                // result container
                parent.Add(resultContainer);
                {
                    // Result
                    resultContainer.Add(resultMessage);
                    // Copy button
                    resultContainer.Add(copyButton);
                    {
                        copyButton.Add(buttonLabel);
                    }
                }
            }
        }

        #endregion

        #region Page handling

        void HideAllPages()
        {
            if(pages == null)
            {
                return;
            }

            foreach (var page in pages.Values)
            {
                page.style.height = 0;
                page.visible = false;
            }

            // Hide detailed error message
            detailedErrorScrollView.visible = false;
            detailedErrorScrollView.style.height = 0;
            detailedErrorScrollView.style.paddingTop = 0;
            detailedErrorScrollView.style.paddingBottom = 0;
        }

        void ShowPage(DeploymentPage page)
        {
            if(pages != null && pages.ContainsKey(page))
            {
                pages[page].style.height = new StyleLength(StyleKeyword.Auto);
                pages[page].visible = true;
            }
        }

        private void UpdateProgressBar(Box progressForeground, Label percentLabel)
        {
            if (progressForeground != null)
            {
                progressForeground.style.width = progressBarWidth * progress;
            }
            if (percentLabel != null)
            {
                // Progress text with no decimals.
                percentLabel.text = (progress * 100.0f).ToString("F1");
            }
        }

        #endregion

        #region Button clicks

        private async void OnSignIn(bool useDefaultBrowser)
        {
            try
            {
                var signInURL = await GetSignInURL();
                if (signInURL != string.Empty)
                {
                    if (useDefaultBrowser)
                    {
                        Help.BrowseURL(signInURL);
                    }
                    else
                    {
                        UnityEngine.GUIUtility.systemCopyBuffer = signInURL;
                    }
                    await WaitForBrowserAuthentication();
                }
                else
                {
                    activePage = DeploymentPage.SignIn;
                }

                activePage = DeploymentPage.SignInSucess;
                OnGUI(); // Refresh GUI.

                await Task.Delay(1000); // Show the success page for this long.

                activePage = DeploymentPage.Start;
                OnGUI(); // Refresh GUI.
            }
            catch
            {
                // Sign in failed.

                activePage = DeploymentPage.SignIn;
                OnGUI();
            }
        }

        private void OnCancelSignIn()
        {
            loginFlow.Cancel();
            activePage = DeploymentPage.SignIn;
            OnGUI();
        }

        private Action OnAddPort(VisualElement parent)
        {
            return (() =>
            {
                var newPort = new Port() { Number = 7777, Protocol = Protocol.UDP };
                ports.Add(newPort);
                CreateProcessSettings(parent, newPort);
                SavePorts();
            });
        }

        private Action OnRemovePort(VisualElement parent, Port port, VisualElement row)
        {
            return (() =>
            {
                ports.Remove(port);

                if (parent.childCount > 1)
                {
                    parent.Remove(row);
                }
                EvaluateRemoveButonVisibility(firstRemoveButton, parent);
                var removeButtons = parent.Query<Button>().Where(b => b.name == "remove-button").ToList();
                if (removeButtons.Count == 1)
                {
                    removeButtons[0].visible = false;
                    firstRemoveButton = removeButtons[0];
                }
                SavePorts();
            });
        }

        private async void OnDeploy()
        {
            // Set initial values.
            progress = stepToProgressDictionary[DeploymentStep.BuildingServer];
            activePage = DeploymentPage.Deploying;
            resultMessage.value = string.Empty;
            OnGUI(); // Refresh UI to ensure latest progress is visible.
            deploymentFlow.Dispose(); // Clean up old token source.
            deploymentFlow = new CancellationTokenSource(); // "Reset" the cancelation token source.

            try
            {
                var result = await Deploy(deploymentFlow.Token);
                if (result != null)
                {
                    resultMessage.value = result;
                    SaveResult();
                    activePage = DeploymentPage.Finished;
                }
                else
                {
                    activePage = DeploymentPage.Start;
                }
            }
            catch (Exception e)
            {
                shortErrorMessage.value = e.Message;
                longErrorMessage.value = e.StackTrace;
                resultMessage.value = string.Empty;
                activePage = DeploymentPage.Error;
            }
        }

        private async void OnCancelDeploy()
        {
            // Cancel the deployment.
            deploymentFlow.Cancel();

            // Wait until cancelation was successful.
            while (activePage == DeploymentPage.Deploying)
            {
                await Task.Delay(5);
            }

            OnGUI(); // Update UI to show correct view.
        }

        private void OnToggleDetailedErrorMessage()
        {
            detailedErrorScrollView.visible = !detailedErrorScrollView.visible;
            if (detailedErrorScrollView.visible)
            {
                detailedErrorScrollView.style.height = new StyleLength(StyleKeyword.Auto);
                detailedErrorScrollView.style.paddingTop = 10;
                detailedErrorScrollView.style.paddingBottom = 10;
            }
            else
            {
                detailedErrorScrollView.style.height = 0;
                detailedErrorScrollView.style.paddingTop = 0;
                detailedErrorScrollView.style.paddingBottom = 0;
            }
        }

        private void OnCopyResult()
        {
            var test = rootVisualElement.Q<TextField>("BigResultInputField");
            if (test != null)
            {
                UnityEngine.GUIUtility.systemCopyBuffer = test.value;
            }
        }

        private void OnDashboard()
        {
            Help.BrowseURL("https://app.unordinal.com/");
        }

        private void OnHome()
        {
            activePage = DeploymentPage.Start;
        }

        #endregion

        #region PlayerPrefs

        private void EvaluateSignIn()
        {
            tokenStorage.Load();
            if (tokenStorage.HasValidToken) // from previous run
            {
                var initializationTask = InitializeUserInfo();
                initializationTask.Wait();
                if (initializationTask.Result && showSignIn)
                {
                    activePage = DeploymentPage.Start;
                    showSignIn = false;
                }
            }
        }

        private async Task<bool> InitializeUserInfo()
        {
            var response = await auth0Client.isTokenValid();
            if (response.valid)
            {
                userInfoHolder.userInfo = response.user ?? new UserInfo();
                return true;
            }
            return false;
        }

        private void LoadPorts()
        {
            ports = new List<Port>();
            amountOfPorts = PlayerPrefs.HasKey("AmountOfPorts") ? PlayerPrefs.GetInt("AmountOfPorts") : 0;
            if (amountOfPorts > 0)
            {
                for (int i = 0; i < amountOfPorts; i++)
                {
                    var port = new Port();
                    var portKey = "Port" + i;
                    var protocolKey = "Protocol" + i;
                    if (PlayerPrefs.HasKey(portKey) && PlayerPrefs.GetInt(portKey) > 0)
                    {
                        port.Number = PlayerPrefs.GetInt(portKey);
                    }
                    if (PlayerPrefs.HasKey(protocolKey))
                    {
                        port.Protocol = (Protocol)PlayerPrefs.GetInt(protocolKey);
                    }
                    ports.Add(port);

                }
            }
            else
            {
                ports.Add(new Port() { Number = 7777, Protocol = Protocol.UDP });
            }

        }

        private void SavePorts()
        {
            PlayerPrefs.SetInt("AmountOfPorts", ports.Count());
            int i = 0;
            foreach (var port in ports)
            {
                var portKey = "Port" + i;
                var protocolKey = "Protocol" + i;
                PlayerPrefs.SetInt(portKey, port.Number);
                PlayerPrefs.SetInt(protocolKey, (int)port.Protocol);
                ++i;
            }
        }

        private string GetOldResult()
        {
            return PlayerPrefs.GetString("DeployedIpResult", "000.000.000.000:000");
        }

        private void SaveResult()
        {
            PlayerPrefs.SetString("DeployedIpResult", resultMessage.value);
        }

        #endregion

        #region Debug stuff

        private void AddDebugButtons(VisualElement root)
        {
            VisualElement debugContainer = new VisualElement();
            debugContainer.style.flexDirection = FlexDirection.Row;
            debugContainer.style.marginBottom = 0;

            var signInButton = new Button();
            var signInActiveButton = new Button();
            var signInSuccessButton = new Button();
            var startButton = new Button();
            var deployingButton = new Button();
            var errorButton = new Button() { text = "e" };
            var finishedButton = new Button();
            var resetAuth0Button = new Button();
            resetAuth0Button.style.backgroundColor = Color.red;

            signInButton.tooltip = "Sign in page";
            signInActiveButton.tooltip = "Sign in active page";
            signInSuccessButton.tooltip = "Sign in successful page";
            startButton.tooltip = "Start deployment page";
            deployingButton.tooltip = "Deployment in progress page";
            errorButton.tooltip = "Deployment error page";
            finishedButton.tooltip = "Deployment finished page";
            resetAuth0Button.tooltip = "Reset stored sign in token to force sign in again";

            signInButton.clicked += (() =>
            {
                activePage = DeploymentPage.SignIn;
            });
            signInActiveButton.clicked += (() =>
            {
                activePage = DeploymentPage.SignInActive;
            });
            signInSuccessButton.clicked += (() =>
            {
                activePage = DeploymentPage.SignInSucess;
            });
            startButton.clicked += (() =>
            {
                activePage = DeploymentPage.Start;
            });
            deployingButton.clicked += (() =>
            {
                activePage = DeploymentPage.Deploying;
            });
            errorButton.clicked += (() =>
            {
                activePage = DeploymentPage.Error;
            });
            finishedButton.clicked += (() =>
            {
                activePage = DeploymentPage.Finished;
            });
            resetAuth0Button.clicked += (() =>
            {
                PlayerPrefs.SetString("oAuthToken", "lajksdljfa");
            });

            debugContainer.Add(signInButton);
            debugContainer.Add(signInActiveButton);
            debugContainer.Add(signInSuccessButton);
            debugContainer.Add(startButton);
            debugContainer.Add(deployingButton);
            debugContainer.Add(errorButton);
            debugContainer.Add(finishedButton);
            debugContainer.Add(resetAuth0Button);
            root.Add(debugContainer);
        }

        #endregion

        private async Task<string> GetSignInURL()
        {
            var response = await auth0Client.getDeviceCode();
            deviceCode = response.device_code;
            verificationCodeLabel.text = response.user_code;
            activePage = DeploymentPage.SignInActive;
            return response.verification_uri_complete;
        }

        public async Task WaitForBrowserAuthentication()
        {
            logger.LogDebug("Authorize loop start");
            loginFlow.Dispose();
            loginFlow = new CancellationTokenSource();
            var cancelToken = loginFlow.Token;
            var tokenResponse = await auth0Client.getToken(deviceCode, cancelToken);

            logger.LogDebug("Authorize loop finished, reading user info");
            await InitializeUserInfo();
        }

        private async Task<string> Deploy(CancellationToken token)
        {
            LoadEstimatedTimes();

            // Attempt at making the deployment page visible before UI is locked during server build.
            // This is to get the correct progress visible while building the server.
            await Task.Delay(50);

            progressText = "Registering project...";
            if (pluginData.ProjectID == default) { // first time project deployment
                pluginData.ProjectID = await unordinalApi.addProject(pluginData.ProjectName, token);
                pluginData.Save();
                refreshHandler.forceRefresh();
            }

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Build server
            progressText = "Building server...";
            string outputPath = bundler.Bundle(BundleArchitectures.Intel64);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Zipping server
            progressText = "Zipping server...";
            Func<Task> zippingFunc = (async () =>
                await archiver.CreateTarGZAsync("server.tar.gz", outputPath.Replace("\\", "/"), token));
            await InterpolateProgressBarBetweenSteps(zippingFunc, DeploymentStep.ZippingServer);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Building docker image
            var guid = Guid.Empty;
            Func<Task> buildDockerImageFunc = (async () =>
                guid = await unordinalApi.startProcess(
                    pluginData.ProjectID,
                    new List<Port>(ports),
                    token));
            await InterpolateProgressBarBetweenSteps(buildDockerImageFunc, DeploymentStep.BuildingDockerImage);
            if (guid == Guid.Empty) { throw (new Exception("Ops, something went wrong.")); } // Shows error view.

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Get upload URL
            progressText = "GetUploadUrl...";
            var uploadUrl = string.Empty;
            Func<Task> getUploadUrlFunc = (async () => uploadUrl = await unordinalApi.getUploadUrl(guid, token));
            await InterpolateProgressBarBetweenSteps(getUploadUrlFunc, DeploymentStep.GetUploadURL);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Upload file
            progressText = "Upload file!...";
            Func<Task> uploadFileFunc = (async () => await fileUploader.UploadFile(uploadUrl, @"server.tar.gz", token));
            await InterpolateProgressBarBetweenSteps(uploadFileFunc, DeploymentStep.UploadFile);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Build image
            progressText = "Build image!...";
            /*var runId = */await unordinalApi.buildImage(guid, token);
            Func<Task> checkBuildStatusFunc = (async () =>
                await TaskHelpers.RetryWithCondition(
                    () => unordinalApi.checkBuildStatus(guid),
                    (result) => result.Status == "Succeeded",
                    token,
                    maxRetries: int.MaxValue,
                    delay: 5000));
            await InterpolateProgressBarBetweenSteps(checkBuildStatusFunc, DeploymentStep.BuildImage);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Find suitable region
            progressText = "Evaluate region!...";
            Dictionary<string, long> regions = new Dictionary<string, long>(); // If failing to evaluate good region, it will still be able to deploy.
            Func<Task> regionFunc = (async () =>
                regions = await regionalDeploymentService.Ping(regionalDeploymentService.ListOfRegions(), token));
            await InterpolateProgressBarBetweenSteps(regionFunc, DeploymentStep.PingRegions);
#if DEBUG
            if (regions.Count == 0) 
            {
                Debug.Log("Failed to evaluate good region.");
            }
#endif

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Deploy game
            progressText = "Deploy game!...";
            await unordinalApi.deploy(guid, regions, token);
            DeployStatusMessage deployStatusMessage = default;
            Func<Task> deploymentFunc = (async () =>
                deployStatusMessage = await TaskHelpers.RetryWithCondition(
                    () => unordinalApi.checkDeployStatus(guid),
                    result => !string.IsNullOrEmpty(result.Ip),
                    token,
                    maxRetries: int.MaxValue,
                    delay: 5000));
            await InterpolateProgressBarBetweenSteps(deploymentFunc, DeploymentStep.Deploying);

            if (deploymentFlow.IsCancellationRequested) { return null; }

            // Deployment-process finished
            Func<Task> delayFunc = (async () => await Task.Delay(500)); // Go from 94% to 100 % over 500ms
            await InterpolateProgressBarBetweenSteps(delayFunc, DeploymentStep.ShowFinished);

            await Task.Delay(500); // Show 100% for this long (ms)

            progressText = "Publish okay!";
            return deployStatusMessage.Ip;
        }
    }
}
