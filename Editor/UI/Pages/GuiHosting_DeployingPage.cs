using UnityEngine.UIElements;
using Unordinal.Editor.UI;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        public float Progress
        {
            get { return progressBar.Progress; }
            set { progressBar.Progress = value; }
        }

        public bool IsError
        {
            get { return progressBar.IsError; }
            set { progressBar.IsError = value; }
        }

        private ProgressBar progressBar;

        public VisualElement CreateDeployingPage() 
        {
            var page = CreatePageBase(
                "Deploying game to server",
                "Server is now being built and uploaded to Unordinal's servers.\n" +
                "Sit back and relax, this might take a couple of minutes depending on your server size."
                );

            progressBar = new ProgressBar();
            progressBar.BigMessage = "Vacuuming X-Wings";

            var cancelBtn = Controls.ButtonWithClass("Cancel deploying", false, "only-text-button");
            cancelBtn.RegisterCallback<MouseUpEvent>((args) => OnCancelDeploy());

            // Layout
            {
                page.Add(progressBar);
                page.Add(cancelBtn);
            }

            return page;
        }

        float progressFeedbackInterval = 1.0f / 120; // 120 updates per second
        double lastprogressFeedback = 0.0f;
        private void HandleProgressBarFeedback()
        {
            if (ActivePage == DeploymentPages.Deploying)
            {
                if (UnityEditor.EditorApplication.timeSinceStartup - lastprogressFeedback > progressFeedbackInterval)
                {
                    lastprogressFeedback = UnityEditor.EditorApplication.timeSinceStartup;
                    progressBar.RenderProgressFeedback();
                }
            }
        }
    }
}
