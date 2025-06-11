using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class McpTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public List<McpToolParameter> Parameters { get; set; }

        public McpTool()
        {
            Parameters = new List<McpToolParameter>();
        }
    }
}
