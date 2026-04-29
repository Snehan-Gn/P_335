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

        [JsonPropertyName("epub_file_path")]
        public string? EpubFilePath { get; set; }

        [JsonPropertyName("cover_image_path")]
        public string? CoverImagePath { get; set; }

        public byte[]? EpubData { get; set; }
        public byte[]? CoverImage { get; set; }

        public DateTime DateAdded { get; set; }

        [JsonPropertyName("uploaded_at")]
        [Ignore]
        public string? UploadedAt { get; set; }

        [JsonPropertyName("description")]
        public string Tags { get; set; } = string.Empty;

        public int LastPageRead { get; set; }
    }
}