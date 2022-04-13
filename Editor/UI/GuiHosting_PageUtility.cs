using System.Collections.Generic;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;

namespace Unordinal.Editor
{
    public partial class GuiHosting/*: VisualElement*/
    {
        private VisualElement container;

        public event PageChangeHandler<DeploymentPages> PageChanged;

        public DeploymentPages PreviousPage { get; private set; }
        public DeploymentPages ActivePage { get; private set; } = DeploymentPages.SignIn;

        private readonly Dictionary<DeploymentPages, VisualElement> pages = new Dictionary<DeploymentPages, VisualElement>();

        public VisualElement CreatePageContainer()
        {
            container = new VisualElement();
            container.AddToClassList("plugin-container");
            container.style.minHeight = minSize.y;
            PageChanged += OnPageChanged;

            return container;
        }

        public VisualElement CreatePageBase(string title, string description)
        {
            var pageBase = new VisualElement();
            pageBase.AddToClassList("plugin-container");

            Image logoImage = Assets.Images["Icon-Silver"];
            logoImage.AddToClassList("logo");

            var titleElement = new Label(title.ToUpper());
            titleElement.AddToClassList("MainTitle");

            var subtitleElement = new Label(description);
            subtitleElement.AddToClassList("SubTitle");

            // Layout
            {
                pageBase.Add(logoImage);
                pageBase.Add(titleElement);
                pageBase.Add(subtitleElement);
            }

            return pageBase;
        }

        public Label GetSubtitleElement(VisualElement parent)
        {
            return parent.Query<Label>().Where(l => l.ClassListContains("SubTitle")).First();
        }

        #region Utility

        public void AddPage(DeploymentPages pageIdentifer, VisualElement page)
        {
            pages.Add(pageIdentifer, page);
        }

        public void ShowPage(DeploymentPages pageIdentifier)
        {
            container.Clear();
            container.Add(pages[pageIdentifier]);
            PageChanged?.Invoke(ActivePage, pageIdentifier);
            PreviousPage = ActivePage;
            ActivePage = pageIdentifier;

            if(addDebugButtons && OptionsMenu != null)
            {
                OptionsMenu.Clear();
            }
        }

        #endregion
    }

    public delegate void PageChangeHandler<T>(T oldPage, T newPage);
}
