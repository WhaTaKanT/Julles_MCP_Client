namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class LlmConfigurationDialogData
    {
        public string ConfigName { get; set; }
        public string ProviderType { get; set; } // e.g., "LMStudio", "OpenAI", "Claude"
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string ModelName { get; set; } // Optional
        public string SystemPrompt { get; set; } // Optional

        public bool IsNewConfig { get; set; } // To differentiate between add and edit mode in the dialog

        public LlmConfigurationDialogData()
        {
            // Initialize with sensible defaults, especially for a new configuration
            ConfigName = "New LLM Configuration";
            ProviderType = "OpenAI-Compatible"; // Default or most common type
            ApiEndpoint = "http://localhost:1234/v1"; // Common LMStudio default
            ApiKey = string.Empty;
            ModelName = string.Empty;
            SystemPrompt = string.Empty;
            IsNewConfig = true;
        }
    }
}
