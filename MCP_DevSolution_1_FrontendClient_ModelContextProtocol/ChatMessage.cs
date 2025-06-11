using System.Text.Json.Serialization; // For JsonSerializer attributes if needed

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } // "system", "user", or "assistant"

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ToolCallId { get; set; }

        public ChatMessage(string role, string content, string toolCallId = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
        }
    }
}
