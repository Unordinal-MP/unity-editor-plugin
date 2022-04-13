using System;
using Unordinal.Hosting;
using static Unordinal.Editor.External.UnordinalApi;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private async void EvaluateVersion()
        {
            // We can't evaluate version until we have internet access.
            await ShowNoInternetPopupUntilWeHaveInternet(); // This call will return once we have internet access.

            VersionResponse result;
            try
            {
                result = await unordinalApi.checkPluginSupport(PluginSettings.GetPluginVersion());
            }
            catch(Exception)
            {
                // Something went wrong when communicating with the check version endpoint
                // Or the checkversion endpoint had some internal issues.
                result = new VersionResponse()
                {
                    ApiCallFailed = true
                };
            }
            ShowPopupForVersionResult(result);
        }

        private void ShowPopupForVersionResult(VersionResponse result)
        {
            var extraMessage = result.Message != null ? result.Message + "\n\n" : string.Empty;

            if(result.ApiCallFailed)
            {
                var popup = new VersionCheckFailedPopup(
                rootVisualElement,
                addDebugButtons); // This popup can not be closed (but it's possible in dev mode)
                popup.ShowPopupInPlugin();
            }
            else if (result.MustUpdate)
            {
                var popup = new UpgradeVersionPopup(
                rootVisualElement,
                "Update required",
                $"{extraMessage}" +
                $"This version is no longer supported. Download and add the latest version of the plugin.",
                addDebugButtons); // This popup can not be closed (but it's possible in dev mode)
                popup.ShowPopupInPlugin();
            }
            else if (result.SuggestUpdate)
            {
                var popup = new UpgradeVersionPopup(
                rootVisualElement,
                "Update recommended",
                $"{extraMessage}" +
                $"It's suggested to download and install the latest version of the plugin.",
                true);
                popup.ShowPopupInPlugin();
            }
        }
    }
}
