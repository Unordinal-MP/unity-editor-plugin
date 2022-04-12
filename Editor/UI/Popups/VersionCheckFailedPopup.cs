using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public class VersionCheckFailedPopup : Popup
    {
        float buttonMarginTop = 20;
        float buttonWidth = 250;

        public VersionCheckFailedPopup(VisualElement parent, bool showClose) : base (parent, showClose)
        {
            var popupTitle = new Label("Something went wrong");
            popupTitle.AddToClassList("MainTitle");
            popupTitle.AddToClassList("popup-title");

            var versionText = new Label("Ensure that you are using the latest version of the plugin:");
            versionText.AddToClassList("popup-text");

            var downloadPageButton = Controls.BigButton("Download page");
            downloadPageButton.style.marginTop = buttonMarginTop;
            downloadPageButton.style.width = buttonWidth;
            downloadPageButton.clicked += () =>
            {
                Help.BrowseURL("https://github.com/Unordinal-MP/unity-editor-plugin");
            };

            var howToManagePackages = new Button() { text = "Unity: How to manage packages." };
            howToManagePackages.AddToClassList("hyperlink-button");
            howToManagePackages.style.marginTop = 10;
            howToManagePackages.clicked += () =>
            {
                Help.BrowseURL("https://docs.unity3d.com/2020.1/Documentation/Manual/upm-ui-actions.html");
            };

            var contactSupportText = new Label("If the issue still persists, reach out to us.");
            contactSupportText.AddToClassList("popup-text");
            contactSupportText.style.marginTop = 40;

            var discordButton = Controls.BigButton("Discord");
            discordButton.style.marginTop = buttonMarginTop;
            discordButton.style.width = buttonWidth;
            discordButton.clicked += () =>
            {
                Help.BrowseURL("https://discord.gg/kPhCMwHc2M");
            };

            var supportButton = Controls.BigButton("unordinal.com");
            supportButton.style.marginTop = buttonMarginTop;
            supportButton.style.width = buttonWidth;
            supportButton.clicked += () =>
            {
                Help.BrowseURL("https://unordinal.com");
            };

            // Layout
            {
                popupBox.Add(popupTitle);
                popupBox.Add(versionText);
                popupBox.Add(downloadPageButton);
                popupBox.Add(howToManagePackages);
                popupBox.Add(contactSupportText);
                popupBox.Add(discordButton);
                popupBox.Add(supportButton);
            }
        }
    }
}
