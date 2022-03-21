using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unordinal.Hosting;

namespace Unordinal.Editor.External
{
    public class AnalyticsApi
    {
        #region GA4 constants
        // Deployment (IMPORTANT: Do NOT change any of the strings! Used by GA4.)
        public const string GA4_Event_DeploymentStep_ShowStarted =                "plugin_deployment_start";
        public const string GA4_Event_DeploymentStep_AddProject =                 "plugin_deployment_add_project";
        public const string GA4_Event_DeploymentStep_BuildingClient =             "plugin_deployment_build_client";
        public const string GA4_Event_DeploymentStep_ZippingClient =              "plugin_deployment_zipp_client";
        public const string GA4_Event_DeploymentStep_BuildingServer =             "plugin_deployment_build_server";
        public const string GA4_Event_DeploymentStep_ZippingServer =              "plugin_deployment_zipp_server";
        public const string GA4_Event_DeploymentStep_BuildingDockerImage =        "plugin_deployment_build_docker_image";
        public const string GA4_Event_DeploymentStep_GetClientUploadURL =         "plugin_deployment_get_client_upload_url";
        public const string GA4_Event_DeploymentStep_UploadClient =               "plugin_deployment_upload_client";
        public const string GA4_Event_DeploymentStep_GetServerUploadURL =         "plugin_deployment_get_upload_url";
        public const string GA4_Event_DeploymentStep_UploadServer =               "plugin_deployment_upload_server";
        public const string GA4_Event_DeploymentStep_BuildImage =                 "plugin_deployment_build_image";
        public const string GA4_Event_DeploymentStep_PingRegions =                "plugin_deployment_ping_regions";
        public const string GA4_Event_DeploymentStep_Deploying =                  "plugin_deployment_deploy";
        public const string GA4_Event_DeploymentStep_ShowFinished =               "plugin_deployment_finished";
        // Deployment fails (IMPORTANT: Do NOT change any of the strings! Used by GA4.)
        public const string GA4_Event_DeploymentStep_Failed_ShowStarted =         "plugin_deployment_failed_on_start";
        public const string GA4_Event_DeploymentStep_Failed_AddProject =          "plugin_deployment_failed_on_add_project";
        public const string GA4_Event_DeploymentStep_Failed_BuildingClient =      "plugin_deployment_failed_on_build_client";
        public const string GA4_Event_DeploymentStep_Failed_ZippingClient =       "plugin_deployment_failed_on_zipp_client";
        public const string GA4_Event_DeploymentStep_Failed_BuildingServer =      "plugin_deployment_failed_on_build_server";
        public const string GA4_Event_DeploymentStep_Failed_ZippingServer =       "plugin_deployment_failed_on_zipp_server";
        public const string GA4_Event_DeploymentStep_Failed_BuildingDockerImage = "plugin_deployment_failed_on_build_docker_image";
        public const string GA4_Event_DeploymentStep_Failed_GetClientUploadURL =  "plugin_deployment_failed_on_get_client_upload_url";
        public const string GA4_Event_DeploymentStep_Failed_UploadClient =        "plugin_deployment_failed_on_upload_client";
        public const string GA4_Event_DeploymentStep_Failed_GetServerUploadURL =  "plugin_deployment_failed_on_get_upload_url";
        public const string GA4_Event_DeploymentStep_Failed_UploadServer =        "plugin_deployment_failed_on_upload_server";
        public const string GA4_Event_DeploymentStep_Failed_BuildImage =          "plugin_deployment_failed_on_build_image";
        public const string GA4_Event_DeploymentStep_Failed_PingRegions =         "plugin_deployment_failed_on_ping_regions";
        public const string GA4_Event_DeploymentStep_Failed_Deploying =           "plugin_deployment_failed_on_deploy";
        public const string GA4_Event_DeploymentStep_Failed_ShowFinished =        "plugin_deployment_failed_on_finished";
        // Pages (IMPORTANT: Do NOT change any of the strings! Used by GA4.)
        public const string GA4_Event_Pages_SignIn =                              "plugin_page_sign_in";
        public const string GA4_Event_Pages_SignInActive =                        "plugin_page_waiting_browser";
        public const string GA4_Event_Pages_SignInSuccess =                       "plugin_page_sign_in_success";
        public const string GA4_Event_Pages_Start =                               "plugin_page_deployment_configuration";
        public const string GA4_Event_Pages_Deploying =                           "plugin_page_deploying";
        public const string GA4_Event_Pages_Error =                               "plugin_page_error";
        public const string GA4_Event_Pages_Finished =                            "plugin_page_deployment_result";
        // Clicks (IMPORTANT: Do NOT change any of the strings! Used by GA4.)
        public const string GA4_Event_Clicked_Sign_In =                           "plugin_clicked_sign_in";
        public const string GA4_Event_Clicked_Copy_Sign_In =                      "plugin_clicked_copy_sign_in";
        public const string GA4_Event_Clicked_Cancel_Sign_In =                    "plugin_clicked_cancel_sign_in";
        public const string GA4_Event_Clicked_Deploy =                            "plugin_clicked_deploy";
        public const string GA4_Event_Clicked_Cancel_Deploy =                     "plugin_clicked_cancel_deploy";
        public const string GA4_Event_Clicked_Dashboard =                         "plugin_clicked_dashboard";
        public const string GA4_Event_Clicked_Home =                              "plugin_clicked_home";
        public const string GA4_Event_Clicked_Add_Port =                          "plugin_clicked_add_port";
        public const string GA4_Event_Clicked_Remove_Port =                       "plugin_clicked_remove_port";

        #endregion

        // Dictionaries.
        private static Dictionary<DeploymentPages, string> pageEventDictionary;
        private static Dictionary<DeploymentStep, string> deploymentEventDictionary;
        private static Dictionary<DeploymentStep, string> deploymentFailEventDictionary;

        private readonly HttpClient client;
        private readonly IUserInfoHolder userInfoHolder;
        private readonly ILogger<AnalyticsApi> logger;
        private static string clientID;

        public AnalyticsApi(HttpClient client, IUserInfoHolder userInfoHolder, ILogger<AnalyticsApi> logger)
        {
            this.client = client;
            this.userInfoHolder = userInfoHolder;
            this.logger = logger;
            clientID = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            SetupDictionaries();
        }

        private static void SetupDictionaries()
        {
            // Pages.
            pageEventDictionary = new Dictionary<DeploymentPages, string>();
            pageEventDictionary.Add(DeploymentPages.SignIn, GA4_Event_Pages_SignIn);
            pageEventDictionary.Add(DeploymentPages.SignInActive, GA4_Event_Pages_SignInActive);
            pageEventDictionary.Add(DeploymentPages.SignInSuccess, GA4_Event_Pages_SignInSuccess);
            pageEventDictionary.Add(DeploymentPages.Start, GA4_Event_Pages_Start);
            pageEventDictionary.Add(DeploymentPages.Deploying, GA4_Event_Pages_Deploying);
            pageEventDictionary.Add(DeploymentPages.Error, GA4_Event_Pages_Error);
            pageEventDictionary.Add(DeploymentPages.Finished, GA4_Event_Pages_Finished);

            // Deployment step.
            deploymentEventDictionary = new Dictionary<DeploymentStep, string>();
            deploymentEventDictionary.Add(DeploymentStep.ShowStarted, GA4_Event_DeploymentStep_ShowStarted);
            deploymentEventDictionary.Add(DeploymentStep.AddProject, GA4_Event_DeploymentStep_AddProject);
            deploymentEventDictionary.Add(DeploymentStep.BuildingClient, GA4_Event_DeploymentStep_BuildingClient);
            deploymentEventDictionary.Add(DeploymentStep.ZippingClient, GA4_Event_DeploymentStep_ZippingClient);
            deploymentEventDictionary.Add(DeploymentStep.BuildingServer, GA4_Event_DeploymentStep_BuildingServer);
            deploymentEventDictionary.Add(DeploymentStep.ZippingServer, GA4_Event_DeploymentStep_ZippingServer);
            deploymentEventDictionary.Add(DeploymentStep.BuildingDockerImage, GA4_Event_DeploymentStep_BuildingDockerImage);
            deploymentEventDictionary.Add(DeploymentStep.GetClientUploadURL, GA4_Event_DeploymentStep_GetClientUploadURL);
            deploymentEventDictionary.Add(DeploymentStep.UploadClient, GA4_Event_DeploymentStep_UploadClient);
            deploymentEventDictionary.Add(DeploymentStep.GetServerUploadURL, GA4_Event_DeploymentStep_GetServerUploadURL);
            deploymentEventDictionary.Add(DeploymentStep.UploadServer, GA4_Event_DeploymentStep_UploadServer);
            deploymentEventDictionary.Add(DeploymentStep.BuildImage, GA4_Event_DeploymentStep_BuildImage);
            deploymentEventDictionary.Add(DeploymentStep.PingRegions, GA4_Event_DeploymentStep_PingRegions);
            deploymentEventDictionary.Add(DeploymentStep.Deploying, GA4_Event_DeploymentStep_Deploying);
            deploymentEventDictionary.Add(DeploymentStep.ShowFinished, GA4_Event_DeploymentStep_ShowFinished);

            // Deployment step failed.
            deploymentFailEventDictionary = new Dictionary<DeploymentStep, string>();
            deploymentFailEventDictionary.Add(DeploymentStep.ShowStarted, GA4_Event_DeploymentStep_Failed_ShowStarted);
            deploymentFailEventDictionary.Add(DeploymentStep.AddProject, GA4_Event_DeploymentStep_Failed_AddProject);
            deploymentFailEventDictionary.Add(DeploymentStep.BuildingClient, GA4_Event_DeploymentStep_Failed_BuildingClient);
            deploymentFailEventDictionary.Add(DeploymentStep.ZippingClient, GA4_Event_DeploymentStep_Failed_ZippingClient);
            deploymentFailEventDictionary.Add(DeploymentStep.BuildingServer, GA4_Event_DeploymentStep_Failed_BuildingServer);
            deploymentFailEventDictionary.Add(DeploymentStep.ZippingServer, GA4_Event_DeploymentStep_Failed_ZippingServer);
            deploymentFailEventDictionary.Add(DeploymentStep.BuildingDockerImage, GA4_Event_DeploymentStep_Failed_BuildingDockerImage);
            deploymentFailEventDictionary.Add(DeploymentStep.GetClientUploadURL, GA4_Event_DeploymentStep_Failed_GetClientUploadURL);
            deploymentFailEventDictionary.Add(DeploymentStep.UploadClient, GA4_Event_DeploymentStep_Failed_UploadClient);
            deploymentFailEventDictionary.Add(DeploymentStep.GetServerUploadURL, GA4_Event_DeploymentStep_Failed_GetServerUploadURL);
            deploymentFailEventDictionary.Add(DeploymentStep.UploadServer, GA4_Event_DeploymentStep_Failed_UploadServer);
            deploymentFailEventDictionary.Add(DeploymentStep.BuildImage, GA4_Event_DeploymentStep_Failed_BuildImage);
            deploymentFailEventDictionary.Add(DeploymentStep.PingRegions, GA4_Event_DeploymentStep_Failed_PingRegions);
            deploymentFailEventDictionary.Add(DeploymentStep.Deploying, GA4_Event_DeploymentStep_Failed_Deploying);
            deploymentFailEventDictionary.Add(DeploymentStep.ShowFinished, GA4_Event_DeploymentStep_Failed_ShowFinished);
        }

        public async Task SendAnalyticsGA4(string userID, string eventName, bool isPageViewEvent = false, string details = null)
        {
            // URL
            var baseUrl = "https://www.google-analytics.com/mp/collect";
            var api_secret = PluginSettings.APISecret;
            var measurement_id = PluginSettings.MeasurementID;
            var url = baseUrl + $"?api_secret={api_secret}&measurement_id={measurement_id}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Body
            AnalyticsInfo analyticsInfo = new AnalyticsInfo()
            {
                client_id = clientID,
                user_id = userID,
                events = new List<EventInfo>()
                {
                    new EventInfo()
                    {
                        name = isPageViewEvent ? "page_view" : eventName,
                        paramsPlaceholder = new ParameterInfo()
                        {
                            user_id = userID,
                            details = details,
                            page_title = isPageViewEvent ? eventName : null
                        }
                    }
                }
            };
// TODO: Have a look on how to remove this warning the correct way.
#pragma warning disable CS1701 // Disable warning
            var options = new JsonSerializerOptions() { IgnoreNullValues = true };
            var body = JsonSerializer.Serialize(analyticsInfo, options).Replace("paramsPlaceholder", "params");
#pragma warning restore CS1701
            request.Content = new StringContent(body);

            var response = await client.SendAsync(request);
        }

        public static string GetEventName(DeploymentPages page)
        {
            var foundEventName = pageEventDictionary.TryGetValue(page, out string eventName);
            if (!foundEventName)
            {
                eventName = "plugin_page_" + Enum.GetName(typeof(DeploymentPages), page);
            }

            return eventName;
        }

        public static string GetEventName(DeploymentStep step)
        {
            var foundEventName = deploymentEventDictionary.TryGetValue(step, out string eventName);
            if (!foundEventName)
            {
                eventName = "plugin_deployment_" + Enum.GetName(typeof(DeploymentStep), step);
            }

            return eventName;
        }

        public static string GetEventFailedName(DeploymentStep step)
        {
            var foundEventName = deploymentFailEventDictionary.TryGetValue(step, out string eventName);
            if (!foundEventName)
            {
                eventName = "plugin_deployment_failed_on_" + Enum.GetName(typeof(DeploymentStep), step);
            }

            return eventName;
        }

        #region Classes for analytics data
        public struct AnalyticsInfo
        {
            public string client_id { get; set; }
            public string user_id { get; set; }
            public List<EventInfo> events { get; set; }
        }

        public struct EventInfo
        {
            public string name { get; set; }
            public ParameterInfo paramsPlaceholder { get; set; }
        }

        public struct ParameterInfo
        {
            public string user_id { get; set; }
            public string page_title { get; set; }
            public string details { get; set; }
        }
        #endregion
    }
}
