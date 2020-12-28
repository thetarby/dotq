using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace dotq.Client
{
    public class LongPollingClient
    {
        private HttpClient _httpClient;
        
        public LongPollingClient(int timeoutInMs=4000)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutInMs);
        }

        public async Task<HttpResponseMessage> Get(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return response;
        }

        public async Task<string> GetAsString(string url)
        {
            using var response = await Get(url);
            using var body = await response.Content.ReadAsStreamAsync();
            
            using var reader = new StreamReader(body);
            string res=await reader.ReadToEndAsync();
            return res;
        }
    }
}