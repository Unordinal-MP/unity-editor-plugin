using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using Unordinal.Editor.Utils;
using UnityEditor;
using UnityEngine.SceneManagement;

using System.Linq;
using UnityEngine;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        public VisualElement CreateWaitPage()
        {
            var page = CreatePageBase(
                "Unity is building!",
                "It can take some time for Unity to build the server. Once Unity is finished, deployment will start."
                );

            var backgroundBoard = new VisualElement();
            backgroundBoard.AddToClassList("board");
            var paddingWidth = 80;
            var paddingHeight = 50;

            backgroundBoard.style.paddingRight = paddingWidth;
            backgroundBoard.style.paddingLeft = paddingWidth;
            backgroundBoard.style.paddingTop = paddingHeight;
            backgroundBoard.style.paddingBottom = paddingHeight;
            backgroundBoard.style.borderBottomWidth = 0;
            backgroundBoard.style.flexDirection = FlexDirection.Row;

            Image clockImage = Assets.Images["Clock"];
            clockImage.style.width = 35;
            clockImage.style.height = 35;
            clockImage.style.marginRight = 8;

            var waitingLabel = new Label("Waiting for Unity...");
            waitingLabel.style.fontSize = 20;

            // Layout
            {
                page.Add(backgroundBoard);
                {
                    backgroundBoard.Add(clockImage);
                    backgroundBoard.Add(waitingLabel);
                }
            }

            return page;
        }
    }
}
