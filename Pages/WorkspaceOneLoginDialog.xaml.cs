using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
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
            Username = UsernameTextBox.Text.Trim(),
            Password = PasswordBox.Password,
            ApiKey = ApiKeyTextBox.Text.Trim(),

            // assuming you already have a selector control:
            Environment = EnvironmentCombo.SelectedIndex == 1
                ? WorkspaceOneEnvironment.QA
                : WorkspaceOneEnvironment.Production
        };
    }

    
}
