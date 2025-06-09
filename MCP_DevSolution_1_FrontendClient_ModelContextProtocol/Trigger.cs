namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class Trigger
    {
        public string Pattern { get; set; }
        public string ActionType { get; set; } // e.g., "Send Command", "Highlight Line", "Play Sound"
        public string ActionValue { get; set; } // e.g., the command, color, sound file path
        public bool IsEnabled { get; set; }

        public Trigger()
        {
            Pattern = string.Empty;
            ActionType = "Send Command"; // Default action type
            ActionValue = string.Empty;
            IsEnabled = true; // Enabled by default
        }

        // Optional: Constructor for convenience
        public Trigger(string pattern, string actionType, string actionValue, bool isEnabled = true)
        {
            Pattern = pattern;
            ActionType = actionType;
            ActionValue = actionValue;
            IsEnabled = isEnabled;
        }
    }
}
