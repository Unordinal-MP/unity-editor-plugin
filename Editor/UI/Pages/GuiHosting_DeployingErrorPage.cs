using UnityEditor;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private TextField shortErrorMessage;
        private TextField longErrorMessage;

        private int textWidthPercent = 70;
        private ProgressBar errorProgressBar;
        private ScrollView detailedErrorScrollView;

        public string ShortErrorMessage
        {
            get { return shortErrorMessage.value; }
            set 
            { 
                shortErrorMessage.value = value;
                EditorPrefs.SetString(UnordinalKeys.errorMainMessageKey, value);
            }
        }

        public string LongErrorMessage
        {
            get { return longErrorMessage.value; }
            set 
            { 
                longErrorMessage.value = value;
                EditorPrefs.SetString(UnordinalKeys.errorDetailedMessageKey, value);
            }
        }

        public float ErrorProgress
        {
            get { return errorProgressBar.Progress; }
            set { errorProgressBar.Progress = value; }
        }

        public VisualElement CreateDeployingErrorPage()
        {
            var page = CreatePageBase(
                "Deploying game to server",
                "Something went wrong, see below for details."
                );

            errorProgressBar = new ProgressBar();
            errorProgressBar.IsError = true;
            errorProgressBar.BigMessage = "Error occurred";

            var errorBox = new Box();
            errorBox.AddToClassList("error-box");

            var errorHeaderContainer = new VisualElement();
            errorHeaderContainer.style.flexDirection = FlexDirection.Row;

            var defaultShortMessage = "Basic info about what went wrong with a link to see details";
            var shortErrorText = UnityEditor.EditorPrefs.GetString(UnordinalKeys.errorMainMessageKey, defaultShortMessage);
            shortErrorMessage = Controls.TextBox("short-error-message-label", "ErrorMessageInputField", shortErrorText);
            shortErrorMessage.style.width = new StyleLength(new Length(textWidthPercent, LengthUnit.Percent));
            VisualElement errorButtonContainer = Controls.PaddedContainer(textWidthPercent);

            var errorDetailsButton = Controls.ButtonWithClass("Details", false, "error-details-button");
            errorDetailsButton.clicked += OnToggleDetailedErrorMessage;

            detailedErrorScrollView = new ScrollView();
            detailedErrorScrollView.AddToClassList("detailed-error-scrollview");

            var defaultLongErrorMessage = "An error occured, please try again later";
            var longErrorText = UnityEditor.EditorPrefs.GetString(UnordinalKeys.errorDetailedMessageKey, defaultLongErrorMessage);
            longErrorMessage = Controls.TextBox("long-error-message-label", "ErrorMessageInputField", longErrorText);

            Button homeButton = Controls.BigButtonWithoutBackground("Start new deploy");
            homeButton.clicked += OnHome;
            homeButton.style.marginTop = 10;

            SetDetailedErrorMessageVisible(false);

            // Layout
            {
                page.Add(errorProgressBar);
                page.Add(errorBox);
                {
                    errorBox.Add(errorHeaderContainer);
                    {
                        errorHeaderContainer.Add(shortErrorMessage);

                        errorHeaderContainer.Add(errorButtonContainer);
                        {
                            errorButtonContainer.Add(errorDetailsButton);
                        }
                    }
                    errorBox.Add(detailedErrorScrollView);
                    {
                        detailedErrorScrollView.Add(longErrorMessage);
                    }
                }
                page.Add(homeButton);
            }
            

            return page;
        }

        public void ResetError()
        {
            SetDetailedErrorMessageVisible(false);
        }

        private void OnToggleDetailedErrorMessage()
        {
            SetDetailedErrorMessageVisible(!longErrorMessage.visible);
        }

        private void SetDetailedErrorMessageVisible(bool visible)
        {
            longErrorMessage.visible = visible;
            if (longErrorMessage.visible)
            {
                detailedErrorScrollView.style.height = new StyleLength(StyleKeyword.Auto);
                detailedErrorScrollView.style.paddingTop = 10;
                detailedErrorScrollView.style.paddingBottom = 10;
            }
            else
            {
                detailedErrorScrollView.style.height = 0;
                detailedErrorScrollView.style.paddingTop = 0;
                detailedErrorScrollView.style.paddingBottom = 0;
            }
        }
    }
}
