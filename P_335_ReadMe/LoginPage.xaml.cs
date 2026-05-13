using P_335_ReadMe.Services;

namespace P_335_ReadMe
{
    public partial class LoginPage : ContentPage
    {
        private readonly ApiService _apiService = new ApiService();

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Erreur", "Veuillez remplir tous les champs.", "OK");
                return;
            }

            try
            {
                BusyIndicator.IsRunning = true;
                var token = await _apiService.LoginAsync(email, password);

                if (!string.IsNullOrEmpty(token))
                {
                    Preferences.Set("jwt_token", token);
                    if (Application.Current != null)
                        Application.Current.MainPage = new AppShell();
                }
                else
                {
                    await DisplayAlert("Erreur", "Identifiants invalides ou erreur serveur.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", "Une erreur inattendue est survenue : " + ex.Message, "OK");
            }
            finally
            {
                BusyIndicator.IsRunning = false;
            }
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            if (Application.Current != null)
                Application.Current.MainPage = new RegisterPage();
        }
    }
}
