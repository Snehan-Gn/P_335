namespace P_335_ReadMe
{
    public partial class MainPage : ContentPage
    {
        public List<Book> Books { get; set; }

        public MainPage()
        {
            InitializeComponent();

            Books = new List<Book>
        {
            new Book { Title = "Livre 1", Author = "Auteur Nom" },
            new Book { Title = "Livre 2", Author = "Auteur Nom" },
            new Book { Title = "Livre 3", Author = "Auteur Nom" },
            new Book { Title = "Livre 4", Author = "Auteur Nom" }
        };

            BooksCollection.ItemsSource = Books;
        }
    }

    public class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
    }

}
