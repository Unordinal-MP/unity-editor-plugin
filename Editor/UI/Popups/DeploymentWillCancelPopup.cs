using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public class DeploymentWillCancelPopup : Popup
    {
        string cancelDeployTitle = "Cancel deploy";

        // Fields used to animate (shake)
        bool shouldAnimate = false;
        float animationWeight = 1.0f; // Moving to left or right?
        float currentShake = 0;       // Current shake
        float maxShakes = 3;    // How many times should it shake

        public DeploymentWillCancelPopup(VisualElement parent, Action action) : base (parent, false)
        {
            var fontSize = 13;

            var popupTitle = new Label("NOTICE!");
            popupTitle.AddToClassList("MainTitle");
            popupTitle.AddToClassList("popup-title");

            // Container used to have content lign up in same row.
            var playmodeContainer = new VisualElement();
            playmodeContainer.style.flexDirection = FlexDirection.Row;

            var pressedPlayStartLabel = new Label("Play mode (");
            pressedPlayStartLabel.style.fontSize = fontSize;
            pressedPlayStartLabel.AddToClassList("popup-text");

            var pressedPlayEndLabel = new Label(") prevented!");
            pressedPlayEndLabel.style.fontSize = fontSize;
            pressedPlayEndLabel.AddToClassList("popup-text");

            Image playImage = Assets.Images["UnityPlayButton"];
            playImage.style.width = 32;
            playImage.style.height = 16;

            var availableOptionsLabel = new Label("Server deployment needs to be canceled before entering play mode.");
            availableOptionsLabel.AddToClassList("popup-text");
            availableOptionsLabel.style.marginTop = 20;
            availableOptionsLabel.style.fontSize = fontSize;

            var stopButton = Controls.BigButton(cancelDeployTitle);
            stopButton.style.marginTop = 20;
            stopButton.AddToClassList("red-button");
            stopButton.clicked += action;

            var continueDeployButton = Controls.BigButton("Continue deploy");
            continueDeployButton.style.marginTop = 20;
            continueDeployButton.clicked += () =>
            {
                ClosePopupInPlugin();
            };

            // Layout
            {
                popupBox.Add(popupTitle);
                popupBox.Add(playmodeContainer);
                {
                    playmodeContainer.Add(pressedPlayStartLabel);
                    playmodeContainer.Add(playImage);
                    playmodeContainer.Add(pressedPlayEndLabel);
                }
                popupBox.Add(availableOptionsLabel);
                popupBox.Add(stopButton);
                popupBox.Add(continueDeployButton);
            }
        }
        
        public void ShowWarningPopup()
        {
            ShowPopupInPlugin();
            shouldAnimate = true;
            popupBox.transform.position = Vector3.zero;
            currentShake = 0;
        }

        public void TryAnimateToGetAttention()
        {
            if(shouldAnimate)
            {
                // Shake stuff.
                var maxShakeRadius = 30.0f;
                var shakeDelta = 1.3f;
                popupBox.transform.position += animationWeight * Vector3.right * shakeDelta;
                if (popupBox.transform.position.x > maxShakeRadius)
                {
                    animationWeight = -1.0f;
                    currentShake++;
                }
                else if (popupBox.transform.position.x < -maxShakeRadius)
                {
                    animationWeight = 1.0f;
                }

                if(currentShake > maxShakes)
                {
                    if (popupBox.transform.position.x < 0)
                    {
                        // Once back centered again, stop animating.

                        popupBox.transform.position = Vector3.zero;
                        shouldAnimate = false;
                    }
                }
            }
        }
    }
}
