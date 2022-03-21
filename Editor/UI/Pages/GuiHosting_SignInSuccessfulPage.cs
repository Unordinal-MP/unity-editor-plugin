using UnityEngine.UIElements;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        public VisualElement CreateSignInSuccessfulPage()
        {
            var page = CreatePageBase(
                "Get login token",
                "A sign in page has been opened in your browser. Ensure that the verification code matches the one in your browser."
                );

            var successImage = Assets.Images["SignInSuccessful"];
            successImage.AddToClassList("success-image");

            Label successLabel = new Label("LOGIN SUCCESSFUL");

            // Layout
            {
                page.Add(successImage);
                page.Add(successLabel);
            }

            return page;
        }
    }
}
