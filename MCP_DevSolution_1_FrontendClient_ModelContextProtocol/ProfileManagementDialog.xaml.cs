using System.Windows;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    /// <summary>
    /// Interaction logic for ProfileManagementDialog.xaml
    /// </summary>
    public partial class ProfileManagementDialog : Window
    {
        public ProfileDialogData FormData { get; private set; }

        public ProfileManagementDialog(ProfileDialogData profileData)
        {
            InitializeComponent();
            FormData = profileData ?? throw new System.ArgumentNullException(nameof(profileData));

            ProfileNameTextBox.Text = FormData.ProfileName;
            ServerHostTextBox.Text = FormData.ServerHost;
            ServerPortTextBox.Text = FormData.ServerPort.ToString();

            if (!FormData.IsNewProfile)
            {
                ProfileNameTextBox.IsEnabled = false; // Do not allow editing name of existing profile
            }
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation can remain for immediate feedback
            if (string.IsNullOrWhiteSpace(ProfileNameTextBox.Text))
            {
                MessageBox.Show("Profile Name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ProfileNameTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(ServerHostTextBox.Text))
            {
                MessageBox.Show("Server Hostname/IP cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ServerHostTextBox.Focus();
                return;
            }

            if (!int.TryParse(ServerPortTextBox.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Server Port must be a valid number between 1 and 65535.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ServerPortTextBox.Focus();
                return;
            }

            // Update FormData before closing
            FormData.ProfileName = ProfileNameTextBox.Text; // Name might be disabled, but read it anyway
            FormData.ServerHost = ServerHostTextBox.Text;
            FormData.ServerPort = port;

            this.DialogResult = true;
            this.Close();
        }

        private void CancelProfileDialogButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
