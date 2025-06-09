using System;
using System.Collections.Generic;
using System.Text; // For StringBuilder

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class McpMessage
    {
        public string MessageName { get; set; }
        public Dictionary<string, string> Arguments { get; private set; }
        public string RawMessageContent { get; private set; } // The original string after #$# (message-name and args-string)

        public McpMessage(string messageName, string rawContent = null)
        {
            MessageName = messageName ?? throw new ArgumentNullException(nameof(messageName));
            // rawContent should be the full string *after* the initial #$#
            RawMessageContent = rawContent ?? messageName; // If no further content, raw is just the name
            Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Convenience method to get an argument
        public string GetArgument(string key)
        {
            Arguments.TryGetValue(key, out string value);
            return value;
        }

        // Convenience method to add an argument (primarily for formatting by McpParserService)
        public void AddArgument(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            Arguments[key] = value ?? string.Empty;
        }

        public override string ToString()
        {
            // Primarily for debugging. For network format, use McpParserService.Format()
            StringBuilder sb = new StringBuilder();
            sb.Append($"#$#{MessageName}");

            // Reconstruct argument string from Arguments dictionary for consistency in debug output
            if (Arguments.Count > 0)
            {
                foreach (var kvp in Arguments)
                {
                    string value = kvp.Value;
                    // Simple quoting for debug if value contains space or is empty
                    if (string.IsNullOrEmpty(value) || value.Contains(" ") || value.Contains("\"") || value.Contains(":"))
                    {
                        // Basic escaping of quotes within the value for display
                        value = $"\"{value?.Replace("\"", "\\\"")}\"";
                    }
                    sb.Append($" {kvp.Key}:{value}");
                }
            }
            // If there are no arguments but RawMessageContent has more than just the message name,
            // it implies a simple command or non-key:value args. This ToString won't reconstruct that perfectly
            // if RawMessageContent was not parsed into Arguments. The McpParserService.Parse is responsible for that.
            // This ToString is best effort for debugging based on what's been parsed into Arguments.
            return sb.ToString();
        }
    }
}
