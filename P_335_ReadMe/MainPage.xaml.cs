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
            
            // On vide la table pour supprimer les anciens livres (Harry Potter, etc.)
            await _db.DropTableAsync<Book>();
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
                        System.Diagnostics.Debug.WriteLine($">>> Nouveau livre API détecté : {apiBook.Title}");
                        
                        apiBook.Id = 0; 

                        if (!string.IsNullOrEmpty(apiBook.CoverImagePath))
                        {
                            apiBook.CoverImage = await _apiService.FetchFileAsync(apiBook.CoverImagePath);
                        }

                        if (!string.IsNullOrEmpty(apiBook.EpubFilePath))
                        {
                            apiBook.EpubData = await _apiService.FetchFileAsync(apiBook.EpubFilePath);
                        }

                        await _db.InsertAsync(apiBook);
                    }
                    else
                    {
                        bool updated = false;

                        if (existing.EpubFilePath != apiBook.EpubFilePath)
                        {
                            existing.EpubFilePath = apiBook.EpubFilePath;
                            updated = true;
                        }
                        if (existing.CoverImagePath != apiBook.CoverImagePath)
                        {
                            existing.CoverImagePath = apiBook.CoverImagePath;
                            updated = true;
                        }

                        if (existing.EpubData == null || existing.EpubData.Length == 0)
                        {
                            if (!string.IsNullOrEmpty(apiBook.EpubFilePath))
                            {
                                existing.EpubData = await _apiService.FetchFileAsync(apiBook.EpubFilePath);
                                updated = true;
                            }
                        }

                        if (existing.CoverImage == null || existing.CoverImage.Length == 0)
                        {
                            if (!string.IsNullOrEmpty(apiBook.CoverImagePath))
                            {
                                existing.CoverImage = await _apiService.FetchFileAsync(apiBook.CoverImagePath);
                                updated = true;
                            }
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
            System.Diagnostics.Debug.WriteLine($">>> Ouverture du livre : {book.Title}");

            if (book.EpubData == null || book.EpubData.Length == 0)
            {
                if (!string.IsNullOrEmpty(book.EpubFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($">>> Données manquantes, tentative de téléchargement immédiat...");
                    book.EpubData = await _apiService.FetchFileAsync(book.EpubFilePath);
                    
                    if (book.EpubData != null && _db != null)
                    {
                        await _db.UpdateAsync(book);
                        System.Diagnostics.Debug.WriteLine($">>> Livre mis à jour dans la base locale.");
                    }
                }
            }

            if (book.EpubData == null || book.EpubData.Length == 0)
            {
                var urlTried = book.EpubFilePath;
                if (!string.IsNullOrEmpty(urlTried) && !urlTried.StartsWith("http"))
                    urlTried = $"http://10.0.2.2:3000/{urlTried.TrimStart('/')}";

                bool pickLocally = await DisplayAlert("Fichier introuvable", 
                    $"Le serveur n'a pas pu fournir le fichier pour '{book.Title}'.\n\n" +
                    "Souhaitez-vous sélectionner le fichier .epub manuellement sur votre appareil pour l'associer à ce livre ?", "Oui", "Non");

                if (pickLocally)
                {
                    var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Sélectionnez le fichier correspondant" });
                    if (result != null)
                    {
                        book.EpubData = await File.ReadAllBytesAsync(result.FullPath);
                        if (_db != null) await _db.UpdateAsync(book);
                        await DisplayAlert("Succès", "Le fichier a été associé et sauvegardé localement.", "OK");
                    }
                    else return;
                }
                else return;
            }

            try
            {
                if (book.EpubData == null)
                {
                    await DisplayAlert("Erreur", "Données du livre non disponibles.", "OK");
                    return;
                }
                using var stream = new MemoryStream(book.EpubData);
                _openedBook = await EpubReader.ReadBookAsync(stream);

                ReaderTitleLabel.Text = book.Title;
                BooksCollection.IsVisible = false;
                ReaderContainer.IsVisible = true;

                DisplayPage(book.LastPageRead);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ERREUR Lecture Epub : {ex.Message}");
                await DisplayAlert("Erreur", "Impossible de lire l'Epub (fichier corrompu ou format invalide) : " + ex.Message, "OK");
            }
        }

        private async void DisplayPage(int index)
        {
            if (_openedBook == null || _currentBookRecord == null || _db == null) return;
            if (index < 0 || index >= _openedBook.ReadingOrder.Count) return;

            var chapter = _openedBook.ReadingOrder[index];
            ReaderView.Source = new HtmlWebViewSource { Html = chapter.Content };

            PageIndicatorLabel.Text = $"Chapitre {index + 1} / {_openedBook.ReadingOrder.Count}";

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
                                 .Where(b => (b.Description ?? "").ToLower().Contains(filter) ||
                                             (b.Title ?? "").ToLower().Contains(filter))
                                 .ToListAsync();
            BooksCollection.ItemsSource = books;
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selectionnez un Epub" });
            if (result != null && _db != null)
            {
                try
                {
                    // On affiche une alerte pour dire que le téléchargement commence
                    await DisplayAlert("Importation", "Envoi du livre vers votre bibliothèque en ligne...", "OK");
                    
                    var uploadedBook = await _apiService.UploadBookAsync(result.FullPath);
                    
                    if (uploadedBook != null)
                    {
                        await DisplayAlert("Succès", $"'{uploadedBook.Title}' a été ajouté à votre bibliothèque.", "OK");
                        // On relance une synchronisation pour récupérer le nouveau livre localement
                        await SyncWithApi();
                    }
                    else
                    {
                        await DisplayAlert("Erreur", "Impossible d'envoyer le livre au serveur.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur", "Une erreur est survenue lors de l'importation : " + ex.Message, "OK");
                }
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Remove("jwt_token");
            if (Application.Current != null)
                Application.Current.MainPage = new LoginPage();
        }

        private void OnBookTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Book selectedBook)
            {
                OpenBook(selectedBook);
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