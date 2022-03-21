using UnityEngine.UIElements;

namespace Unordinal.Editor.UI
{
    public static class Controls
    {
        public static Button BigButton(string text)
        {
            return ButtonWithClass(text, true, "big-button");
        }

        public static Button ButtonWithClass(string text, bool caps, string className)
        {
            var button = new Button();
            button.AddToClassList(className);
            button.text = caps ? text.ToUpper() : text;
            return button;
        }

        public static Button BigButtonWithoutBackground(string text)
        {
            return ButtonWithClass(text, true, "big-button-no-background");
        }

        public static TextField TextBox(string className, string name, string value)
        {
            var textBox = new TextField();
            textBox.AddToClassList(className);
            textBox.name = name;
            textBox.value = value;
            textBox.isReadOnly = true;
            return textBox;
        }

        public static VisualElement PaddedContainer(int paddedContentWidthPercentage)
        {
            var errorButtonContainer = new VisualElement();
            errorButtonContainer.style.width = new StyleLength(new Length(100 - paddedContentWidthPercentage, LengthUnit.Percent));
            errorButtonContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            errorButtonContainer.style.alignItems = Align.FlexEnd;
            errorButtonContainer.style.justifyContent = Justify.Center;
            return errorButtonContainer;
        }
    }
}
