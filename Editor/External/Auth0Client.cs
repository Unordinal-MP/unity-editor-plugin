using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unordinal.Editor.Utils;
using Unordinal.Hosting;

namespace Unordinal.Editor.External
{
    public class Auth0Client
    {
        private const string RequiredScopes = "openid offline_access profile";
        private const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";
        private const string RefreshGrantType = "refresh_token";

        private HttpClient client;
        private ITokenStorage tokenStorage;

        public Auth0Client(ITokenStorage tokenStorage, HttpClient client)
        {
            this.tokenStorage = tokenStorage;
            this.client = client;
        }

        public async Task<TokenValidationResponse> isTokenValid()
        {
            var response = await makeRequestAsync(HttpMethod.Get, "/userinfo");
            if (response.IsSuccessStatusCode) {
                var responseContent = await deserializeResponse<UserInfo>(response);
                return new TokenValidationResponse { user = responseContent, valid = true };
            }
            return new TokenValidationResponse { valid = false };
        }

        public async Task<DeviceCodeResponse> getDeviceCode(CancellationToken cancelToken)
        {
            var response = await makeRequestAsync(HttpMethod.Post, "/oauth/device/code", new Dictionary<string, string> {
                { "client_id", PluginSettings.ClientId },
                { "scope", RequiredScopes },
                { "audience", PluginSettings.Audience }
            }, cancellationToken: cancelToken);
            response.EnsureSuccessStatusCode();
            return await deserializeResponse<DeviceCodeResponse>(response);
        }

        public async Task<GetTokenResponse> getToken(string deviceCode, CancellationToken cancellationToken)
        {
            return await TaskHelpers.Retry(() => tryToGetToken(DeviceCodeGrantType, new Dictionary<string, string>() {
                { "device_code", deviceCode },
            }), cancellationToken);
        }

        private async Task<GetTokenResponse> tryToGetToken(string grantType, Dictionary<string, string> extraParams, CancellationToken token = default) {
            var response = await makeRequestAsync(HttpMethod.Post, "/oauth/token", new Dictionary<string, string> {
                        { "client_id", PluginSettings.ClientId },
                        { "grant_type", grantType }
                    }.Concat(extraParams)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    token);
            response.EnsureSuccessStatusCode();
            TokenInfo tokenInfo = await deserializeResponse<TokenInfo>(response);
            tokenStorage.populateFrom(tokenInfo);
            return new GetTokenResponse { tokenInfo = tokenInfo, status = Auth0AuthenticationStatus.SUCCESS };
        }

        public async Task<GetTokenResponse> refreshToken(CancellationToken token = default) {
            try
            {
                var dict = new Dictionary<string, string>() { { "refresh_token", tokenStorage.RefreshToken } };
                return await tryToGetToken(RefreshGrantType, dict, token);
            }
            catch {
                return new GetTokenResponse { status = Auth0AuthenticationStatus.FAILED };
            };
        }


        private async Task<HttpResponseMessage> makeRequestAsync(HttpMethod method, string path, Dictionary<string, string> dict = null, CancellationToken cancellationToken = default)
        {
            var req = new HttpRequestMessage(method, PluginSettings.Auth0BaseUrl + path) { Content = dict != null ? new FormUrlEncodedContent(dict) : null };
            return await client.SendAsync(req, cancellationToken: cancellationToken);
        }

        private async Task<T> deserializeResponse<T>(HttpResponseMessage response)
        {
            var c = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(c, options);
        }
    }

    public class DeviceCodeResponse
    {
        public string device_code { get; set; }
        public string user_code { get; set; }
        public string verification_uri { get; set; }
        public int expires_in { get; set; }
        public int interval { get; set; }
        public string verification_uri_complete { get; set; }
    }

    public enum Auth0AuthenticationStatus
    {
        PENDING,
        FAILED,
        SUCCESS
    }

    public struct TokenValidationResponse
    {
        public bool valid { get; set; }
        public UserInfo? user { get; set; }
    }

    public struct UserInfo
    {
        public string sub { get; set; }
        public string name { get; set; }
    }

    public struct GetTokenResponse {
        public Auth0AuthenticationStatus status { get; set; }
        public TokenInfo? tokenInfo { get; set; }
    }

    public struct TokenInfo
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }
}
