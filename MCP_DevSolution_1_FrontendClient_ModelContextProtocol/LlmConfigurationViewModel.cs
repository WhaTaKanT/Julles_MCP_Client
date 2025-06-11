using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For MessageBox
using System.Windows.Input;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class LlmConfigurationViewModel : ViewModelBase
    {
        private readonly LlmConfigurationService _llmConfigService;
        private readonly IDialogService _dialogService;
        private readonly Action<string> _logMessageAction; // Optional: For logging status messages to main UI

        public ObservableCollection<LlmConfiguration> LlmConfigs { get; private set; }

        private LlmConfiguration _selectedLlmConfig;
        public LlmConfiguration SelectedLlmConfig
        {
            get => _selectedLlmConfig;
            set
            {
                if (SetProperty(ref _selectedLlmConfig, value))
                {
                    // Update CanExecute for commands that depend on a selection
                    ((RelayCommand)OpenEditConfigDialogCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteConfigCommand).RaiseCanExecuteChanged();
                    // Potentially other actions when selection changes
                }
            }
        }

        public ICommand OpenAddConfigDialogCommand { get; }
        public ICommand OpenEditConfigDialogCommand { get; }
        public ICommand DeleteConfigCommand { get; }

        // Constructor
        public LlmConfigurationViewModel(LlmConfigurationService llmConfigService, IDialogService dialogService, Action<string> logMessageAction = null)
        {
            _llmConfigService = llmConfigService ?? throw new ArgumentNullException(nameof(llmConfigService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); // Add this
            _logMessageAction = logMessageAction ?? ((_) => { });

            LlmConfigs = new ObservableCollection<LlmConfiguration>();

            OpenAddConfigDialogCommand = new RelayCommand(ExecuteOpenAddConfigDialog);
            OpenEditConfigDialogCommand = new RelayCommand(ExecuteOpenEditConfigDialog, CanEditOrDeleteConfig);
            DeleteConfigCommand = new RelayCommand(async () => await ExecuteDeleteConfigAsync(), CanEditOrDeleteConfig);

            // Load configurations asynchronously
            _ = LoadConfigsAsync();
        }

        private async Task LoadConfigsAsync()
        {
            _logMessageAction("Loading LLM configurations...");
            try
            {
                var loadedConfigs = await _llmConfigService.LoadConfigurationsAsync();
                LlmConfigs.Clear();
                foreach (var config in loadedConfigs)
                {
                    LlmConfigs.Add(config);
                }
                _logMessageAction($"Loaded {LlmConfigs.Count} LLM configurations.");
                if (!LlmConfigs.Any())
                {
                     _logMessageAction("INFO: No LLM configurations found or file was empty/corrupt. Create a new one to get started.");
                }
            }
            catch (Exception ex)
            {
                _logMessageAction($"ERROR: Failed to load LLM configurations: {ex.Message}");
                // Avoid direct MessageBox here for better MVVM. Log it. View can decide to show error.
                // MessageBox.Show($"Failed to load LLM configurations: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanEditOrDeleteConfig(object parameter = null)
        {
            return SelectedLlmConfig != null;
        }

        private void ExecuteOpenAddConfigDialog(object parameter = null)
        {
            var dialogData = new LlmConfigurationDialogData { IsNewConfig = true }; // Uses defaults from LlmConfigurationDialogData constructor

            LlmConfigurationDialog dialog = new LlmConfigurationDialog(dialogData);
            dialog.Owner = Application.Current?.MainWindow; // Set owner for proper dialog behavior

            if (dialog.ShowDialog() == true)
            {
                // FormData in dialogData is updated by the dialog on save
                _ = AddNewConfigAsync(dialog.FormData);
            }
        }

        private async Task AddNewConfigAsync(LlmConfigurationDialogData data)
        {
            if (LlmConfigs.Any(c => c.ConfigName.Equals(data.ConfigName, StringComparison.OrdinalIgnoreCase)))
            {
                string errorMsg = $"An LLM configuration with the name '{data.ConfigName}' already exists.";
                _logMessageAction($"ERROR: {errorMsg}");
                _dialogService.ShowError(errorMsg, "Add LLM Config Error");
                return;
            }

            var newConfig = new LlmConfiguration
            {
                ConfigName = data.ConfigName,
                ProviderType = data.ProviderType,
                ApiEndpoint = data.ApiEndpoint,
                ApiKey = data.ApiKey,
                ModelName = data.ModelName,
                SystemPrompt = data.SystemPrompt
            };

            LlmConfigs.Add(newConfig);
            bool success = await _llmConfigService.SaveConfigurationsAsync(LlmConfigs.ToList());
            if (success)
            {
                _logMessageAction($"INFO: LLM configuration '{newConfig.ConfigName}' added successfully.");
                SelectedLlmConfig = newConfig; // Select the newly added config
            }
            else
            {
                string errorMsg = $"Failed to save new LLM configuration '{newConfig.ConfigName}'.";
                _logMessageAction($"ERROR: {errorMsg}");
                _dialogService.ShowError(errorMsg + " The configuration list may be out of sync.", "Save Error");
            }
        }

        private void ExecuteOpenEditConfigDialog(object parameter = null)
        {
            if (SelectedLlmConfig == null) return;

            var configToEdit = SelectedLlmConfig.Clone();

            var dialogData = new LlmConfigurationDialogData
            {
                ConfigName = configToEdit.ConfigName,
                ProviderType = configToEdit.ProviderType,
                ApiEndpoint = configToEdit.ApiEndpoint,
                ApiKey = configToEdit.ApiKey,
                ModelName = configToEdit.ModelName,
                SystemPrompt = configToEdit.SystemPrompt,
                IsNewConfig = false
            };

            LlmConfigurationDialog dialog = new LlmConfigurationDialog(dialogData);
            dialog.Owner = Application.Current?.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                _ = UpdateConfigAsync(SelectedLlmConfig.ConfigName, dialog.FormData);
            }
        }

        private async Task UpdateConfigAsync(string originalConfigName, LlmConfigurationDialogData data)
        {
            var configToUpdate = LlmConfigs.FirstOrDefault(c => c.ConfigName.Equals(originalConfigName, StringComparison.OrdinalIgnoreCase));
            if (configToUpdate == null)
            {
                 string errorMsg = $"Could not find LLM configuration '{originalConfigName}' to update.";
                _logMessageAction($"ERROR: {errorMsg}");
                 _dialogService.ShowError(errorMsg + " It might have been deleted or renamed externally.", "Update Error");
                return;
            }

            if (!configToUpdate.ConfigName.Equals(data.ConfigName, StringComparison.OrdinalIgnoreCase) &&
                LlmConfigs.Any(c => c != configToUpdate && c.ConfigName.Equals(data.ConfigName, StringComparison.OrdinalIgnoreCase)))
            {
                string errorMsg = $"Another LLM configuration with the name '{data.ConfigName}' already exists.";
                _logMessageAction($"ERROR: {errorMsg}");
                _dialogService.ShowError(errorMsg + " Please choose a different name.", "Update LLM Config Error");
                return;
            }

            configToUpdate.ConfigName = data.ConfigName;
            configToUpdate.ProviderType = data.ProviderType;
            configToUpdate.ApiEndpoint = data.ApiEndpoint;
            configToUpdate.ApiKey = data.ApiKey;
            configToUpdate.ModelName = data.ModelName;
            configToUpdate.SystemPrompt = data.SystemPrompt;

            bool success = await _llmConfigService.SaveConfigurationsAsync(LlmConfigs.ToList());
            if (success)
            {
                _logMessageAction($"INFO: LLM configuration '{configToUpdate.ConfigName}' updated successfully.");
            }
            else
            {
                string errorMsg = $"Failed to save updates for LLM configuration '{configToUpdate.ConfigName}'.";
                _logMessageAction($"ERROR: {errorMsg}");
                 _dialogService.ShowError(errorMsg + " The configuration list may be out of sync.", "Save Error");
            }
        }

        private async Task ExecuteDeleteConfigAsync()
        {
            if (SelectedLlmConfig == null) return;
            string configNameToDelete = SelectedLlmConfig.ConfigName;

            if (_dialogService.ShowConfirmation($"Are you sure you want to delete the LLM configuration '{configNameToDelete}'?", "Delete LLM Configuration"))
            {
                LlmConfigs.Remove(SelectedLlmConfig);
                bool success = await _llmConfigService.SaveConfigurationsAsync(LlmConfigs.ToList());
                if (success)
                {
                    _logMessageAction($"INFO: LLM configuration '{configNameToDelete}' deleted successfully.");
                }
                else
                {
                    string errorMsg = $"Failed to save deletion of LLM configuration '{configNameToDelete}'.";
                    _logMessageAction($"ERROR: {errorMsg}");
                    _dialogService.ShowError(errorMsg + " List might be out of sync.", "Save Error");
                }
            }
        }
    }
}
