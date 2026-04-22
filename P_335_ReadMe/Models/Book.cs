using SQLite;
using System;
using System.Text.Json.Serialization;

namespace P_335_ReadMe.Models
{
    public class Book
    {
        [PrimaryKey, AutoIncrement]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        public byte[]? EpubData { get; set; }

        public byte[]? CoverImage { get; set; }

        [JsonPropertyName("uploaded_at")]
        public DateTime DateAdded { get; set; }

        [JsonPropertyName("description")]
        public string Tags { get; set; } = string.Empty;

        public int LastPageRead { get; set; }
    }
}