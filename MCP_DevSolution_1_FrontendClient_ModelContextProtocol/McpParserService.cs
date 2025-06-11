using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions; // For a more robust parser later if needed
using System.Text.Json; // For JsonSerializer and JsonException

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class McpParserService
    {
        private const string McpPrefix = "#$#";
        private const string McpToolDefineMessageName = "dns-com-yourorg-tool-define"; // Placeholder

        public McpMessage Parse(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine) || !rawLine.StartsWith(McpPrefix))
            {
                return null;
            }

            string content = rawLine.Substring(McpPrefix.Length);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null; // Invalid MCP message, just prefix
            }

            string messageName;
            string argsContentString;

            int firstSpaceIndex = content.IndexOf(' ');
            if (firstSpaceIndex == -1)
            {
                messageName = content;
                argsContentString = string.Empty;
            }
            else
            {
                messageName = content.Substring(0, firstSpaceIndex);
                argsContentString = content.Substring(firstSpaceIndex + 1).Trim();
            }

            if (string.IsNullOrWhiteSpace(messageName))
            {
                return null; // Invalid MCP message, no name
            }

            McpMessage mcpMsg = new McpMessage(messageName, content); // 'content' is the full string after #$#

            // Simple KVP parser - this can be made more robust, especially for quoted values with escaped quotes.
            // Regex might be better: (\w+)\s*:\s*(?:\"((?:\\\"|[^\"])*)\"|(\S+))
            // This simple parser iterates through space-separated tokens.
            // It expects "key:value" or "key:\"value with spaces\"".
            if (!string.IsNullOrWhiteSpace(argsContentString))
            {
                // This simple split won't handle spaces in unquoted values or complex quoted strings well.
                // For MCP, values with spaces MUST be quoted.
                List<string> parts = new List<string>();
                StringBuilder currentPart = new StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < argsContentString.Length; i++)
                {
                    char c = argsContentString[i];
                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                        currentPart.Append(c); // Keep quotes for now, will trim later
                    }
                    else if (c == ' ' && !inQuotes)
                    {
                        if (currentPart.Length > 0)
                        {
                            parts.Add(currentPart.ToString());
                            currentPart.Clear();
                        }
                    }
                    else
                    {
                        currentPart.Append(c);
                    }
                }
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                }


                foreach (string part in parts)
                {
                    int colonIndex = part.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < part.Length - 1)
                    {
                        string key = part.Substring(0, colonIndex).Trim();
                        string value = part.Substring(colonIndex + 1).Trim();

                        // Trim quotes if they are the first and last characters
                        if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                            // Further unescaping of \" inside value could be done here if necessary
                        }
                        // No support for single quotes as per common MCP spec, but could be added.

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            mcpMsg.AddArgument(key, value);
                        }
                    }
                }
            }

            if (messageName.Equals(McpToolDefineMessageName, StringComparison.OrdinalIgnoreCase))
            {
                string toolName = mcpMsg.GetArgument("name");
                string toolDescription = mcpMsg.GetArgument("description");
                string paramsJson = mcpMsg.GetArgument("parameters");

                if (!string.IsNullOrWhiteSpace(toolName) && !string.IsNullOrWhiteSpace(toolDescription) && !string.IsNullOrWhiteSpace(paramsJson))
                {
                    try
                    {
                        var parameters = JsonSerializer.Deserialize<List<McpToolParameter>>(paramsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (parameters != null)
                        {
                            mcpMsg.DefinedTool = new McpTool
                            {
                                Name = toolName,
                                Description = toolDescription,
                                Parameters = parameters
                            };
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Log error: failed to parse tool parameters JSON
                        System.Diagnostics.Debug.WriteLine($"MCP Parser: Failed to deserialize parameters for tool '{toolName}'. JSON: {paramsJson}. Error: {ex.Message}");
                    }
                }
            }
            return mcpMsg;
        }

        public string Format(McpMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.MessageName))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(McpPrefix);
            sb.Append(message.MessageName);

            if (message.Arguments != null && message.Arguments.Count > 0)
            {
                foreach (var kvp in message.Arguments)
                {
                    sb.Append(" ");
                    sb.Append(kvp.Key);
                    sb.Append(":");

                    string value = kvp.Value ?? string.Empty;
                    // Quote if value is empty, contains spaces, quotes, or colons.
                    // Basic escaping for quotes within the value.
                    if (string.IsNullOrEmpty(value) || value.Contains(" ") || value.Contains("\"") || value.Contains(":"))
                    {
                        sb.Append("\"");
                        sb.Append(value.Replace("\"", "\\\"")); // Basic escaping
                        sb.Append("\"");
                    }
                    else
                    {
                        sb.Append(value);
                    }
                }
            }
            // If there are no arguments, but RawMessageContent differs from MessageName,
            // it might imply a simple MCP command like #$#dm-package-version or similar.
            // The current Format method reconstructs strictly from MessageName and Arguments.
            // If RawMessageContent should be used when Arguments is empty, that logic would go here.
            // However, for well-formed MCP, Arguments should be populated by Parse if there are args.

            return sb.ToString();
        }
    }
}
