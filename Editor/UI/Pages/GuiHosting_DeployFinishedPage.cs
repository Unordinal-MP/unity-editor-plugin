﻿using UnityEngine.UIElements;
using Unordinal.Editor.UI;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private int copyResultButtonWidth = 50;
        private int bigButtonWidth = 280;
        private TextField ipResultMessage;
        private TextField PlayWithFriendsResultMessage;
        private VisualElement playWithFriendsContainer;

        private string ipResult = "000.000.000.0";
        private string playWithFriendsResult = "https:Azure";
        public string IpResult
        {
            get { return ipResult; }
            set 
            {
                if (ipResult != value)
                {
                    ipResult = value;
                    ipResultMessage.value = value;
                }
            }
        }

        public string PlayWithFriendsResult
        {
            get { return playWithFriendsResult; }
            set
            {
                if(playWithFriendsResult != value)
                {
                    playWithFriendsResult = value;
                    PlayWithFriendsResultMessage.value = value;
                }
            }
        }

        public VisualElement CreateDeployFinishedPage() 
        {
            var page = CreatePageBase(
                "Game deployed successfully!",
                "Choose what you want to do next, you can go to the dashboard for an overview, or share the IP with your friends right away!"
                );

            CreateShowDeployResult(page);

            Button dashboardButton = Controls.BigButton("Go to dashboard");
            dashboardButton.clicked += OnDashboard;
            dashboardButton.style.marginTop = 25;
            dashboardButton.style.width = bigButtonWidth;
            dashboardButton.style.marginLeft = 0;
            dashboardButton.style.marginRight = 0;

            Button homeButton = Controls.BigButtonWithoutBackground("Start new deploy");
            homeButton.clicked += OnHome;
            homeButton.style.marginTop = 10;

            // Layout
            {
                page.Add(dashboardButton);
                page.Add(homeButton);
            }

            return page;
        }

        private void CreateShowDeployResult(VisualElement parent)
        {
            var infoLabel = new Label("Your IP number");
            infoLabel.style.marginTop = -20;
            infoLabel.style.marginBottom = 10;

            var playWithFriendsInfoLabel = new Label("Game download link");
            playWithFriendsInfoLabel.style.marginTop = 23;
            playWithFriendsInfoLabel.style.marginBottom = 10;

            var resultContainer = new VisualElement();
            resultContainer.style.flexDirection = FlexDirection.Row;

            ipResultMessage = new TextField();
            ipResultMessage.AddToClassList("big-result-field");
            ipResultMessage.name = "BigResultInputField";
            ipResultMessage.isReadOnly = true;
            ipResultMessage.style.width = bigButtonWidth - copyResultButtonWidth;
            ipResultMessage.value = ipResult;

            playWithFriendsContainer = new VisualElement();
            HandlePlayWithFriendsResultVisibility(playWithFriendsToggleButton.value);

            var playWithFriendsResultContainer = new VisualElement();
            playWithFriendsResultContainer.style.flexDirection = FlexDirection.Row;

            PlayWithFriendsResultMessage = new TextField();
            PlayWithFriendsResultMessage.AddToClassList("playWithFriends-big-result-field");
            PlayWithFriendsResultMessage.name = "playWithFriends-BigResultInputField";
            PlayWithFriendsResultMessage.isReadOnly = true;
            PlayWithFriendsResultMessage.style.width = bigButtonWidth - copyResultButtonWidth;
            PlayWithFriendsResultMessage.value = playWithFriendsResult;


            var copyButton = new Button();
            copyButton.clicked += () => UnityEngine.GUIUtility.systemCopyBuffer = ipResultMessage.value;
            copyButton.AddToClassList("copy-button");
            copyButton.style.width = copyResultButtonWidth;

            var playWithFriendsCopyButton = new Button();
            playWithFriendsCopyButton.clicked += () => UnityEngine.GUIUtility.systemCopyBuffer = PlayWithFriendsResultMessage.value;
            playWithFriendsCopyButton.AddToClassList("playWithFriends-copy-button");
            playWithFriendsCopyButton.style.width = copyResultButtonWidth;

            var buttonLabel = new Label("Copy");
            buttonLabel.AddToClassList("button-label-centered");

            var playWithFriendsButtonLabel = new Label("Copy");
            playWithFriendsButtonLabel.AddToClassList("playWithFriends-button-label-centered");

            // Layout
            {
                parent.Add(infoLabel);
                parent.Add(resultContainer);
                {
                    // Result
                    resultContainer.Add(ipResultMessage);
                    // Copy button
                    resultContainer.Add(copyButton);
                    {
                        copyButton.Add(buttonLabel);
                    }
                }
                parent.Add(playWithFriendsContainer);
                {
                    // Title
                    playWithFriendsContainer.Add(playWithFriendsInfoLabel);
                    //parent.Add(playWithFriendsInfoLabel);
                    playWithFriendsContainer.Add(playWithFriendsResultContainer);
                    {
                        // Result
                        playWithFriendsResultContainer.Add(PlayWithFriendsResultMessage);
                        // Copy button
                        playWithFriendsResultContainer.Add(playWithFriendsCopyButton);
                        {
                            playWithFriendsCopyButton.Add(playWithFriendsButtonLabel);
                        }
                    }
                }
            }
        }

        private void HandlePlayWithFriendsResultVisibility(bool visible)
        {
            if(visible)
            {
                playWithFriendsContainer.visible = true;
                playWithFriendsContainer.style.height = new StyleLength(StyleKeyword.Auto); ;
                playWithFriendsContainer.style.width = new StyleLength(StyleKeyword.Auto); ;
            }
            else
            {
                playWithFriendsContainer.visible = false;
                playWithFriendsContainer.style.height = 0;
                playWithFriendsContainer.style.width = 0;
            }
        }
    }
}