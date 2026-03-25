namespace P_335
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var decks = new List<Deck>
        {
            new Deck { Name = "Deck 1" },
            new Deck { Name = "Deck 1" },
            new Deck { Name = "Deck 1" }
        };

            DecksCollection.ItemsSource = decks;
        }
    }

    public class Deck
    {
        public string Name { get; set; }
    }

}
