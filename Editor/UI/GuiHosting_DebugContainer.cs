#if DEBUG

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private VisualElement CreateDebugContainer()
        {
            var debugContainer = new VisualElement();
            debugContainer.style.flexDirection = FlexDirection.Row;
            debugContainer.style.marginBottom = 0;
            AddDebugButtons(debugContainer);

            return debugContainer;
        }

        public Action AnalyticsAction;

        private void AddDebugButtons(VisualElement parent)
        {
            var signInButton = new Button();
            var signInActiveButton = new Button();
            var signInSuccessButton = new Button();
            var linuxBuildSupportButton = new Button() { text = "L" };
            var unityRestartRequiredButton = new Button() { text = "R" };
            var startButton = new Button();
            var waitButton = new Button() { text = "w" };
            var deployingButton = new Button();
            var errorButton = new Button() { text = "e" };
            var finishedButton = new Button();
            var popupButton = new Button() { text = "P" };
            var clearPluginButton = new Button();
            clearPluginButton.style.backgroundColor = Color.red;
            var googleAnalyticsTestButton = new Button() { text = "GA4" };
            
            signInButton.tooltip = "Sign in page";
            signInActiveButton.tooltip = "Sign in active page";
            signInSuccessButton.tooltip = "Sign in successful page";
            linuxBuildSupportButton.tooltip = "Linux build support page";
            unityRestartRequiredButton.tooltip = "Unity restart required page";
            startButton.tooltip = "Start deployment page";
            waitButton.tooltip = "Wait for builds to finish page";
            deployingButton.tooltip = "Deployment in progress page";
            errorButton.tooltip = "Deployment error page";
            finishedButton.tooltip = "Deployment finished page";
            popupButton.tooltip = "Show popup window";
            clearPluginButton.tooltip = "Reset stored values in the plugin";
            googleAnalyticsTestButton.tooltip = "Send data to google analytics";

            signInButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.SignIn);
            });
            signInActiveButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.SignInActive);
            });
            signInSuccessButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.SignInSuccess);
            });
            linuxBuildSupportButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.LinuxBuildSupportRequired);
            });
            unityRestartRequiredButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.RestartUnityRequired);
            });
            startButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.Start);
            });
            waitButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.Wait);
            });
            deployingButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.Deploying);
            });
            errorButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.Error);
            });
            finishedButton.clicked += (() =>
            {
                ShowPage(DeploymentPages.Finished);
            });
            popupButton.clicked += (() =>
            {
                ShowPopupForVersionResult(new External.UnordinalApi.VersionResponse() { MustUpdate = true });
            });
            clearPluginButton.clicked += (() =>
            {
                IpResult = "bla";
                UnordinalKeys.ClearEditorPrefs();
                tokenStorage.Clear();
            });
            googleAnalyticsTestButton.clicked += (() =>
            {
                AnalyticsAction?.Invoke();
            });

            parent.Add(signInButton);
            parent.Add(signInActiveButton);
            parent.Add(signInSuccessButton);
            parent.Add(linuxBuildSupportButton);
            parent.Add(unityRestartRequiredButton);
            parent.Add(startButton);
            parent.Add(waitButton);
            parent.Add(deployingButton);
            parent.Add(errorButton);
            parent.Add(finishedButton);
            parent.Add(popupButton);
            parent.Add(clearPluginButton);
            parent.Add(googleAnalyticsTestButton);
        }
    }
}

#endif
