using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Unordinal.Editor.External;
using Unordinal.Editor.Utils;

namespace Unordinal.Editor
{
    public class RefreshTokenHttpMessageHandler : DelegatingHandler
    {
        public const string SkipAuthorization = "SkipAuthorization";

        private readonly ITokenStorage tokenStorage;
        private readonly Lazy<Auth0Client> auth0;
        private readonly List<string> anonymousUrls;

        private string csrfToken;

        private static bool RefreshForced { get; set; }

        private static readonly AsyncLock Lock = new AsyncLock();

        private CancellationTokenSource requestNewTokenFlow;

        public RefreshTokenHttpMessageHandler(Lazy<Auth0Client> auth0, ITokenStorage tokenHolder, List<string> anonymousUrls, HttpMessageHandler delegateHandler) : base(delegateHandler)
        {
            this.tokenStorage = tokenHolder;
            this.auth0 = auth0;
            this.anonymousUrls = anonymousUrls;
        }
         
        public void ForceRefresh() {
            RefreshForced = true;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (RequiresToken(request) && (!tokenStorage.HasValidToken || RefreshForced))
            {
                if (tokenStorage.RefreshToken == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                using (await Lock.LockAsync())
                {
                    requestNewTokenFlow = new CancellationTokenSource();
                    requestNewTokenFlow.CancelAfter(2000);
                    await RequestNewToken(requestNewTokenFlow.Token);
                    requestNewTokenFlow.Dispose();
                }
                RefreshForced = false;
            }

            if (RequiresToken(request) && !string.IsNullOrWhiteSpace(tokenStorage.Token))
            {
                request.Headers.Add("Authorization", $"Bearer {tokenStorage.Token}");
            }

            var result = new HttpResponseMessage();
            try
            {
                result = await base.SendAsync(request, cancellationToken);
            }
            catch
            {
                // No connection could be made.
                // We end up here if the endpoint doesn't exist.
                result.StatusCode = HttpStatusCode.NotFound;
            }
            
            return result;
        }

        private bool RequiresToken(HttpRequestMessage request)
        {
            object skipAuthorization;
            if (!request.Properties.TryGetValue(SkipAuthorization, out skipAuthorization)) {
                skipAuthorization = false;
            }
            return !anonymousUrls.Contains(request.RequestUri.AbsoluteUri) && !(bool)skipAuthorization;
        }

        private async Task RequestNewToken(CancellationToken token = default)
        {
            await auth0.Value.refreshToken(token);
        }
    }

    public abstract class ITokenStorage
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpirationDate { get; set; }

        public bool HasValidToken => !string.IsNullOrWhiteSpace(Token) && TokenExpirationDate > DateTime.Now;

        public ITokenStorage populateFrom(TokenInfo tokenInfo)
        {
            Token = tokenInfo.access_token;
            RefreshToken = tokenInfo.refresh_token;
            TokenExpirationDate = DateTime.Now.AddSeconds(tokenInfo.expires_in);
            return this;
        }

        #region EditorPrefs

        public void Save()
        {
            EditorPrefs.SetString(UnordinalKeys.tokenKey, Token);

            EditorPrefs.SetString(UnordinalKeys.refreshTokenKey, RefreshToken);

            var json = JsonSerializer.Serialize(TokenExpirationDate);
            EditorPrefs.SetString(UnordinalKeys.tokenExpirationDateKey, json);
        }

        public void Load()
        {
            if (EditorPrefs.HasKey(UnordinalKeys.tokenKey))
            {
                Token = EditorPrefs.GetString(UnordinalKeys.tokenKey);
            }
            if (EditorPrefs.HasKey(UnordinalKeys.refreshTokenKey))
            {
                RefreshToken = EditorPrefs.GetString(UnordinalKeys.refreshTokenKey);
            }
            if (EditorPrefs.HasKey(UnordinalKeys.tokenExpirationDateKey))
            {
                var json = EditorPrefs.GetString(UnordinalKeys.tokenExpirationDateKey);
                try
                {
                    TokenExpirationDate = JsonSerializer.Deserialize<DateTime>(json);
                }
                catch(Exception)
                {
                    // Failed to deserialize the object. (might have been an empty string)
                    // Defaults are automatically used instead.
                }
            }
        }

        #endregion

        public abstract void Clear();
    }
}
