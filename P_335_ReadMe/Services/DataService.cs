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

        public async Task<List<Book>> FetchBooksAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Book>>(UrlApi) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur API : {ex.Message}");
                return new List<Book>();
            }
        }

        public async Task<byte[]?> FetchFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            try
            {
                // Normalisation de l'URL
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

                // Ajustement pour l'émulateur Android (redirection localhost vers l'hôte)
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    url = url.Replace("127.0.0.1", "10.0.2.2").Replace("localhost", "10.0.2.2");
                }

                System.Diagnostics.Debug.WriteLine($">>> Tentative de téléchargement : {url}");

                var response = await _httpClient.GetAsync(url);
                
                // Si non trouvé, on tente plusieurs préfixes courants
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound && !filePath.StartsWith("http"))
                {
                    var prefixes = new[] { "uploads", "public", "static", "books", "files" };
                    var cleanPath = filePath.TrimStart('/').Replace("\\", "/");

                    foreach (var prefix in prefixes)
                    {
                        url = $"{BaseUrl}/{prefix}/{cleanPath}";
                        if (DeviceInfo.Platform == DevicePlatform.Android)
                            url = url.Replace("127.0.0.1", "10.0.2.2").Replace("localhost", "10.0.2.2");

                        System.Diagnostics.Debug.WriteLine($">>> Tentative alternative ({prefix}) : {url}");
                        response = await _httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode) break;
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync();
                    System.Diagnostics.Debug.WriteLine($">>> Succès : {data.Length} octets téléchargés");
                    return data;
                }

                System.Diagnostics.Debug.WriteLine($">>> Échec définitif : {url} (Statut: {response.StatusCode})");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ERREUR critique téléchargement : {ex.Message}");
                return null;
            }
        }
    }
}