using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.Services.Authentication;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Newtonsoft.Json;

namespace BBIHardwareSupport.MDM.WorkspaceOneManager.Services;
public class WorkspaceOneAuthService : AuthServiceBase, IWorkspaceOneAuthService
{
    private string _username;
    private string _password;
    private string _cachedToken;
    private DateTime _tokenExpiry;
    private string _baseUri;

    public string BaseUri => _baseUri;
    public string Username => _username;
    public string Password => _password;
    private readonly HttpClient _httpClient;
    private string? _bearerToken;
    private string _apiKey;

    public WorkspaceOneAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _baseUri = "https://as863.awmdm.com/API";
    }

    public string? BearerToken => _bearerToken;


    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        var payload = new
        {
            username,
            password
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://as863.awmdm.com/API/system/users/login", content);

        if (!response.IsSuccessStatusCode)
            return false;

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);

        if (tokenObj != null && tokenObj.TryGetValue("token", out var token))
        {
            _bearerToken = token;
            return true;
        }

        return false;
    }



    public void SetCredentials(string username, string password, string apiKey)
    {
        _username = username;
        _password = password;
        var raw = $"{_username}:{_password}"; // Use raw credentials for Basic Auth
        _apiKey = apiKey;
        _cachedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
        _tokenExpiry = DateTime.MaxValue; // no expiration expected
    }

    public override async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        // 🔐 Replace with actual API token request logic
        var tokenResponse = await RequestNewTokenAsync(_username, _password);

        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddMinutes(tokenResponse.ExpiresInMinutes - 1);

        return _cachedToken;
    }

    private async Task<(string AccessToken, int ExpiresInMinutes)> RequestNewTokenAsync(string user, string pass)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://as863.awmdm.com/API/system/users/login");

        var payload = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("username", user),
        new KeyValuePair<string, string>("password", pass),
        new KeyValuePair<string, string>("grant_type", "password")
    });


        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        request.Content = content;
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Auth failed: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenObj = JsonConvert.DeserializeObject<TokenResponse>(json);

        return (tokenObj.access_token, tokenObj.expires_in / 60);
    }

    // Example token response model
    private class TokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(_username)
        && !string.IsNullOrWhiteSpace(_password);

    public Uri GetBaseUri()
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Workspace ONE credentials are not set.");

        return new Uri(_baseUri!);
    }
    public override async Task<Dictionary<string, string>> GetAuthorizationHeaderAsync()
    {
        var raw = $"{_username}:{_password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

        return new Dictionary<string, string>
    {
        { "Authorization", $"Basic {encoded}" },
        { "aw-tenant-code", _apiKey }, // stored during SetCredentials(...)
        { "Accept", "application/json" }
    };
    }

    public Task SetCredentialsAsync(string username, string password, string tenantCode)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetBearerTokenAsync()
    {
        throw new NotImplementedException();
    }

    public string GetTenantCode()
    {
        throw new NotImplementedException();
    }
}
