using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using static Unordinal.Editor.External.AnalyticsApi;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        public VisualElement CreateSignInPage()
        {
            var page = CreatePageBase(
                "This is where it all starts!",
                "Clicking the button below will open Unordinal's sign in page in your browser. Once signed in, hosting a server and sharing it will be a piece of cake."
                );

            // "Browser" sign in button
            Button signInButton = Controls.BigButton("Log in to Unordinal");
            signInButton.clicked += () => OnSignIn(async signInURL =>
            {
                IsBrowserOpened = true;
                Help.BrowseURL(signInURL);

                // Analytics
                await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Sign_In);
            });

            // "CopyUrl" sign in button.
            Button copySignInURLButton = Controls.ButtonWithClass("Copy sign in URL to open in other browser", false, "only-text-button");
            copySignInURLButton.clicked += () => OnSignIn(async signInURL =>
            {
                IsBrowserOpened = false;
                GUIUtility.systemCopyBuffer = signInURL;

                // Analytics
                await analyticsApi.SendAnalyticsGA4(userID, GA4_Event_Clicked_Copy_Sign_In);
            });

            // Layout
            {
                page.Add(signInButton);
                page.Add(copySignInURLButton);
            }

            return page;
        }
    }
}
