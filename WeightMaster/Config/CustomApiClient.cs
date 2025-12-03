using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CustomApiClient
{
    private readonly HttpClient _httpClient;
    private string _accessToken;
    private DateTime _tokenExpiry;

    private const string LoginUrl = "https://api.teacoop.lk/api/v1/thirdPartyLogin";
    private const string Username = "teacoop@codehub.lk";
    private const string Password = "teacoop@1234";

    public CustomApiClient()
    {
        _httpClient = new HttpClient();
    }

    private async Task AuthenticateAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return;

        var credentials = new
        {
            username = Username,
            password = Password
        };

        var content = new StringContent(JsonConvert.SerializeObject(credentials), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(LoginUrl, content);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseData);

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh token 1 minute before expiry
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        await AuthenticateAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return _httpClient;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(responseData);
    }

    public async Task<T> PostAsync<T>(string url, object data)
    {
        var client = await GetAuthenticatedClientAsync();
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(responseData);
    }

    private class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
