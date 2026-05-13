using System.Net.Http.Json;
using System.Text.Json.Serialization;
using P_335_ReadMe.Models;

namespace P_335_ReadMe.Services
{
    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public class ApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        private static string BaseUrl => 
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:3000" : "http://127.0.0.1:3000";
            
        public static string UrlApi => $"{BaseUrl}/books";

        public async Task<string?> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", new { email, password });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return result?.Token;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur Login : {ex.Message}");
            }
            return null;
        }

        public async Task<bool> RegisterAsync(string username, string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", new { username, email, password });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur Register : {ex.Message}");
                return false;
            }
        }

        private void AddAuthHeader()
        {
            var token = Preferences.Get("jwt_token", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<Book?> UploadBookAsync(string filePath)
        {
            try
            {
                AddAuthHeader();
                using var content = new MultipartFormDataContent();
                
                var fileStream = File.OpenRead(filePath);
                var streamContent = new StreamContent(fileStream);
                
                // On s'assure que le Content-Type est correct pour un EPUB
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/epub+zip");
                
                // Le nom du champ doit correspondre exactement à celui attendu par multer: 'epub_file'
                content.Add(streamContent, "epub_file", Path.GetFileName(filePath));

                System.Diagnostics.Debug.WriteLine($">>> Uploading to: {BaseUrl}/books");
                
                var response = await _httpClient.PostAsync($"{BaseUrl}/books", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Book>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($">>> Upload Failed: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur Upload : {ex.Message}");
            }
            return null;
        }

        public async Task<List<Book>> FetchBooksAsync()
        {
            try
            {
                AddAuthHeader();
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
                AddAuthHeader();
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