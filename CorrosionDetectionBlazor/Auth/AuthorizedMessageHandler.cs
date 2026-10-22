using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace CorrosionDetectionBlazor.Auth
{
    public class AuthorizedMessageHandler : DelegatingHandler
    {
        private readonly IJSRuntime _js;

        public AuthorizedMessageHandler(IJSRuntime js)
        {
            _js = js;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await _js.InvokeAsync<string>("authStorage.getToken");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving token: {ex.Message}");
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}