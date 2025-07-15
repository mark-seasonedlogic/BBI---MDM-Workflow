using Microsoft.UI.Xaml.Controls;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class WorkspaceOneLoginDialog : ContentDialog
    {
        public WorkspaceOneLoginDialog()
        {
            this.InitializeComponent();
        }

        public string Username => UsernameTextBox.Text.Trim();
        public string Password => PasswordBox.Password;
        public string ApiKey => ApiKeyTextBox.Text.Trim();

        public WorkspaceOneCredentials EnteredCredentials => new()
        {
            Username = this.Username,
            Password = this.Password,
            ApiKey = this.ApiKey
        };
    }

    public class WorkspaceOneCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }
    }
}
