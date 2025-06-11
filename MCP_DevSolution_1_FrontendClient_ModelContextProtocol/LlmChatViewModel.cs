using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input; // For ICommand

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class LlmChatViewModel : ViewModelBase
    {
        private readonly LlmConfigurationViewModel _llmConfigurationViewModel;
        private readonly HttpService _httpService; // To create ILlmService instances
        private readonly Action<string> _logMessageAction;

        private ILlmService _activeLlmService;
        public ILlmService ActiveLlmService => _activeLlmService;

        public ObservableCollection<LlmConfiguration> AvailableLlmConfigs => _llmConfigurationViewModel.LlmConfigs;

        private LlmConfiguration _selectedLlmConfig;
        public LlmConfiguration SelectedLlmConfig
        {
            get => _selectedLlmConfig;
            set
            {
                if (SetProperty(ref _selectedLlmConfig, value))
                {
                    UpdateActiveLlmService();
                    // RaiseCanExecuteChanged for SendCommand is handled by IsSending or UserInput changes as well.
                    // We should ensure SendCommand's CanExecute is re-evaluated if _activeLlmService changes.
                    ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
                    ChatHistory.Clear();
                    _logMessageAction($"INFO: LLM Chat switched to configuration: {SelectedLlmConfig?.ConfigName ?? "None"}");
                }
            }
        }

        public ObservableCollection<ChatMessage> ChatHistory { get; private set; }

        private string _userInput;
        public string UserInput
        {
            get => _userInput;
            set
            {
                if (SetProperty(ref _userInput, value))
                {
                    ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isSending;
        public bool IsSending
        {
            get => _isSending;
            private set // Make private to be controlled by SendCommand logic
            {
                // Use a backing field for SetProperty to correctly compare old/new values.
                // The lambda for dependent property change is not needed if Command's CanExecute directly checks IsSending.
                if (SetProperty(ref _isSending, value))
                {
                    ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand SendCommand { get; }
        public ICommand ClearChatHistoryCommand { get; }


        public LlmChatViewModel(LlmConfigurationViewModel llmConfigurationViewModel, HttpService httpService, Action<string> logMessageAction = null)
        {
            _llmConfigurationViewModel = llmConfigurationViewModel ?? throw new ArgumentNullException(nameof(llmConfigurationViewModel));
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _logMessageAction = logMessageAction ?? ((_) => { });

            ChatHistory = new ObservableCollection<ChatMessage>();
            SendCommand = new RelayCommand(async () => await ExecuteSendCommandAsync(), CanExecuteSendCommand);
            ClearChatHistoryCommand = new RelayCommand(ExecuteClearChatHistory, CanClearChatHistory);

            _llmConfigurationViewModel.LlmConfigs.CollectionChanged += (s, e) => RefreshAvailableConfigs();


            if (AvailableLlmConfigs.Any())
            {
                SelectedLlmConfig = AvailableLlmConfigs.First();
            }
            else
            {
                UpdateActiveLlmService(); // Ensure _activeLlmService is null if no configs initially
            }
        }

        private void UpdateActiveLlmService()
        {
            if (SelectedLlmConfig == null)
            {
                _activeLlmService = null;
                _logMessageAction("INFO: No LLM configuration selected. LLM Chat disabled.");
                return;
            }

            switch (SelectedLlmConfig.ProviderType?.Trim().ToUpperInvariant())
            {
                case "OPENAI-COMPATIBLE":
                case "LMSTUDIO":
                case "OPENAI":
                    _activeLlmService = new OpenAiCompatibleLlmService(_httpService, _logMessageAction);
                    _logMessageAction($"INFO: Active LLM Service set to OpenAI-Compatible for '{SelectedLlmConfig.ConfigName}'.");
                    break;
                default:
                    _logMessageAction($"WARN: No LLM service implementation found for provider type: {SelectedLlmConfig.ProviderType}. LLM Chat will be disabled.");
                    _activeLlmService = null;
                    break;
            }
            OnPropertyChanged(nameof(ActiveLlmService));
        }

        private bool CanExecuteSendCommand(object parameter = null)
        {
            return !string.IsNullOrWhiteSpace(UserInput) && SelectedLlmConfig != null && _activeLlmService != null && !IsSending;
        }

        private async Task ExecuteSendCommandAsync()
        {
            if (!CanExecuteSendCommand()) return;

            IsSending = true;
            var currentUserMessage = new ChatMessage("user", UserInput);
            ChatHistory.Add(currentUserMessage);

            var messagesForLlm = new List<ChatMessage>();
            if (SelectedLlmConfig != null && !string.IsNullOrWhiteSpace(SelectedLlmConfig.SystemPrompt))
            {
                messagesForLlm.Add(new ChatMessage("system", SelectedLlmConfig.SystemPrompt));
            }
            // Add a snapshot of the current history for the request
            foreach(var msg in ChatHistory) messagesForLlm.Add(msg);


            string tempUserInput = UserInput;
            UserInput = string.Empty;

            try
            {
                _logMessageAction($"INFO: Sending message to LLM ({SelectedLlmConfig.ConfigName})...");
                string assistantResponse = await _activeLlmService.SendChatAsync(SelectedLlmConfig, messagesForLlm);

                if (!string.IsNullOrWhiteSpace(assistantResponse))
                {
                    ChatHistory.Add(new ChatMessage("assistant", assistantResponse));
                    _logMessageAction("INFO: LLM response received.");
                }
                else
                {
                    ChatHistory.Add(new ChatMessage("assistant", "[No response or empty response from LLM]"));
                    _logMessageAction("WARN: LLM returned no response or an empty response.");
                }
            }
            catch (Exception ex)
            {
                _logMessageAction($"ERROR: Failed to get response from LLM: {ex.Message}");
                ChatHistory.Add(new ChatMessage("assistant", $"[Error: {ex.Message}]"));
            }
            finally
            {
                IsSending = false;
            }
        }

        private bool CanClearChatHistory(object parameter = null)
        {
            return ChatHistory.Any();
        }

        private void ExecuteClearChatHistory(object parameter = null)
        {
            ChatHistory.Clear();
            _logMessageAction("INFO: LLM Chat history cleared.");
            // No need to call RaiseCanExecuteChanged if ClearChatHistoryCommand's CanExecute depends on ChatHistory.Any()
            // and ChatHistory is an ObservableCollection. However, explicit call is safer if any doubt.
            ((RelayCommand)ClearChatHistoryCommand).RaiseCanExecuteChanged();
        }

        public void RefreshAvailableConfigs()
        {
            OnPropertyChanged(nameof(AvailableLlmConfigs)); // Should not be needed if AvailableLlmConfigs directly references LlmConfigVM.LlmConfigs
            if (SelectedLlmConfig != null && !_llmConfigurationViewModel.LlmConfigs.Contains(SelectedLlmConfig))
            {
                SelectedLlmConfig = _llmConfigurationViewModel.LlmConfigs.FirstOrDefault();
            }
            else if (SelectedLlmConfig == null && _llmConfigurationViewModel.LlmConfigs.Any())
            {
                 SelectedLlmConfig = _llmConfigurationViewModel.LlmConfigs.FirstOrDefault();
            }
             else if (!_llmConfigurationViewModel.LlmConfigs.Any())
            {
                SelectedLlmConfig = null; // No configs available, so select none
            }
            // Ensure CanExecute for SendCommand is re-evaluated after config changes
            ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
        }
    }
}
