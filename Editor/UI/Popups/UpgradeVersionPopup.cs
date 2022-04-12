using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public class UpgradeVersionPopup : Popup
    {
        public UpgradeVersionPopup(VisualElement parent, string title, string subtitle, bool showClose) : base (parent, title, subtitle, showClose)
        {
            var downloadPageButton = Controls.BigButton("Download page");
            downloadPageButton.style.marginTop = 40;
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

            // Layout
            {
                popupBox.Add(downloadPageButton);
                popupBox.Add(howToManagePackages);
            }
        }
    }
}
