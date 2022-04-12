using System.Net.Http;
using System.Threading.Tasks;
using static Unordinal.Editor.External.UnordinalApi;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        /// <summary>
        /// Shows a popup when the the plugin has no internet access.
        /// </summary>
        /// <returns>Method returns when internet is available.</returns>
        private async Task ShowNoInternetPopupUntilWeHaveInternet()
        {
            bool noInternet = !await HasInternet();   

            if(noInternet)
            {
                var popup = new Popup(
                    rootVisualElement,
                    "No internet access",
                    "This plugin helps you deploy your multiplayer servers\n\n" +
                    "Without internet we can't help you.",
                    addDebugButtons); // Don't allow manual close of popup since deploy won't auto continue if popup is closed before obtaining internet again.
                popup.ShowPopupInPlugin();

                while (noInternet)
                {
                    // Continue to check internet access.
                    await Task.Delay(500);
                    noInternet = !await HasInternet();
                }

                // Internet available again! Close popup so user can use plugin again.
                popup.ClosePopupInPlugin();
            }
        }

        private async Task<bool> HasInternet()
        {
            bool hasInternet = true;
            try
            {
                HttpClient client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://google.com");
                var response = await client.SendAsync(request);
            }
            catch
            {
                // We didn't get any response. We don't have internet.
                hasInternet = false;
            }
            
            return hasInternet;
        }
    }
}
