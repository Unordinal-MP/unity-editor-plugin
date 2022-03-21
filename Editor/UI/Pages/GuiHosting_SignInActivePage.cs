using UnityEngine.UIElements;
using Unordinal.Editor.UI;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private const string VerificationCodeSubtitle = "Ensure that the verification code matches the one in your browser. ";
        private const string BrowserOpenedScenarioComment = "A sign in page has been opened in your browser. ";
        private const string UrlCopiedScenarioComment = "A sign in URL has been copied to your clipboard. Paste it in a browser and complete the sign in. ";

        private Label SignInActiveSubtitle;

        private Label _verificationCodeLabel;
        public string VerificationCode
        {
            get { return _verificationCodeLabel.text; }
            set { _verificationCodeLabel.text = value; }
        }

        private bool _browserOpened;
        public bool IsBrowserOpened
        {
            get { return _browserOpened; }
            set
            {
                _browserOpened = value;
                SignInActiveSubtitle.text = (value ? BrowserOpenedScenarioComment : UrlCopiedScenarioComment) + VerificationCodeSubtitle;
            }
        }

        public VisualElement CreateSignInActivePage()
        {
            var page = CreatePageBase(
                "Get login token",
                VerificationCodeSubtitle
                );

            // Store subtitle for later update.
            SignInActiveSubtitle = GetSubtitleElement(page);

            // Verification code
            _verificationCodeLabel = new Label("232-F2F");
            _verificationCodeLabel.AddToClassList("verification-code-label");

            Button cancelButton = Controls.ButtonWithClass("Cancel sign in", false, "only-text-button");
            cancelButton.clicked += OnCancelSignIn;

            // Layout
            {
                page.Add(_verificationCodeLabel);
                page.Add(cancelButton);
            }

            return page;
        }
    }
}
