# How to Utilize the Model Context Protocol C# SDK

This document provides practical guidance on integrating and using the [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) within your C# WPF client application. It focuses on leveraging the SDK to implement the features outlined in your project's `ProjectRequirements*.md` files.

**Key SDK Resources**:
- SDK GitHub Repository: [https://github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk) (Contains README, samples, and tests)
- SDK API Documentation: [https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html)
- Main NuGet Package: `ModelContextProtocol`

## 1. SDK Installation and Setup

As mentioned in `HowtoMCP_ImplementingClientFeatures.md`, start by installing the primary NuGet package:
```bash
dotnet add package ModelContextProtocol --prerelease
```
You might also include `Microsoft.Extensions.Hosting` if you plan to use dependency injection for managing SDK components, following patterns seen in the SDK's examples.

```csharp
// Example: Basic using statements you might need
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Client.Transport; // For transport options
using ModelContextProtocol.Protocol.Types; // For types like Tool, Content, etc.
using System.Threading;
using System.Collections.Generic; // For Dictionaries
// Potentially using Microsoft.Extensions.DependencyInjection and Microsoft.Extensions.Logging
```

## 2. Core Client Operations: `IMcpClient`

The central piece from the SDK you'll interact with for client operations is the `IMcpClient` interface.

### 2.1. Creating and Connecting an `IMcpClient`
(Corresponds to FR.CONFIG.UI.03 - Profile Management & Connection)

The primary way to get an `IMcpClient` instance is via `McpClientFactory.CreateAsync`. This requires an `IClientTransport`.

**Example: Connecting to a server using `StdioClientTransport`**
(This is common for local MCP servers that run as command-line tools, like those in the SDK samples)

```csharp
async Task<IMcpClient?> ConnectToServerAsync(string profileName, string command, string[] arguments)
{
    // 'command' could be "npx", "python", "path/to/executable.exe"
    // 'arguments' could be ["-y", "@modelcontextprotocol/server-everything"] for an npx server
    var stdioOptions = new StdioClientTransportOptions
    {
        Name = profileName, // Logical name for the transport instance
        Command = command,
        Arguments = arguments,
        WorkingDirectory = System.IO.Directory.GetCurrentDirectory() // Or a relevant path
    };
    var clientTransport = new StdioClientTransport(stdioOptions);

    try
    {
        IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport, CancellationToken.None);
        // Store this mcpClient instance, perhaps in a dictionary keyed by profile name/ID
        // Update UI: Connection successful (FR.CONFIG.UI.02)
        return mcpClient;
    }
    catch (Exception ex)
    {
        // Log error, update UI: Connection failed
        Console.WriteLine($"Failed to connect to '{profileName}': {ex.Message}");
        return null;
    }
}
```
- **Other Transports**: If your MCP servers are network-based (TCP/HTTP), you'll need a different `IClientTransport` implementation. Check the C# SDK documentation or its `src/ModelContextProtocol/Client/Transport` directory for available transports or guidance on creating custom ones if the server uses a standard TCP or HTTP binding for MCP. The `ModelContextProtocol.AspNetCore` package is for *building* HTTP servers, but client-side HTTP transport details would be separate.

### 2.2. Disposing of an `IMcpClient`
When disconnecting or shutting down:
```csharp
async Task DisconnectFromServerAsync(IMcpClient mcpClient)
{
    if (mcpClient != null)
    {
        await mcpClient.DisposeAsync();
        // Update UI: Disconnected (FR.CONFIG.UI.02)
    }
}
```

## 3. Interacting with Server Capabilities

Once connected, you can use the `IMcpClient` instance.

### 3.1. Listing Available Tools
(Useful for dynamic UI, debugging, or advanced scenarios)

```csharp
async Task ListServerToolsAsync(IMcpClient mcpClient)
{
    try
    {
        IReadOnlyList<Tool> tools = await mcpClient.ListToolsAsync(CancellationToken.None);
        foreach (var tool in tools)
        {
            // Display tool.Name, tool.Description, tool.InputSchema in UI or logs
            // (FR.UI.CENTER.01 or FR.LOG.UI.02 for MCP Debug)
            Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error listing tools: {ex.Message}");
    }
}
```

### 3.2. Calling a Tool
(Corresponds to FR.UI.CENTER.02 - Command Input; FR.ADV.ALIAS; FR.ADV.TRIG)

This is the most common operation.
```csharp
async Task<CallToolResponse?> CallSpecificToolAsync(
    IMcpClient mcpClient,
    string toolName,
    Dictionary<string, object?> arguments) // Arguments must match tool's InputSchema
{
    try
    {
        CallToolResponse response = await mcpClient.CallToolAsync(toolName, arguments, CancellationToken.None);

        // Process response.Content
        // Each 'Content' object in response.Content has a 'Type' (e.g., "text", "image")
        // and data (e.g., 'Text', 'ImageUrl', 'Json').
        foreach (var contentItem in response.Content)
        {
            if (contentItem.Type == "text" && contentItem.Text != null)
            {
                // Display contentItem.Text in Main Output (FR.UI.CENTER.01)
                Console.WriteLine($"Server response (text): {contentItem.Text}");
            }
            else if (contentItem.Type == "json" && contentItem.Json != null)
            {
                // Display or process contentItem.Json (which is a JsonElement)
                 Console.WriteLine($"Server response (json): {contentItem.Json.ToString()}");
            }
            // Handle other content types as needed
        }
        return response;
    }
    catch (McpException mcpEx) // Specific exception type from the SDK
    {
        Console.WriteLine($"MCP Tool Call Error ('{toolName}'): {mcpEx.Message}");
        // Potentially inspect mcpEx.ErrorData for more details
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"General Error Calling Tool ('{toolName}'): {ex.Message}");
        return null;
    }
}
```
- **Argument Serialization**: The `arguments` dictionary needs to be structured such that it can be serialized to JSON matching the `InputSchema` of the target MCP Tool. The SDK handles the serialization.
- **Response Content**: `CallToolResponse.Content` is a list of `Content` objects. Your client needs to iterate through this list and handle different content types appropriately. For a typical chat-like client, you'll primarily look for `Type == "text"`.

## 4. Handling Other MCP Features (If Required by Servers)

The MCP specification includes concepts like Resources, Prompts, and Sampling.

- **Resources**: If an MCP server exposes data as "Resources", the SDK would provide methods on `IMcpClient` to query or access them (e.g., `ListResourcesAsync`, `GetResourceAsync`). Refer to the SDK API docs if your client needs to interact with these directly. Often, resources might be implicitly used by Tools.
- **Prompts**: Similar to tools, if servers expose "Prompts", the SDK would have `ListPromptsAsync` and `CallPromptAsync` (or similar). Your client's command input (FR.UI.CENTER.02) could be used to invoke these if needed.
- **Sampling (Server-to-Client requests)**: If an MCP server performs "sampling" (i.e., asks the client/host to get a completion from an LLM), the `IMcpClient` or the `IClientTransport` would need to handle these incoming requests. The SDK's `IMcpServer.AsSamplingChatClient()` (used on the *server* side in SDK examples) hints at this. On the *client* side, you'd need to configure how your application responds to such requests, potentially by forwarding them to an actual LLM if your client has that capability. This is an advanced scenario; check SDK examples or documentation if this is a requirement.

## 5. Logging and Diagnostics with the SDK

The SDK likely uses `Microsoft.Extensions.Logging`. You can integrate this with your client's logging system (UR.LOG).
- When setting up your host or services, configure logging providers (e.g., to a debug output, a file, or your client's "MCP Debug" channel).
- SDK diagnostic messages can then be captured and displayed.

```csharp
// Example: If using Microsoft.Extensions.Hosting
Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole(); // Or your custom WPF log provider
        logging.SetMinimumLevel(LogLevel.Debug); // Adjust as needed
    })
    .ConfigureServices((context, services) =>
    {
        // services.AddSingleton<IMcpClientFactory, McpClientFactory>(); // If needed
        // ... other services
    })
    // ...
```

## 6. Advanced SDK Usage Considerations

- **Cancellation**: Most asynchronous SDK methods accept a `CancellationToken`. Your UI should provide a way to cancel long-running MCP operations (e.g., a "Cancel" button for a tool call).
- **Custom Transports**: If you need to connect to MCP servers over protocols not covered by built-in SDK transports, you might need to implement `IClientTransport`. This is a more involved task requiring good understanding of MCP transport specifications.
- **Extensibility**: The SDK is designed with extensibility in mind (e.g., custom tool invokers, capability handlers). For most client applications, direct use of `IMcpClient` will suffice.

## Summary

The ModelContextProtocol C# SDK provides the necessary tools (`McpClientFactory`, `IMcpClient`, transport options, type definitions) to build a fully featured MCP client. The key is to map your application's UI actions and internal logic (aliases, triggers) to the appropriate SDK calls, primarily `CreateAsync` for connections and `CallToolAsync` for interactions, while handling responses and errors robustly. Always refer to the [official SDK API documentation](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html) for the most detailed and up-to-date information on specific methods and types.
