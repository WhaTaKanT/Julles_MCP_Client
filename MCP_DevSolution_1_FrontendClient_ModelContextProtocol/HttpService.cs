using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class HttpService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public HttpService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, string apiKey = null)
        {
            try
            {
                var jsonRequest = JsonSerializer.Serialize(data, _jsonSerializerOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    requestMessage.Content = content;
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        return default(TResponse);
                    }
                    return JsonSerializer.Deserialize<TResponse>(jsonResponse, _jsonSerializerOptions);
                }
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP Request Error: {e.Message} for URL {url}. Status Code: {e.StatusCode}");
                throw; // Re-throw to be handled by the calling service
            }
            catch (JsonException e)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Error: {e.Message} for URL {url}");
                throw;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected Error in HttpService PostAsync for URL {url}: {e.Message}");
                throw;
            }
        }

        public async Task<TResponse> GetAsync<TResponse>(string url, string apiKey = null)
        {
            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                     if (string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        return default(TResponse);
                    }
                    return JsonSerializer.Deserialize<TResponse>(jsonResponse, _jsonSerializerOptions);
                }
            }
            catch (HttpRequestException e)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP Request Error: {e.Message} for URL {url}. Status Code: {e.StatusCode}");
                throw;
            }
            catch (JsonException e)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Error: {e.Message} for URL {url}");
                throw;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected Error in HttpService GetAsync for URL {url}: {e.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
