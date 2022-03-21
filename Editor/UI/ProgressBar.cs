using UnityEngine;
using UnityEngine.UIElements;

namespace Unordinal.Editor.UI
{
    public class ProgressBar: VisualElement
    {
        public float Progress
        {
            get { return progress; }
            set { progress = value; RenderProgress(); }
        }

        public bool IsError
        {
            get { return error; }
            set { error = value; RenderProgress(); }
        }

        public string BigMessage
        {
            get { return bigProgressBarLabel.text; }
            set { bigProgressBarLabel.text = value; }
        }

        private static Color ErrorColor = new Color(134.0f / 255.0f, 6.0f / 255.0f, 6.0f / 255.0f);

        private float progress = 0.0f;
        private bool error = false;

        private Label percentLabel;
        private Box progressForeground;
        private Box progressBar;
        private Label bigProgressBarLabel;

        public ProgressBar()
        {
            Render();
        }

        private void Render()
        {

            progressBar = new Box();
            progressBar.AddToClassList("progress-bar-background");

            progressForeground = new Box();
            progressForeground.AddToClassList("progress-bar-foreground");

            var progressBarTextContainer = new VisualElement();
            progressBarTextContainer.AddToClassList("progress-bar-text-container");

            bigProgressBarLabel = new Label();
            bigProgressBarLabel.AddToClassList("progress-bar-funny-message");

            var smallMessageContainer = new VisualElement();
            smallMessageContainer.AddToClassList("container-row-centered");

            percentLabel = new Label();
            percentLabel.AddToClassList("percent-label");
            percentLabel.AddToClassList("progress-label");
            RenderProgress();

            // Layout.
            {
                {
                    this.Add(progressBar);
                    {
                        // Progress bar (Actuall progress)
                        progressBar.Add(progressForeground);
                    }
                    this.Add(progressBarTextContainer);
                    {
                        // Funny message
                        progressBarTextContainer.Add(bigProgressBarLabel);

                        // Serious message
                        progressBarTextContainer.Add(smallMessageContainer);
                        {
                            smallMessageContainer.Add(percentLabel);
                        }
                    }
                }
            }
        }

        private void RenderProgress()
        {
            percentLabel.text = string.Format("{0:0.0}% of process completed", progress * 100.0f);
            percentLabel.name = IsError ? "percent-error-label" : "percent-label";
            progressForeground.name = IsError ? "progress-error" : "progress";
            progressForeground.style.width = ProgressBarWidth * progress;
            if (IsError)
            {
                progressForeground.style.backgroundColor = ErrorColor;
            }
        }

        private float ProgressBarWidth => float.IsNaN(progressBar.contentRect.width) ? 0.0f : progressBar.contentRect.width;
    }
}
