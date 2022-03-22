using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Unordinal.Editor.Services
{
    public class FileUploader
    {
        private readonly HttpClient client;
        private readonly ILogger<FileUploader> logger;

        public FileUploader(HttpClient client, ILogger<FileUploader> logger) {
            this.client = client;
            this.logger = logger;
        }

        public async Task UploadFile(string url, string filePath, CancellationToken token)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var request = BuildFileUploadRequest(url, stream);
                client.Timeout = Timeout.InfiniteTimeSpan;
                var response = await client.SendAsync(request, token);
                logger.LogDebug(response.Content.ReadAsStringAsync().Result);
                response.EnsureSuccessStatusCode();
            }
        }

        private static HttpRequestMessage BuildFileUploadRequest(string url, FileStream stream)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
#pragma warning disable 0618 // HttpRequestMessage.Properties used as HttpMessageRequest.Options is not available in .Net 4.5
            request.Properties[RefreshTokenHttpMessageHandler.SkipAuthorization] = true;
#pragma warning restore 0618
            var content = new StreamContent(stream);
            content.Headers.Add("x-ms-blob-type", "BlockBlob");
            content.Headers.Add("x-ms-version", "2019-12-12");
            request.Content = content;
            return request;
        }

        
    }
}
