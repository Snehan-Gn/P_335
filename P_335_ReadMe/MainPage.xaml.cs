using P_335_ReadMe.Models;
using P_335_ReadMe.Services;
using SQLite;
using System.IO;
using System.Text.Json;
using VersOne.Epub;

namespace P_335_ReadMe
{
    public partial class MainPage : ContentPage
    {
        private SQLiteAsyncConnection? _db;
        private EpubBook? _openedBook;
        private Book? _currentBookRecord;
        private readonly ApiService _apiService = new ApiService();
        private string? _activeTagFilter;

        private enum SortMode { DateDesc, DateAsc, TitleAsc, TitleDesc }
        private SortMode _sortMode = SortMode.DateDesc;

        public MainPage()
        {
            InitializeComponent();
            InitDatabase();
        }

        private async void InitDatabase()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "readme.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.DropTableAsync<Book>();
            await _db.CreateTableAsync<Book>();
            await SyncWithApi();
        }

        private async Task SyncWithApi()
        {
            if (_db == null) return;
            try
            {
                var booksFromApi = await _apiService.FetchBooksAsync();

                foreach (var apiBook in booksFromApi)
                {
                    if (DateTime.TryParse(apiBook.UploadedAt, out var dt))
                        apiBook.DateAdded = dt;
                    else
                        apiBook.DateAdded = DateTime.Now;

                    apiBook.TagsJson = apiBook.ApiCategories != null
                        ? JsonSerializer.Serialize(apiBook.ApiCategories.Select(c => c.Name).ToList())
                        : null;

                    var existing = await _db.Table<Book>()
                                           .Where(b => b.ApiBookId == apiBook.ApiBookId)
                                           .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        if (!string.IsNullOrEmpty(apiBook.EpubFilePath))
                            apiBook.EpubData = await _apiService.FetchFileAsync(apiBook.EpubFilePath);

                        await _db.InsertAsync(apiBook);
                    }
                    else
                    {
                        bool updated = false;

                        if (existing.TagsJson != apiBook.TagsJson)
                        {
                            existing.TagsJson = apiBook.TagsJson;
                            updated = true;
                        }

                        if (existing.EpubFilePath != apiBook.EpubFilePath)
                        {
                            existing.EpubFilePath = apiBook.EpubFilePath;
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

                        if (updated) await _db.UpdateAsync(existing);
                    }
                }
            }
            catch { }
            finally
            {
                MainThread.BeginInvokeOnMainThread(LoadLibrary);
            }
        }

        private async void LoadLibrary()
        {
            if (_db == null) return;
            var books = await _db.Table<Book>().OrderByDescending(b => b.DateAdded).ToListAsync();

            var allTags = books.SelectMany(b => b.Tags).Distinct().OrderBy(t => t).ToList();
            UpdateTagFilterBar(allTags);

            if (!string.IsNullOrEmpty(_activeTagFilter))
                books = books.Where(b => b.Tags.Contains(_activeTagFilter)).ToList();

            books = _sortMode switch
            {
                SortMode.DateAsc  => books.OrderBy(b => b.DateAdded).ToList(),
                SortMode.TitleAsc => books.OrderBy(b => b.Title).ToList(),
                SortMode.TitleDesc => books.OrderByDescending(b => b.Title).ToList(),
                _                 => books.OrderByDescending(b => b.DateAdded).ToList(),
            };

            BooksCollection.ItemsSource = books;
        }

        private void UpdateTagFilterBar(List<string> tags)
        {
            TagFilterBar.Children.Clear();
            FilterScrollView.IsVisible = tags.Count > 0;

            foreach (var tag in tags)
            {
                bool isActive = tag == _activeTagFilter;
                var chip = new Frame
                {
                    BackgroundColor = isActive ? Color.FromArgb("#512BD4") : Color.FromArgb("#EEE8FF"),
                    CornerRadius = 15,
                    Padding = new Thickness(12, 5),
                    HasShadow = false,
                    BorderColor = Colors.Transparent,
                };
                chip.Content = new Label
                {
                    Text = tag,
                    FontSize = 12,
                    TextColor = isActive ? Colors.White : Color.FromArgb("#512BD4"),
                    VerticalOptions = LayoutOptions.Center,
                };
                var captured = tag;
                chip.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _activeTagFilter = _activeTagFilter == captured ? null : captured;
                        LoadLibrary();
                    }),
                });
                TagFilterBar.Children.Add(chip);
            }
        }

        private async void OpenBook(Book book)
        {
            _currentBookRecord = book;

            if (book.EpubData == null || book.EpubData.Length == 0)
            {
                if (!string.IsNullOrEmpty(book.EpubFilePath))
                {
                    book.EpubData = await _apiService.FetchFileAsync(book.EpubFilePath);
                    if (book.EpubData != null && _db != null)
                        await _db.UpdateAsync(book);
                }
            }

            if (book.EpubData == null || book.EpubData.Length == 0)
            {
                bool pickLocally = await DisplayAlert("Fichier introuvable",
                    $"Le serveur n'a pas pu fournir le fichier pour '{book.Title}'.\n\nSouhaitez-vous sélectionner le fichier .epub manuellement ?", "Oui", "Non");

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
                FilterScrollView.IsVisible = false;
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
            if (result == null || _db == null) return;

            var (uploadedBook, error) = await _apiService.UploadBookAsync(result.FullPath);
            if (uploadedBook != null)
            {
                await DisplayAlert("Succès", $"'{uploadedBook.Title}' a été ajouté à votre bibliothèque.", "OK");
                await SyncWithApi();
            }
            else
            {
                await DisplayAlert("Erreur", error ?? "Impossible d'envoyer le livre au serveur.", "OK");
            }
        }

        private void OnBookTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Book selectedBook)
                OpenBook(selectedBook);
        }

        private async void OnManageTagsTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not Book book) return;

            var options = new List<string> { "Ajouter un tag" };
            foreach (var tag in book.Tags)
                options.Add($"Supprimer '{tag}'");

            var action = await DisplayActionSheet($"Tags — {book.Title}", "Annuler", null, options.ToArray());
            if (action == null || action == "Annuler") return;

            if (action == "Ajouter un tag")
            {
                var tagName = await DisplayPromptAsync("Nouveau tag", "Nom du tag :", "Ajouter", "Annuler", maxLength: 30);
                if (string.IsNullOrWhiteSpace(tagName)) return;
                var (success, error) = await _apiService.AddTagAsync(book.ApiBookId, tagName.Trim().ToLower());
                if (!success) { await DisplayAlert("Erreur", error, "OK"); return; }
            }
            else if (action.StartsWith("Supprimer '"))
            {
                var tagName = action["Supprimer '".Length..^1];
                var (success, error) = await _apiService.RemoveTagAsync(book.ApiBookId, tagName);
                if (!success) { await DisplayAlert("Erreur", error, "OK"); return; }
            }

            await SyncWithApi();
        }

        private async void OnSortClicked(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Trier par", "Annuler", null,
                "Date ↓ (récent)", "Date ↑ (ancien)", "Titre A → Z", "Titre Z → A");

            _sortMode = action switch
            {
                "Date ↑ (ancien)" => SortMode.DateAsc,
                "Titre A → Z"     => SortMode.TitleAsc,
                "Titre Z → A"     => SortMode.TitleDesc,
                "Date ↓ (récent)" => SortMode.DateDesc,
                _                 => _sortMode,
            };

            LoadLibrary();
        }

        private void OnCloseReaderClicked(object sender, EventArgs e)
        {
            ReaderContainer.IsVisible = false;
            BooksCollection.IsVisible = true;
            LoadLibrary();
        }
    }
}
