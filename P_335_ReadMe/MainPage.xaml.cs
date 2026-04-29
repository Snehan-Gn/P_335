using P_335_ReadMe.Models;
using P_335_ReadMe.Services;
using SQLite;
using System.IO;
using VersOne.Epub;

namespace P_335_ReadMe
{
    public partial class MainPage : ContentPage
    {
        private SQLiteAsyncConnection? _db;
        private EpubBook? _openedBook;
        private Book? _currentBookRecord;
        private readonly ApiService _apiService = new ApiService();

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
                System.Diagnostics.Debug.WriteLine($">>> Connexion : {ApiService.UrlApi}");
                var booksFromApi = await _apiService.FetchBooksAsync();
                System.Diagnostics.Debug.WriteLine($">>> API : {booksFromApi.Count} livres trouvés");

                foreach (var apiBook in booksFromApi)
                {
                    if (DateTime.TryParse(apiBook.UploadedAt, out var dt))
                        apiBook.DateAdded = dt;
                    else
                        apiBook.DateAdded = DateTime.Now;

                    var existing = await _db.Table<Book>()
                                           .Where(b => b.Title == apiBook.Title)
                                           .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> Ajout : {apiBook.Title}");
                        if (!string.IsNullOrEmpty(apiBook.CoverImagePath))
                            apiBook.CoverImage = await _apiService.FetchFileAsync(apiBook.CoverImagePath);

                        if (!string.IsNullOrEmpty(apiBook.EpubFilePath))
                            apiBook.EpubData = await _apiService.FetchFileAsync(apiBook.EpubFilePath);

                        await _db.InsertAsync(apiBook);
                    }
                    else if (existing.EpubData == null || existing.EpubData.Length == 0)
                    {
                        // Tentative de récupération des données si manquantes
                        bool updated = false;
                        if (!string.IsNullOrEmpty(apiBook.CoverImagePath) && (existing.CoverImage == null || existing.CoverImage.Length == 0))
                        {
                            existing.CoverImage = await _apiService.FetchFileAsync(apiBook.CoverImagePath);
                            updated = true;
                        }

                        if (!string.IsNullOrEmpty(apiBook.EpubFilePath))
                        {
                            existing.EpubData = await _apiService.FetchFileAsync(apiBook.EpubFilePath);
                            updated = true;
                        }

                        if (updated) await _db.UpdateAsync(existing);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR Sync : {ex.Message}");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(LoadLibrary);
            }
        }

        private async void LoadLibrary()
        {
            try 
            {
                if (_db == null) return;
                var books = await _db.Table<Book>().OrderByDescending(b => b.DateAdded).ToListAsync();
                System.Diagnostics.Debug.WriteLine($">>> Interface : Affichage de {books.Count} livres");
                BooksCollection.ItemsSource = books;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR Load : {ex.Message}");
            }
        }

        private async void OpenBook(Book book)
        {
            _currentBookRecord = book;
            if (book.EpubData == null || book.EpubData.Length == 0)
            {
                await DisplayAlert("Erreur", "Le fichier Epub est vide ou manquant. Vérifiez votre connexion.", "OK");
                return;
            }

            try
            {
                using var stream = new MemoryStream(book.EpubData);
                _openedBook = await EpubReader.ReadBookAsync(stream);

                BooksCollection.IsVisible = false;
                ReaderContainer.IsVisible = true;

                DisplayPage(book.LastPageRead);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", "Impossible de lire l'Epub : " + ex.Message, "OK");
            }
        }

        private async void DisplayPage(int index)
        {
            if (_openedBook == null || _currentBookRecord == null || _db == null) return;
            if (index < 0 || index >= _openedBook.ReadingOrder.Count) return;

            var chapter = _openedBook.ReadingOrder[index];
            ReaderView.Source = new HtmlWebViewSource { Html = chapter.Content };

            _currentBookRecord.LastPageRead = index;
            await _db.UpdateAsync(_currentBookRecord);
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            if (_currentBookRecord != null)
                DisplayPage(_currentBookRecord.LastPageRead + 1);
        }

        private void OnPreviousClicked(object sender, EventArgs e)
        {
            if (_currentBookRecord != null)
                DisplayPage(_currentBookRecord.LastPageRead - 1);
        }

        private async void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (_db == null) return;
            string filter = e.NewTextValue?.ToLower() ?? "";
            var books = await _db.Table<Book>()
                                 .Where(b => (b.Tags ?? "").ToLower().Contains(filter) ||
                                             (b.Title ?? "").ToLower().Contains(filter))
                                 .ToListAsync();
            BooksCollection.ItemsSource = books;
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selectionnez un Epub" });
            if (result != null && _db != null)
            {
                var bytes = await File.ReadAllBytesAsync(result.FullPath);
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

        private void OnCloseReaderClicked(object sender, EventArgs e)
        {
            ReaderContainer.IsVisible = false;
            BooksCollection.IsVisible = true;
        }
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