using UnityEngine;
using UnityEngine.UIElements;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public class Popup
    {
        internal VisualElement popupBox;
        private VisualElement parent;

        public VisualElement popup;

        public Popup(VisualElement parent, bool showClose)
        {
            this.parent = parent;

            // This element will cover entire plugin and dim it down.
            popup = new VisualElement();
            popup.AddToClassList("popup-background");
            popup.style.backgroundColor = new Color(0, 0, 0, 0.7f);

            // This is the box on top of the dimmed out area.
            popupBox = new VisualElement();
            popupBox.AddToClassList("popup-box");

            var closeButton = new Button();
            closeButton.AddToClassList("popup-close-button");
            closeButton.clicked += () => ClosePopupInPlugin();

            var crossImage = Assets.Images["cross"];
            crossImage.style.width = 10;
            crossImage.style.height = 10;

            // Layout
            {
                popup.Add(popupBox);
                {
                    if (showClose)
                    {
                        popupBox.Add(closeButton);
                        {
                            closeButton.Add(crossImage);
                        }
                    }
                }
            }
        }

        public Popup(VisualElement parent, string title, string subtitle, bool showClose) : this(parent, showClose)
        {
            var popupTitle = new Label(title);
            popupTitle.AddToClassList("MainTitle");
            popupTitle.AddToClassList("popup-title");

            var popupContent = new Label(subtitle);
            popupContent.AddToClassList("popup-text");

            // Layout
            {
                popupBox.Add(popupTitle);
                popupBox.Add(popupContent);
            }
        }

        public void ShowPopupInPlugin()
        {
            if (!parent.Contains(popup))
            {
                parent.Add(popup);
            }
        }

        public void ClosePopupInPlugin()
        {
            parent.Remove(popup);
        }
    }
}
