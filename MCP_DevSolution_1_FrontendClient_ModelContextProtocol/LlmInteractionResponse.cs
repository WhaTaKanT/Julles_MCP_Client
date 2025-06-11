using System.Collections.Generic; // For List<LlmToolCallRequest>
using System; // For ArgumentNullException

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public enum LlmResponseType
    {
        Text,
        ToolCall
    }

    public class LlmInteractionResponse
    {
        public LlmResponseType ResponseType { get; private set; }
        public string TextContent { get; private set; }

        // An LLM might request multiple tool calls in parallel
        public List<LlmToolCallRequest> ToolCalls { get; private set; }

        // Private constructor for factory methods
        private LlmInteractionResponse()
        {
            ToolCalls = new List<LlmToolCallRequest>();
        }

        public static LlmInteractionResponse CreateTextResponse(string text)
        {
            return new LlmInteractionResponse
            {
                ResponseType = LlmResponseType.Text,
                TextContent = text
            };
        }

        public static LlmInteractionResponse CreateToolCallResponse(List<LlmToolCallRequest> toolCalls)
        {
            if (toolCalls == null || toolCalls.Count == 0)
                throw new ArgumentNullException(nameof(toolCalls), "Tool calls list cannot be null or empty for a tool call response.");

            return new LlmInteractionResponse
            {
                ResponseType = LlmResponseType.ToolCall,
                ToolCalls = toolCalls
            };
        }
    }
}
