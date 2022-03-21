using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private string linuxBuildSupportMainTitle = "Add Linux build support";
        private string linuxBuildSupportSubTitle = "Linux build support is needed for our efficient hosting. It's easy and will just take a minute.";

        public VisualElement CreateAddLinuxPage()
        {
            var page = CreatePageBase(
                linuxBuildSupportMainTitle,
                linuxBuildSupportSubTitle
                );

            var moduleTitle = new Label("Module to add:");
            moduleTitle.style.marginBottom = 5;
            moduleTitle.style.color = new Color(2.0f / 255.0f, 132.0f / 255.0f, 150.0f / 255.0f);
            moduleTitle.style.fontSize = 16;

            var container = new VisualElement();
            container.style.backgroundColor = new Color(75.0f / 255.0f, 71.0f / 255.0f, 71.0f / 255.0f);
            // Corner radius
            container.style.borderBottomLeftRadius = 10;
            container.style.borderBottomRightRadius = 10;
            container.style.borderTopLeftRadius = 10;
            container.style.borderTopRightRadius = 10;
            // Padding
            var padding = 40;
            container.style.paddingBottom = padding;
            container.style.paddingTop = padding;
            container.style.paddingLeft = padding;
            container.style.paddingRight = padding;

            var linuxModuleText = new Label("Linux Build Support (Mono)");
            linuxModuleText.style.fontSize = 24;
            
            var installationGuide = new Label("(Unity Hub > Installs > Add Modules)");
            installationGuide.style.unityFontStyleAndWeight = FontStyle.Italic;
            installationGuide.style.fontSize = 14;
            installationGuide.style.marginTop = 5;

            var documentationButton = new Button();
            documentationButton.text = "Unity documentation";
            documentationButton.AddToClassList("hyperlink-button");
            documentationButton.style.marginTop = 10;
            documentationButton.clicked += (() =>
            {
                // Opens the documentation for how to add modules to unity.
                Help.BrowseURL("https://docs.unity3d.com/2020.1/Documentation/Manual/GettingStartedAddingEditorComponents.html");
            });

            // Layout
            {
                page.Add(moduleTitle);
                page.Add(container);
                {
                    container.Add(linuxModuleText);
                    container.Add(installationGuide);
                    container.Add(documentationButton);
                }
            }

            return page;
        }
    }
}
