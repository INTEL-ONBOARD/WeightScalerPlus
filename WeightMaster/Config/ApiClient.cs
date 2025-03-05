//using System;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using Newtonsoft.Json;

//public class ApiClient
//{
//    private readonly HttpClient _httpClient;

//    public ApiClient()
//    {
//        _httpClient = new HttpClient();
//    }

//    public async Task<T> GetAsync<T>(string url)
//    {
//        HttpResponseMessage response = await _httpClient.GetAsync(url);
//        response.EnsureSuccessStatusCode();
//        string responseData = await response.Content.ReadAsStringAsync();
//        return JsonConvert.DeserializeObject<T>(responseData);
//    }

//    public async Task<T> PostAsync<T>(string url, object data)
//    {
//        string jsonData = JsonConvert.SerializeObject(data);
//        StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
//        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
//        response.EnsureSuccessStatusCode();
//        string responseData = await response.Content.ReadAsStringAsync();
//        return JsonConvert.DeserializeObject<T>(responseData);
//    }
//}

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient()
    {
        _httpClient = new HttpClient();
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

    public async Task<T> PostAsync<T>(string url, object data)
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(data);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine( "An error occurred while sending a POST request to {Url} with data {@Data}", ex, url, data);
            throw; 
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("An unexpected error occurred during the POST request to {Url} with data {@Data}", ex, url, data);
            throw; 
        }
    }
}
