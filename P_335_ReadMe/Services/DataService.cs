using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using P_335_ReadMe.Models;

namespace P_335_ReadMe.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string UrlApi = "http://10.0.2.2:3000/books";

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<Book>> FetchBooksAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Book>>(UrlApi) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur : {ex.Message}");
                return new List<Book>();
            }
        }
    }
}
