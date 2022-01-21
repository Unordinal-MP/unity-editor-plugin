using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Unordinal.Hosting
{
    public class UnordinalApi
    {
        private readonly HttpClient client;

        private readonly IUserInfoHolder userInfoHolder;

        private readonly ILogger<UnordinalApi> logger;

        public UnordinalApi(HttpClient client, IUserInfoHolder userInfoHolder, ILogger<UnordinalApi> logger)
        {
            this.client = client;
            this.userInfoHolder = userInfoHolder;
            this.logger = logger;
        }

        [CanBeNull]
        public async Task<Guid> addProject(string projectName, CancellationToken token)
        {
            var result = await callUnordinalApi(HttpMethod.Post, 
                $"profiles/{userInfoHolder.userInfo.sub}/projects/", 
                new ProjectCreationBody { projectName = projectName }, 
                token: token);
            return JsonSerializer.Deserialize<Guid>(result);
        }

        public async Task<Guid> startProcess(Guid projectID, List<Port> ports, CancellationToken token)
        {
            var body = new StartProcessBody()
            {
                ProjectName = projectID,
                Ports = ports
            };
            var responseContent = await callUnordinalApi(HttpMethod.Post, "hosting/", body, token);

            return Guid.Parse(responseContent.Replace("\"", ""));
        }

        public async Task<string> getUploadUrl(Guid guid, CancellationToken token)
        {
            return await callUnordinalApi(HttpMethod.Post, $"hosting/{guid}/uploadUrl", token: token);
        }

        public async Task<string> buildImage(Guid guid, CancellationToken token)
        {
            return await callUnordinalApi(HttpMethod.Post, $"hosting/{guid}/build", token: token);
        }

        public async Task<StatusMessage> checkBuildStatus(Guid guid)
        {
            var responseContent = await callUnordinalApi(HttpMethod.Get, $"hosting/{guid}/status");
            return toJson<StatusMessage>(responseContent);
        }

        public async Task deploy(Guid guid, Dictionary<string, long> regions, CancellationToken token)
        {
            await callUnordinalApi(HttpMethod.Post, $"hosting/{guid}/deploy", regions, token);
        }

        public async Task<DeployStatusMessage> checkDeployStatus(Guid guid)
        {
            var responseContent = await callUnordinalApi(HttpMethod.Get, $"hosting/{guid}/deployStatus");
            return toJson<DeployStatusMessage>(responseContent);
        }

        private async Task<string> callUnordinalApi(HttpMethod method, string path, object body = null, CancellationToken token = default)
        {
            HttpRequestMessage request = buildRequest(method, path, body);
            var response = await client.SendAsync(request, token);
            return await handleResponse(path, response);
        }

        private async Task<string> handleResponse(string path, HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var c = await response.Content.ReadAsStringAsync();
                logger.LogDebug(c);
                return c;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogDebug("Unauthorized request");
                throw new InvalidOperationException();
            }
            else
            {
                logger.LogDebug(response.ToString());
                throw new ApiException($"Api endpoint {path} returned {response.StatusCode}");
            }
        }

        private HttpRequestMessage buildRequest(HttpMethod method, string path, object body)
        {
            var request = new HttpRequestMessage(method, PluginSettings.ApiBaseUrl + path);
            if (body != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            }

            return request;
        }

        private static T toJson<T>(string responseContent)
        {
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }

        public struct StartProcessBody
        {
            public Guid ProjectName { get; set; }
            public List<Port> Ports { get; set; }
        }

        public struct DeployStatusMessage
        {
            public string Status { get; set; }
            public string Ip { get; set; }
            public List<DeployPort> Ports { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class DeployPort
        {
            public string Protocol { get; set; }
            public int PortProperty { get; set; }

        }

        public struct StatusMessage
        {
            public string Status { get; set; }
            public string ErrorMessage { get; set; }
        }

        public struct ProjectCreationBody
        {
            public string projectName { get; set; }
        }

        public class ApiException : Exception
        {
            public ApiException(string message) : base(message)
            {
            }
        }
    }

    public interface IUserInfoHolder
    {
        UserInfo userInfo { get; set; }
    }
}
