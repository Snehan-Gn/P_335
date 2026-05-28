using SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace P_335_ReadMe.Models
{
    public class ApiCategory
    {
        [JsonPropertyName("category_id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Book
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [JsonPropertyName("book_id")]
        public int ApiBookId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? EpubFilePath { get; set; }

        public byte[]? EpubData { get; set; }

        public DateTime DateAdded { get; set; }

        [JsonPropertyName("uploaded_at")]
        [Ignore]
        public string? UploadedAt { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("average_rating")]
        [Ignore]
        public string? AverageRating { get; set; }

        public int LastPageRead { get; set; }

        [JsonPropertyName("Categories")]
        [Ignore]
        public List<ApiCategory>? ApiCategories { get; set; }

        public string? TagsJson { get; set; }

        [Ignore]
        public List<string> Tags =>
            string.IsNullOrEmpty(TagsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new();

        [Ignore]
        public bool HasEpubData => EpubData != null && EpubData.Length > 0;
    }
}
