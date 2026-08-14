

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeightMaster.Services.Api;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient()
    {
        // ApiAuditHandler records every call into api_post_log. It is inert
        // unless ApiAuditWriter has been started, and never throws, so this
        // behaves exactly as a bare HttpClient when auditing is off.
        _httpClient = new HttpClient(new ApiAuditHandler(new HttpClientHandler()));
    }
    public async Task<T> GetAsync<T>(string url)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine("An error occurred while sending a GET request to {Url}", ex, url);
            throw; 
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("An unexpected error occurred during the GET request to {Url}", ex, url);
            throw; 
        }
    }

    public async Task<T> PostAsync<T>(string url, object data = null)
    {
        try
        {
            string jsonData = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Add the Authorization header with the token
            request.Headers.Add("Authorization", "Token 05f64f326eff436:d27fd60cb19f7d5");


            HttpResponseMessage response = await _httpClient.SendAsync(request);
            System.Diagnostics.Debug.WriteLine(response.ToString());
            response.EnsureSuccessStatusCode();

            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine("An error occurred while sending a POST request to {0} with data {1}: {2}", url, data, ex);
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("An unexpected error occurred during the POST request to {0} with data {1}: {2}", url, data, ex);
            throw;
        }
    }

    public async Task<T> PostAsync<T>(string url, object data = null, JsonSerializerOptions options = null)
    {
        try
        {
            // Serialize the data to JSON
            string jsonData = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Create the HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Add the Authorization header with the token
            request.Headers.Add("Authorization", "Token 05f64f326eff436:d27fd60cb19f7d5");

            // Send the request and get the response
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            System.Diagnostics.Debug.WriteLine(response.ToString());
            response.EnsureSuccessStatusCode();

            // Read the response content
            string responseData = await response.Content.ReadAsStringAsync();

            // Deserialize the response using the provided options (if any)
            return options != null
                ? System.Text.Json.JsonSerializer.Deserialize<T>(responseData, options)  // Deserialize with custom options
                : JsonConvert.DeserializeObject<T>(responseData);      // Fallback to default deserialization
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine("An error occurred while sending a POST request to {0} with data {1}: {2}", url, data, ex);
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("An unexpected error occurred during the POST request to {0} with data {1}: {2}", url, data, ex);
            throw;
        }
    }


}
