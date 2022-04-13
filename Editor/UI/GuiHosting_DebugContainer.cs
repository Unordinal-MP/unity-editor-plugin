#if DEBUG

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        VisualElement OptionsMenu;

        private VisualElement CreateDebugContainer()
        {
            var debugContainer = new VisualElement();
            
            var firstRow = new VisualElement();
            firstRow.style.flexDirection = FlexDirection.Row;
            firstRow.style.marginBottom = 0;

            OptionsMenu = new VisualElement();
            firstRow.style.flexDirection = FlexDirection.Row;
            firstRow.style.marginBottom = 0;

            AddDebugButtons(firstRow);

            debugContainer.Add(firstRow);
            debugContainer.Add(OptionsMenu);

            return debugContainer;
        }

        public Action AnalyticsAction;

        private void AddDebugButtons(VisualElement firstRow)
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

                AddResultPageOptions();
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

            firstRow.Add(signInButton);
            firstRow.Add(signInActiveButton);
            firstRow.Add(signInSuccessButton);
            firstRow.Add(linuxBuildSupportButton);
            firstRow.Add(unityRestartRequiredButton);
            firstRow.Add(startButton);
            firstRow.Add(waitButton);
            firstRow.Add(deployingButton);
            firstRow.Add(errorButton);
            firstRow.Add(finishedButton);
            firstRow.Add(popupButton);
            firstRow.Add(clearPluginButton);
            firstRow.Add(googleAnalyticsTestButton);
        }

        private void AddResultPageOptions()
        {
            // Show the extra buttons for this 

            var setRandomIpResult = new Button() { text = "Set random ip" };
            setRandomIpResult.clicked += () =>
            {
                var rand = new System.Random();
                IpResult = $"{rand.Next(10, 999)}.{rand.Next(1, 99)}.{rand.Next(1, 99)}.{rand.Next(10, 99)}";
            };

            var addPortMappingButton = new Button() { text = "Add port mapping" };
            addPortMappingButton.clicked += () =>
            {
                var newDeployPorts = new List<External.UnordinalApi.DeployPort>();
                // add already existing ports
                foreach (var port in DeployPort)
                {
                    newDeployPorts.Add(port);
                }
                var rand = new System.Random();
                newDeployPorts.Add(new External.UnordinalApi.DeployPort()
                {
                    ExternalNumber = rand.Next(1, 9999),
                    Number = rand.Next(1, 9999),
                    Protocol = "udp"
                });
                DeployPort = newDeployPorts;
            };

            var removeAllDeployPorts = new Button() { text = "Remove all deploy ports" };
            removeAllDeployPorts.clicked += () =>
            {
                DeployPort = new List<External.UnordinalApi.DeployPort>();
            };

            OptionsMenu.Add(setRandomIpResult);
            OptionsMenu.Add(addPortMappingButton);
            OptionsMenu.Add(removeAllDeployPorts);
        }
    }
}

#endif
