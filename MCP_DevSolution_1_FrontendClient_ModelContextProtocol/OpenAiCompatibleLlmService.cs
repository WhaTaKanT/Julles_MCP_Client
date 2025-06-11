using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class OpenAiCompatibleLlmService : ILlmService
    {
        private readonly HttpService _httpService;
        private readonly Action<string> _logMessageAction;

        public string ProviderName => "OpenAI-Compatible";

        public OpenAiCompatibleLlmService(HttpService httpService, Action<string> logMessageAction = null)
        {
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _logMessageAction = logMessageAction ?? ((_) => { });
        }

        //region DTOs for OpenAI API
        private class OpenAiTool
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "function";

            [JsonPropertyName("function")]
            public OpenAiFunctionDefinition Function { get; set; }
        }

        private class OpenAiFunctionDefinition
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("parameters")]
            public OpenAiFunctionParameters Parameters { get; set; }
        }

        private class OpenAiFunctionParameters
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "object";

            [JsonPropertyName("properties")]
            public Dictionary<string, OpenAiFunctionParameterProperty> Properties { get; set; }

            [JsonPropertyName("required")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<string> Required { get; set; }

            public OpenAiFunctionParameters()
            {
                Properties = new Dictionary<string, OpenAiFunctionParameterProperty>();
                // Required list is initialized only if there are required parameters.
                // Required = new List<string>();
            }
        }

        private class OpenAiFunctionParameterProperty
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }
        }

        private class OpenAiChatCompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("messages")]
            public List<ChatMessage> Messages { get; set; }

            [JsonPropertyName("temperature")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public float? Temperature { get; set; } = 0.7f;

            [JsonPropertyName("max_tokens")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? MaxTokens { get; set; } = 1500;

            [JsonPropertyName("tools")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<OpenAiTool> Tools { get; set; }

            [JsonPropertyName("tool_choice")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public object ToolChoice { get; set; } // "none", "auto", or {"type": "function", "function": {"name": "my_function"}}
        }

        private class OpenAiChatCompletionResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }

            [JsonPropertyName("created")]
            public long Created { get; set; }

            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }

            [JsonPropertyName("usage")]
            public UsageStats Usage { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public OpenAiResponseMessage Message { get; set; } // Changed from ChatMessage to handle tool_calls

            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; }
        }

        // Represents the 'message' object from an OpenAI choice, which can contain text or tool_calls
        private class OpenAiResponseMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; } // Can be null if tool_calls are present

            [JsonPropertyName("tool_calls")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<OpenAiToolCall> ToolCalls { get; set; }
        }


        private class OpenAiToolCall
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; } // Should be "function"

            [JsonPropertyName("function")]
            public OpenAiToolFunctionCall Function { get; set; }
        }

        private class OpenAiToolFunctionCall
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("arguments")]
            public string Arguments { get; set; } // This is a JSON string
        }

        private class UsageStats
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        private class OpenAiModelListResponse
        {
            [JsonPropertyName("object")]
            public string Object { get; set; }

            [JsonPropertyName("data")]
            public List<OpenAiModelInfo> Data { get; set; }
        }

        private class OpenAiModelInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }

            [JsonPropertyName("created")]
            public long Created { get; set; }

            [JsonPropertyName("owned_by")]
            public string OwnedBy { get; set; }
        }
        //endregion

        public async Task<LlmInteractionResponse> SendChatAsync(LlmConfiguration config, List<ChatMessage> messages, List<McpTool> tools = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (messages == null || !messages.Any()) throw new ArgumentNullException(nameof(messages));

            var requestPayload = new OpenAiChatCompletionRequest
            {
                Messages = messages,
                Model = string.IsNullOrWhiteSpace(config.ModelName) ? null : config.ModelName,
            };

            if (tools != null && tools.Any())
            {
                requestPayload.Tools = tools.Select(mcpTool => new OpenAiTool
                {
                    Type = "function",
                    Function = new OpenAiFunctionDefinition
                    {
                        Name = mcpTool.Name,
                        Description = mcpTool.Description,
                        Parameters = new OpenAiFunctionParameters
                        {
                            Properties = mcpTool.Parameters.ToDictionary(
                                p => p.Name,
                                p => new OpenAiFunctionParameterProperty
                                {
                                    Type = MapMcpTypeToJsonSchemaType(p.Type), // Implement this mapping
                                    Description = p.Description
                                }
                            ),
                            Required = mcpTool.Parameters.Where(p => p.IsRequired).Select(p => p.Name).ToList()
                        }
                    }
                }).ToList();
                if (!requestPayload.Tools.SelectMany(t => t.Function.Parameters.Required).Any())
                {
                    // If there are no required parameters across all tools, set Required to null for cleaner JSON
                    foreach(var tool in requestPayload.Tools) {
                        tool.Function.Parameters.Required = null;
                    }
                }

                requestPayload.ToolChoice = "auto"; // Or "none", or specific tool
            }

            string chatCompletionsUrl = config.ApiEndpoint.TrimEnd('/') + "/chat/completions";

            try
            {
                _logMessageAction($"INFO: Sending chat request to {chatCompletionsUrl}. Model: {requestPayload.Model ?? "Default/Not Specified"}. Messages: {messages.Count}. Tools: {requestPayload.Tools?.Count ?? 0}");

                var response = await _httpService.PostAsync<OpenAiChatCompletionRequest, OpenAiChatCompletionResponse>(
                    chatCompletionsUrl,
                    requestPayload,
                    config.ApiKey);

                if (response?.Choices != null && response.Choices.Any())
                {
                    var firstChoice = response.Choices[0];
                    _logMessageAction($"INFO: Received LLM response. Finish Reason: {firstChoice.FinishReason}. Usage: {response.Usage?.TotalTokens} tokens.");

                    if (firstChoice.Message?.ToolCalls != null && firstChoice.Message.ToolCalls.Any())
                    {
                        var toolCallRequests = firstChoice.Message.ToolCalls.Select(tc => new LlmToolCallRequest
                        {
                            Id = tc.Id,
                            ToolName = tc.Function.Name,
                            ArgumentsJson = tc.Function.Arguments
                        }).ToList();
                        return LlmInteractionResponse.CreateToolCallResponse(toolCallRequests);
                    }
                    else if (firstChoice.Message?.Content != null)
                    {
                        return LlmInteractionResponse.CreateTextResponse(firstChoice.Message.Content.Trim());
                    }
                }

                _logMessageAction("WARN: LLM response was empty, had no choices, or no content/tool_calls in the first choice.");
                return LlmInteractionResponse.CreateTextResponse(string.Empty); // Or handle as error
            }
            catch (Exception ex)
            {
                _logMessageAction($"ERROR: LLM communication failed: {ex.Message}");
                throw new Exception($"LLM API request failed. Provider: {ProviderName}, Endpoint: {config.ApiEndpoint}. Details: {ex.Message}", ex);
            }
        }

        private string MapMcpTypeToJsonSchemaType(string mcpType)
        {
            // Simple mapping, can be expanded
            return mcpType?.ToLowerInvariant() switch
            {
                "string" => "string",
                "integer" => "integer",
                "number" => "number",
                "boolean" => "boolean",
                // MCP might not have array/object types directly for simple params,
                // but if it did, they'd map here.
                _ => "string" // Default to string if unknown
            };
        }

        public async Task<List<string>> GetAvailableModelsAsync(LlmConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            string modelsUrl = config.ApiEndpoint.TrimEnd('/') + "/models";

            try
            {
                _logMessageAction($"INFO: Attempting to fetch available models from {modelsUrl}.");
                var response = await _httpService.GetAsync<OpenAiModelListResponse>(modelsUrl, config.ApiKey);

                if (response?.Data != null)
                {
                    List<string> modelIds = response.Data.Select(m => m.Id).ToList();
                    _logMessageAction($"INFO: Found {modelIds.Count} models.");
                    return modelIds;
                }
                _logMessageAction("INFO: No models found or model listing not supported by this endpoint.");
                return new List<string>();
            }
            catch (Exception ex)
            {
                 _logMessageAction($"WARN: Failed to fetch models (endpoint might not be supported or other error): {ex.Message}");
                return new List<string>();
            }
        }
    }
}
