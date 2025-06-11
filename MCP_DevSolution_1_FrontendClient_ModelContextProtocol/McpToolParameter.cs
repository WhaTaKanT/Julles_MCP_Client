using System.Text.Json.Serialization;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class McpToolParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // e.g., "string", "integer", "boolean"

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("is_required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] // Only include if true or if explicitly set
        public bool IsRequired { get; set; } = false;
    }
}
