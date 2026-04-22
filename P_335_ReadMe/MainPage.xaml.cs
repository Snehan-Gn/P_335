using P_335_ReadMe.Models;
using SQLite;
using System.IO;
using System.Net.Http.Json;
using VersOne.Epub;

namespace P_335_ReadMe
{
    public partial class MainPage : ContentPage
    {
        private SQLiteAsyncConnection? _db;
        private EpubBook? _openedBook;
        private Book? _currentBookRecord;
        private readonly HttpClient _httpClient = new HttpClient();

        public MainPage()
        {
            InitializeComponent();
            InitDatabase();
        }

        private async void InitDatabase()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "readme.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<Book>();

            await SyncWithApi();
            LoadLibrary();
        }

        private async Task SyncWithApi()
        {
            if (_db == null) return;
            try
            {
                // 10.0.2.2 est l'IP pour le localhost du PC depuis l'émulateur
                var booksFromApi = await _httpClient.GetFromJsonAsync<List<Book>>("http://10.0.2.2:3000/books");
                if (booksFromApi != null)
                {
                    foreach (var apiBook in booksFromApi)
                    {
                        var existing = await _db.Table<Book>().Where(b => b.Title == apiBook.Title).FirstOrDefaultAsync();
                        if (existing == null)
                        {
                            // On initialise la date si l'API ne la fournit pas proprement
                            if (apiBook.DateAdded == default) apiBook.DateAdded = DateTime.Now;
                            await _db.InsertAsync(apiBook);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur Sync: {ex.Message}");
            }
        }

        private async void LoadLibrary()
        {
            if (_db == null) return;
            var books = await _db.Table<Book>().OrderByDescending(b => b.DateAdded).ToListAsync();
            BooksCollection.ItemsSource = books;
        }

        private async void OpenBook(Book book)
        {
            _currentBookRecord = book;
            if (book.EpubData == null || book.EpubData.Length == 0)
            {
                await DisplayAlert("Erreur", "Les données du livre sont manquantes.", "OK");
                return;
            }

            using var stream = new MemoryStream(book.EpubData);
            _openedBook = await EpubReader.ReadBookAsync(stream);

            ReaderContainer.IsVisible = true;
            DisplayPage(book.LastPageRead);
        }

        private void DisplayPage(int index)
        {
            if (_openedBook == null || _currentBookRecord == null || _db == null) return;
            if (index < 0 || index >= _openedBook.ReadingOrder.Count) return;

            var chapter = _openedBook.ReadingOrder[index];
            ReaderView.Source = new HtmlWebViewSource { Html = chapter.Content };

            _currentBookRecord.LastPageRead = index;
            _db.UpdateAsync(_currentBookRecord);
        }

        private void OnNextClicked(object sender, EventArgs e) => DisplayPage(_currentBookRecord!.LastPageRead + 1);
        private void OnPreviousClicked(object sender, EventArgs e) => DisplayPage(_currentBookRecord!.LastPageRead - 1);

        private async void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (_db == null) return;
            string filter = e.NewTextValue?.ToLower() ?? "";
            var books = await _db.Table<Book>()
                                 .Where(b => b.Tags.ToLower().Contains(filter) || b.Title.ToLower().Contains(filter))
                                 .ToListAsync();
            BooksCollection.ItemsSource = books;
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Sélectionnez un Epub" });
            if (result != null && _db != null)
            {
                var bytes = File.ReadAllBytes(result.FullPath);
                using var stream = new MemoryStream(bytes);
                var epub = await EpubReader.ReadBookAsync(stream);

                var newBook = new Book
                {
                    Title = epub.Title ?? result.FileName,
                    Author = epub.Author ?? "Auteur inconnu",
                    EpubData = bytes,
                    CoverImage = epub.CoverImage,
                    DateAdded = DateTime.Now
                };
                await _db.InsertAsync(newBook);
                LoadLibrary();
            }
        }

        private void OnBookSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Book selectedBook)
            {
                OpenBook(selectedBook);
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private void OnCloseReaderClicked(object sender, EventArgs e) => ReaderContainer.IsVisible = false;
    }

    public class ByteArrayToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            return null;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => null;
    }
}