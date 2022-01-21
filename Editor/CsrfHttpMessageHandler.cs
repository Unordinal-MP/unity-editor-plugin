﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Unordinal.Hosting
{
    public class CsrfHttpMessageHandler: MessageProcessingHandler
    {
        private const string CsrfCookieName = "CSRF-TOKEN";

        private const string CsrfHeaderName = "X-CSRF-TOKEN";

        private string csrfToken;

        public CsrfHttpMessageHandler(HttpMessageHandler delegateHandler): base(delegateHandler)
        {
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(csrfToken)) {
                request.Headers.Add(CsrfHeaderName, csrfToken);
            }
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var csrfCookie = FindCookie(response, CsrfCookieName);
            if (csrfCookie != null) {
                csrfToken = csrfCookie.Value;
            }
            return response;
        }

        private Cookie FindCookie(HttpResponseMessage response, string cookieName)
        {
            var cookies = new List<Cookie>(
                new CookieCollectionEnumerable(
                    (this.InnerHandler as HttpClientHandler).CookieContainer
                        .GetCookies(response.RequestMessage.RequestUri)));
            return cookies.FirstOrDefault(cookie => cookie.Name == cookieName);
        }

        private class CookieCollectionEnumerable : IEnumerable<Cookie>
        {
            private readonly CookieCollection wrapped;

            public CookieCollectionEnumerable(CookieCollection wrapped)
            {
                this.wrapped = wrapped;
            }

            public IEnumerator<Cookie> GetEnumerator()
            {
                foreach (Cookie elem in wrapped) {
                    yield return elem;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return wrapped.GetEnumerator();
            }
        }
    }

}
