using System;
using System.Net.Http;

namespace Unordinal.Editor
{
    public static class DelegatingHandlerExtensions
    {

        public static HttpClientHandler GetInnermostHandler(this DelegatingHandler handler)
        {
            DelegatingHandler temp = handler;
            while (temp.InnerHandler is DelegatingHandler)
            {
                temp = temp.InnerHandler as DelegatingHandler;
            }
            return temp.InnerHandler as HttpClientHandler;
        }
    }
}