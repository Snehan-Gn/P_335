namespace P_335_ReadMe
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var token = Preferences.Get("jwt_token", string.Empty);
            if (string.IsNullOrEmpty(token))
            {
                MainPage = new LoginPage();
            }
            else
            {
                MainPage = new AppShell();
            }
        }
    }
}
