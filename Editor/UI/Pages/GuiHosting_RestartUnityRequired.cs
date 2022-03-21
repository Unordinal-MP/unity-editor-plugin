using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        public VisualElement CreateRestartunityRequired()
        {
            var page = CreatePageBase(
                linuxBuildSupportMainTitle,
                linuxBuildSupportSubTitle
                );

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

            var unityRestartLabel = new Label("Unity restart required");
            unityRestartLabel.style.fontSize = 24;

            var restartExplanationLabel = new Label("(Linux build support will be available after restart)");
            restartExplanationLabel.style.fontSize = 12;
            restartExplanationLabel.style.marginTop = 5;

            // Layout
            {
                page.Add(container);
                {
                    container.Add(unityRestartLabel);
                    container.Add(restartExplanationLabel);
                }
            }

            return page;
        }
    }
}
