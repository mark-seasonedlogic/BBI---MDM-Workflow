using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne
{
    public abstract class WorkspaceOneServiceBase
    {
        protected readonly HttpClient _httpClient;
        protected readonly IWorkspaceOneAuthService _authService;

        protected WorkspaceOneServiceBase(HttpClient httpClient, IWorkspaceOneAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public void SetAuthenticationCredentials(string username, string password, string apiKey)
        {
            _authService.SetCredentials(username, password, apiKey);
        }

        public async Task<string> GetAccessTokenAsync() =>
            await _authService.GetAccessTokenAsync();

        public string GetLoggedInUsername() =>
            _authService.Username;

        public async Task AddAuthHeaderAsync()
        {
            var headers = await _authService.GetAuthorizationHeaderAsync();

            foreach (var header in headers)
            {
                if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                    _httpClient.DefaultRequestHeaders.Remove(header.Key);

                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        protected async Task<string?> SendRequestAsync(
            string endpoint,
            HttpMethod method,
            HttpContent? content = null,
            string? accept = null)
        {
            var request = new HttpRequestMessage(method, $"{_authService.BaseUri}{endpoint}");

            var authHeaders = await _authService.GetAuthorizationHeaderAsync();

            foreach (var header in authHeaders)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = header.Value.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
                    }
                }
                else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Accept.Clear();
                    if (!String.IsNullOrEmpty(accept))
                    {
                        request.Headers.TryAddWithoutValidation("Accept", accept);
                    }
                    else
                    {
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
                    }

                }
                else
                {
                    if (request.Headers.Contains(header.Key))
                        request.Headers.Remove(header.Key);

                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (content != null)
            {
                request.Content = content;
            }

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[WorkspaceOne] API Error: {response.StatusCode}, Body: {errorBody}");
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[WorkspaceOne] Request exception: {ex.Message}");
                return null;
            }
        }
        protected async Task<string?> PostJsonAsync(
    string endpoint,
    object payload,
    string? accept = null)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await SendRequestAsync(
                endpoint,
                HttpMethod.Post,
                content,
                accept);
        }

        protected async Task<List<JObject>> GetPagedResponseAsync(
            string endpoint,
            string itemType,
            Dictionary<string, string>? queryParams = null,
            string? accept = null)
        {
            var allResults = new List<JObject>();
            int currentPage = 0;
            int pageSize = 0;
            int totalItems = int.MaxValue;

            do
            {
                // Clone existing query parameters or create a new dictionary
                var queryWithPage = new Dictionary<string, string>(queryParams ?? new());

                // Only add "page" if it's not the first page
                if (currentPage > 0)
                {
                    queryWithPage["page"] = currentPage.ToString();
                }

                // Build query string if any parameters exist
                string queryString = queryWithPage.Count > 0
                    ? string.Join("&", queryWithPage.Select(kvp =>
                        $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))
                    : string.Empty;

                // Construct the full URI
                string requestUri = string.IsNullOrWhiteSpace(queryString)
                    ? endpoint
                    : $"{endpoint}?{queryString}";

                // Make the request
                var response = await SendRequestAsync(requestUri, HttpMethod.Get, null, accept);


                if (string.IsNullOrEmpty(response))
                {
                    Debug.WriteLine($"[WorkspaceOne] Empty response on page {currentPage}");
                    break;
                }

                try
                {
                    var jsonResponse = JToken.Parse(response);
                    JArray objectItems = new();

                    if (jsonResponse is JObject obj)
                    {
                        // Extract pagination metadata if not already set
                        if (pageSize == 0)
                            pageSize = int.TryParse(obj.Value<string>("page_size") ?? obj.Value<string>("PageSize"), out var ps) ? ps : 0;

                        if (totalItems == int.MaxValue)
                            totalItems = int.TryParse(obj.Value<string>("TotalResults") ?? obj.Value<string>("Total") ?? obj.Value<string>("total_size"), out var ti) ? ti : 0;

                        // Extract target collection by itemType key
                        if (obj.TryGetValue(itemType, out var token))
                        {
                            switch (token.Type)
                            {
                                case JTokenType.Array:
                                    objectItems = (JArray)token;
                                    break;

                                case JTokenType.Object:
                                    objectItems = new JArray((JObject)token); // wrap in array
                                    break;

                                default:
                                    Debug.WriteLine($"[WorkspaceOne] Unexpected token type '{token.Type}' for itemType '{itemType}'");
                                    break;
                            }
                        }
                    }
                    else if (jsonResponse is JArray arr)
                    {
                        objectItems = arr;
                    }

                    foreach (var item in objectItems)
                    {
                        if (item is JObject jobject)
                            allResults.Add(jobject);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WorkspaceOne] JSON parsing error on page {currentPage}: {ex.Message}");
                }


                currentPage++;

            } while ((currentPage - 1) * pageSize < totalItems);

            return allResults;
        }
    }


}
