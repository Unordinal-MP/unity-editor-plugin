using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Unordinal.Hosting
{
    public class RefreshTokenHttpMessageHandler : DelegatingHandler
    {
        public const string SkipAuthorization = "SkipAuthorization";

        private readonly ITokenStorage tokenStorage;
        private readonly Lazy<Auth0Client> auth0;
        private readonly List<string> anonymousUrls;

        private string csrfToken;

        private bool refreshForced { get; set; }

        public RefreshTokenHttpMessageHandler(Lazy<Auth0Client> auth0, ITokenStorage tokenHolder, List<string> anonymousUrls) : base(new CsrfHttpMessageHandler(new HttpClientHandler() {
            UseDefaultCredentials = true,
            UseCookies = true
        }))
        {
            var def = new HttpClientHandler();
            this.tokenStorage = tokenHolder;
            this.auth0 = auth0;
            this.anonymousUrls = anonymousUrls;
        }
         
        public void forceRefresh() {
            this.refreshForced = true;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Monitor.Enter(this);
            try
            {
                if (RequiresToken(request) && (!tokenStorage.HasValidToken || refreshForced))
                {
                    if (tokenStorage.refreshToken == null)
                    {
                        return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    }
                    await RequestNewToken();
                    refreshForced = false;
                }
            }
            finally
            {
                Monitor.Exit(this);
            }

            if (RequiresToken(request) && !string.IsNullOrWhiteSpace(tokenStorage.token))
            {
                request.Headers.Add("Authorization", $"Bearer {tokenStorage.token}");
            }


            return await base.SendAsync(request, cancellationToken);
        }

        private bool RequiresToken(HttpRequestMessage request)
        {
            object skipAuthorization;
            if (!request.Properties.TryGetValue(SkipAuthorization, out skipAuthorization)) {
                skipAuthorization = false;
            }
            return !anonymousUrls.Contains(request.RequestUri.AbsoluteUri) && !(bool)skipAuthorization;
        }

        private async Task RequestNewToken()
        {
            await auth0.Value.refreshToken();
        }
    }

    public abstract class ITokenStorage
    {
        public abstract string token { get; set; }
        public abstract string refreshToken { get; set; }
        public abstract DateTime tokenExpirationDate { get; set; }

        public bool HasValidToken
        {
            get
            {
                return !string.IsNullOrWhiteSpace(token) && tokenExpirationDate > DateTime.Now;
            }
        }

        public ITokenStorage populateFrom(TokenInfo tokenInfo)
        {
            token = tokenInfo.access_token;
            refreshToken = tokenInfo.refresh_token;
            tokenExpirationDate = DateTime.Now.AddSeconds(tokenInfo.expires_in);
            return this;
        }

        public abstract void Clear();

        public abstract void Save();

        public abstract void Load();
    }
}
