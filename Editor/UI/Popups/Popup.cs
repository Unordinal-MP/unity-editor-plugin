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

            // The container holding the label with the +
            // This container gets rotated 45 degrees to from an X (looks better than a x-character).
            var crossContainer = new VisualElement();
            crossContainer.transform.position += new Vector3(11, -1.0f, 0.0f);
            var crossConteinerWidth = crossContainer.style.width;
            crossContainer.transform.rotation = Quaternion.Euler(0, 0, 45);

            // The + will be rotated by the parent container and form the close [X] on the button.
            var crossSign = new Label("+");
            crossSign.style.unityFontStyleAndWeight = FontStyle.Bold;
            crossSign.style.fontSize = 25;
            crossSign.style.alignSelf = Align.Center;
            crossSign.style.justifyContent = Justify.Center;

            // Layout
            {
                popup.Add(popupBox);
                {
                    if (showClose)
                    {
                        popupBox.Add(closeButton);
                        {
                            closeButton.Add(crossContainer);
                            {
                                crossContainer.Add(crossSign);
                            }
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
