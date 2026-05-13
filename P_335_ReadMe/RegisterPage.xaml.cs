using P_335_ReadMe.Services;

namespace P_335_ReadMe
{
    public partial class RegisterPage : ContentPage
    {
        private readonly ApiService _apiService = new ApiService();

        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var username = UsernameEntry.Text;
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;
            var confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Erreur", "Veuillez remplir tous les champs.", "OK");
                return;
            }

            if (password != confirmPassword)
            {
                await DisplayAlert("Erreur", "Les mots de passe ne correspondent pas.", "OK");
                return;
            }

            try
            {
                BusyIndicator.IsRunning = true;
                var success = await _apiService.RegisterAsync(username, email, password);

                if (success)
                {
                    await DisplayAlert("Succès", "Compte créé avec succès ! Vous pouvez maintenant vous connecter.", "OK");
                    if (Application.Current != null)
                        Application.Current.MainPage = new LoginPage();
                }
                else
                {
                    await DisplayAlert("Erreur", "L'inscription a échoué. L'email est peut-être déjà utilisé ou le serveur ne répond pas.", "OK");
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

        private void OnBackToLoginClicked(object sender, EventArgs e)
        {
            if (Application.Current != null)
                Application.Current.MainPage = new LoginPage();
        }
    }
}
