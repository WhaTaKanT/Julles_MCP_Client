using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public interface ILlmService
    {
        string ProviderName { get; }

        Task<LlmInteractionResponse> SendChatAsync(LlmConfiguration config, List<ChatMessage> messages, List<McpTool> tools = null);

        Task<List<string>> GetAvailableModelsAsync(LlmConfiguration config);
    }
}
