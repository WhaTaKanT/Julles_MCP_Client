using System.Windows;
using System.Collections.Generic; // For List of provider types if using ComboBox

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public partial class LlmConfigurationDialog : Window
    {
        public LlmConfigurationDialogData FormData { get; private set; }
        // Optional: Predefined list of provider types for a ComboBox
        // public List<string> ProviderTypes { get; set; }

        public LlmConfigurationDialog(LlmConfigurationDialogData configData)
        {
            InitializeComponent();
            FormData = configData ?? throw new System.ArgumentNullException(nameof(configData));

            // Initialize ProviderTypes for ComboBox if used
            // ProviderTypes = new List<string> { "OpenAI-Compatible", "LMStudio", "OpenAI", "Claude", "HuggingFace" };
            // ProviderTypeComboBox.ItemsSource = ProviderTypes; // Assuming a ComboBox named ProviderTypeComboBox

            // Populate dialog fields from FormData
            ConfigNameTextBox.Text = FormData.ConfigName;
            ProviderTypeTextBox.Text = FormData.ProviderType; // Or ComboBox: ProviderTypeComboBox.SelectedValue = FormData.ProviderType;
            ApiEndpointTextBox.Text = FormData.ApiEndpoint;
            ApiKeyTextBox.Text = FormData.ApiKey;
            ModelNameTextBox.Text = FormData.ModelName;
            SystemPromptTextBox.Text = FormData.SystemPrompt;

            if (!FormData.IsNewConfig)
            {
                // For existing configs, the name might be non-editable to prevent changing the key identifier easily.
                // ConfigNameTextBox.IsEnabled = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Basic Validation (can be expanded)
            if (string.IsNullOrWhiteSpace(ConfigNameTextBox.Text))
            {
                MessageBox.Show("Configuration Name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConfigNameTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(ProviderTypeTextBox.Text)) // Or ComboBox: ProviderTypeComboBox.SelectedValue == null
            {
                MessageBox.Show("Provider Type cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ProviderTypeTextBox.Focus(); // Or ComboBox focus
                return;
            }
            if (string.IsNullOrWhiteSpace(ApiEndpointTextBox.Text))
            {
                // More specific validation for URL format could be added for ApiEndpoint
                MessageBox.Show("API Endpoint cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ApiEndpointTextBox.Focus();
                return;
            }
            // API Key can be optional
            // Model Name can be optional
            // System Prompt can be optional

            // Update FormData with values from dialog
            FormData.ConfigName = ConfigNameTextBox.Text;
            FormData.ProviderType = ProviderTypeTextBox.Text; // Or ComboBox: (string)ProviderTypeComboBox.SelectedValue;
            FormData.ApiEndpoint = ApiEndpointTextBox.Text;
            FormData.ApiKey = ApiKeyTextBox.Text;
            FormData.ModelName = ModelNameTextBox.Text;
            FormData.SystemPrompt = SystemPromptTextBox.Text;

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
