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
        private Box progressFeedback;
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

            progressFeedback = new Box();
            progressFeedback.AddToClassList("progress-bar-foreground");
            var borderWidth = 0;
            progressFeedback.style.borderRightWidth = borderWidth;
            progressFeedback.style.borderLeftWidth = borderWidth;
            progressFeedback.style.borderTopWidth = borderWidth;
            progressFeedback.style.borderBottomWidth = borderWidth;

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
                        {
                            progressForeground.Add(progressFeedback);
                        }
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

        public void RenderProgress()
        {
            percentLabel.text = string.Format("{0:0.0}% of process completed", progress * 100.0f);
            percentLabel.name = IsError ? "percent-error-label" : "percent-label";
            progressForeground.name = IsError ? "progress-error" : "progress";
            progressForeground.style.width = ProgressBarWidth * progress;

            if (IsError)
            {
                progressFeedback.style.width = 0; // Hide progress feedback when error.
                progressForeground.style.backgroundColor = ErrorColor;
            }
        }

        public void RenderProgressFeedback()
        {
            var time = (float)UnityEditor.EditorApplication.timeSinceStartup;
            var timeFunc = (time % 4.0f) / 2.0f;                // [0, 2]
            
            // Adjust the width of progress feedback, to go from not showing to filling up to current progress.
            var widthPercent = Mathf.Min(1.0f, timeFunc);              // [0, 1]
            progressFeedback.style.width = Mathf.Max(0, ProgressBarWidth * progress * widthPercent - 1);

            // Code below is for when feedback bar has catched up to current real progress.
            // Once the feedback bar has reached 100% width, tone the color down to match the background color.
            var minColorPercent = 0.8f;
            var colorPercent = Mathf.Min(1, Mathf.Max(2 - timeFunc, minColorPercent));
            var brightColor = new Color(67.0f / 255.0f, 134.0f / 255.0f, 7.0f / 255.0f); // This is the brightest color of progress bar.
            var baseColor = new Color(brightColor.r * minColorPercent, brightColor.g * minColorPercent, brightColor.b * minColorPercent);
            var feedbackColor = new Color(brightColor.r * colorPercent, brightColor.g * colorPercent, brightColor.b * colorPercent);
            progressForeground.style.backgroundColor = baseColor; // The base color is the min visible color.
            progressFeedback.style.backgroundColor = feedbackColor; // This is the color which varies over time to give user feedback.
        }

        private float ProgressBarWidth => float.IsNaN(progressBar.contentRect.width) ? 0.0f : progressBar.contentRect.width;
    }
}
