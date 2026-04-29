using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using P_335_ReadMe.Models;
using Microsoft.Maui.Devices;

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
                var url = filePath.StartsWith("http") 
                    ? filePath 
                    : $"{BaseUrl}/{filePath.TrimStart('/')}";

                var response = await _httpClient.GetAsync(url);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound && !filePath.StartsWith("http"))
                {
                    url = $"{BaseUrl}/files/{filePath.TrimStart('/')}";
                    response = await _httpClient.GetAsync(url);
                }

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsByteArrayAsync();

                System.Diagnostics.Debug.WriteLine($"Fichier introuvable : {url} ({response.StatusCode})");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur telechargement fichier : {ex.Message}");
                return null;
            }
        }
    }
}