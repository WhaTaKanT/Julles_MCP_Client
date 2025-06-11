using System.Collections.Generic; // For potential future use e.g. list of supported features

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class LlmConfiguration : ViewModelBase // Assuming ViewModelBase provides INotifyPropertyChanged
    {
        private string _configName;
        public string ConfigName
        {
            get => _configName;
            set => SetProperty(ref _configName, value);
        }

        private string _providerType; // e.g., "LMStudio", "OpenAI", "Claude", "Gemini"
        public string ProviderType
        {
            get => _providerType;
            set => SetProperty(ref _providerType, value);
        }

        private string _apiEndpoint;
        public string ApiEndpoint
        {
            get => _apiEndpoint;
            set => SetProperty(ref _apiEndpoint, value);
        }

        private string _apiKey;
        public string ApiKey // This should be handled securely, e.g. not directly logged
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        private string _modelName; // Optional, specific model identifier for the provider
        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        private string _systemPrompt; // Optional, system-level instructions for the LLM
        public string SystemPrompt
        {
            get => _systemPrompt;
            set => SetProperty(ref _systemPrompt, value);
        }

        public LlmConfiguration()
        {
            _configName = "New LLM Config";
            _providerType = "OpenAI-Compatible"; // Default to a common type
            _apiEndpoint = "http://localhost:1234/v1"; // Common LMStudio default
            _apiKey = string.Empty;
            _modelName = string.Empty;
            _systemPrompt = string.Empty;
        }

        // Consider adding a method to clone or copy for editing purposes
        public LlmConfiguration Clone()
        {
            // Perform a shallow copy. If LlmConfiguration had complex reference type properties
            // that should also be cloned, a deep copy mechanism would be needed.
            return (LlmConfiguration)this.MemberwiseClone();
        }
    }
}
