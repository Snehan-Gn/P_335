using System.Net.Http.Json;
using P_335_ReadMe.Models;

namespace P_335_ReadMe.Services
{
    public class ApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:3000" : "http://127.0.0.1:3000";

        public static string UrlApi => $"{BaseUrl}/books";

        public async Task<(Book? book, string? error)> UploadBookAsync(string filePath)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileStream = File.OpenRead(filePath);
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/epub+zip");
                content.Add(streamContent, "epub_file", Path.GetFileName(filePath));
                var response = await _httpClient.PostAsync($"{BaseUrl}/books", content);
                if (response.IsSuccessStatusCode)
                {
                    var book = await response.Content.ReadFromJsonAsync<Book>();
                    return (book, null);
                }
                var errorBody = await response.Content.ReadAsStringAsync();
                return (null, $"Erreur {(int)response.StatusCode} : {errorBody}");
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<List<Book>> FetchBooksAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Book>>(UrlApi) ?? new();
            }
            catch
            {
                return new List<Book>();
            }
        }

        public async Task<byte[]?> FetchFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            try
            {
                string url;
                if (filePath.StartsWith("http"))
                {
                    url = filePath;
                }
                else
                {
                    var cleanPath = filePath.TrimStart('/').Replace("\\", "/");
                    url = $"{BaseUrl}/{cleanPath}";
                }

                if (DeviceInfo.Platform == DevicePlatform.Android)
                    url = url.Replace("127.0.0.1", "10.0.2.2").Replace("localhost", "10.0.2.2");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsByteArrayAsync();
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
