# How MCP Client-Server Interaction Patterns Work

This document describes typical interaction patterns between your C# WPF client (an MCP Host) and a Model Context Protocol (MCP) compliant server/service. These patterns are based on the official MCP specification and the features defined in your client's `ProjectRequirements*.md` files.

Refer to:
- Official MCP Introduction: [https://modelcontextprotocol.io/introduction](https://modelcontextprotocol.io/introduction)
- C# SDK API Documentation: [https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html)

## 1. Establishing a Connection

**Client Requirements**: FR.CONFIG.UI.01, FR.CONFIG.UI.02, FR.CONFIG.PROFILE.01
**MCP Concepts**: MCP Client, MCP Server, Transport

**Interaction Flow**:
1.  **User Action (Client)**: User selects a server profile from the `Connections` tab (FR.CONFIG.UI.01) and initiates a connection (e.g., clicks a "Connect" button). Profile contains server address (hostname/IP, port - FR.CONFIG.PROFILE.01).
2.  **Client (Internal)**:
    a.  Retrieves server details from the selected profile.
    b.  Constructs an appropriate `IClientTransport` instance based on the server details and the type of MCP server expected (e.g., `StdioClientTransport` for a local console-based server, or a custom/SDK-provided transport for TCP/IP or HTTP connections).
    c.  Calls `IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);` (from the `ModelContextProtocol` C# SDK).
    d.  Updates the connection status for the profile in the UI to "Connecting..." (FR.CONFIG.UI.02).
3.  **SDK & Transport Layer**: The `CreateAsync` call handles the low-level protocol handshake with the MCP server via the specified transport.
4.  **MCP Server**: Responds to the handshake and establishes the connection.
5.  **Client (Internal)**:
    a.  If `CreateAsync` succeeds, the `mcpClient` instance is now active and connected. Store this instance, associating it with the profile.
    b.  Update connection status to "MCP Active" / "Online" (FR.CONFIG.UI.02).
    c.  The client can now potentially list available tools or perform other initial interactions (e.g., `await mcpClient.ListToolsAsync()`).
6.  **Client (Error)**: If `CreateAsync` fails (e.g., server unreachable, handshake error), catch the exception.
    a.  Update connection status to "Offline" or "Error" (FR.CONFIG.UI.02).
    b.  Display an error message to the user (e.g., in the Main Output FR.UI.CENTER.01 or a status bar).

## 2. Invoking a Server-Side Tool (User Command)

**Client Requirements**: FR.UI.CENTER.02 (Command Input)
**MCP Concepts**: Tools, `CallToolAsync`

**Interaction Flow**:
1.  **User Action (Client)**: User types a command into the command input field (FR.UI.CENTER.02) (e.g., `summarize_text "Some long text..."`) and presses Enter.
2.  **Client (Processing)**:
    a.  Retrieves the raw command text.
    b.  (Optional, if aliases are implemented - FR.ADV.ALIAS.01): Processes the text through the alias system, potentially transforming it.
    c.  Parses the (potentially transformed) command to identify the target MCP Tool name (e.g., `summarize_text`) and its arguments (e.g., `{"input_text": "Some long text..."}`). The exact parsing logic is client-defined.
3.  **Client (MCP Interaction)**:
    a.  If an `IMcpClient` is connected:
        ```csharp
        // Assuming 'toolName' is a string like "summarize_text"
        // Assuming 'toolArgs' is a Dictionary<string, object?> or similar
        // serializable to the expected JSON structure for the tool's input schema.
        try
        {
            CallToolResponse response = await mcpClient.CallToolAsync(toolName, toolArgs, CancellationToken.None);
            // Process and display 'response.Content'
            // For example, if response.Content contains text:
            // string responseText = response.Content.FirstOrDefault(c => c.Type == "text")?.Text;
            // Display responseText in Main Output (FR.UI.CENTER.01)
        }
        catch (McpException ex)
        {
            // Display MCP-specific error (ex.Message) in Main Output
        }
        catch (Exception ex)
        {
            // Display general error in Main Output
        }
        ```
4.  **MCP Server**:
    a.  Receives the `CallTool` request.
    b.  Executes the logic for the specified tool (e.g., `summarize_text`) with the provided arguments.
    c.  Sends a `CallToolResponse` back, containing the result (e.g., the summarized text) or an error.
5.  **Client (Display)**: Receives the response and updates the Main Output display (FR.UI.CENTER.01) with the tool's output or any error messages.

## 3. Handling Server-Pushed Messages (If Applicable)

**MCP Concepts**: Transports, Events/Callbacks (SDK specific)

Some MCP transports or server implementations might support pushing messages to the client without an explicit client request. The C# SDK documentation for the specific `IClientTransport` would detail how such messages are received (e.g., via events on the `IMcpClient` or transport).

**Interaction Flow (Conceptual)**:
1.  **MCP Server**: Sends an unsolicited message/event to the client over the active transport.
2.  **Client (SDK/Transport Layer)**: The `IMcpClient` or its underlying transport receives the message.
3.  **Client (Application Logic)**:
    a.  If the SDK provides events for such messages, the client subscribes to them.
    b.  The event handler processes the incoming message.
    c.  The content is typically displayed in the Main Output (FR.UI.CENTER.01) and processed by the Logging System (UR.LOG) for categorization.
    d.  This incoming message may also be evaluated by the Trigger system (FR.ADV.TRIG.01).

## 4. Trigger Activating a Tool Call

**Client Requirements**: FR.ADV.TRIG.01, FR.ADV.TRIG.02, FR.ADV.TRIG.03
**MCP Concepts**: Tools, `CallToolAsync`

**Interaction Flow**:
1.  **MCP Server**: Sends a message to the client (either a response to a client request or a server-pushed message).
2.  **Client (Processing)**:
    a.  The message is received and displayed in the Main Output (FR.UI.CENTER.01).
    b.  The Trigger system evaluates this message against all active trigger patterns (FR.ADV.TRIG.01).
3.  **Client (Trigger Match)**: A trigger's regex matches the message.
    a.  If the trigger action is `Send Command` (FR.ADV.TRIG.02):
        i.  A new command string is constructed, potentially using capture groups from the regex match (FR.ADV.TRIG.03).
        ii. This new command is parsed to identify a tool name and arguments, similar to a user-typed command (see pattern 2, step 2c).
        iii. The client then executes `await mcpClient.CallToolAsync(...)` with this new tool name and arguments.
    b.  Other actions (`Highlight Line`, `Play Sound`) are handled client-side.
4.  **MCP Server**: (If a command was sent by the trigger) Receives the new `CallTool` request and processes it as usual, unaware that it was automated by a client-side trigger. Sends a response.
5.  **Client (Display)**: Displays the response from the trigger-initiated `CallToolAsync` in the Main Output.

## 5. Using the External API to Send a Command

**Client Requirements**: FR.ADV.API.01
**MCP Concepts**: Tools, `CallToolAsync`

**Interaction Flow**:
1.  **External Application**: Makes an HTTP request to the client's local API endpoint (e.g., POST to `/api/execute_tool`). The request body contains the MCP Tool name and arguments.
2.  **Client (HTTP Server Layer)**:
    a.  The embedded HTTP server receives the request.
    b.  Parses the request to extract the tool name and arguments.
    c.  Validates the request (e.g., ensure required parameters are present).
3.  **Client (MCP Interaction)**:
    a.  If an `IMcpClient` is connected to an MCP Server:
        `CallToolResponse response = await mcpClient.CallToolAsync(toolNameFromApi, toolArgsFromApi, CancellationToken.None);`
    b.  (Or, if the API is designed to just pass raw commands for the client to parse like user input, that client-side parsing would happen here first).
4.  **MCP Server**: Receives and processes the `CallTool` request, sends a response.
5.  **Client (HTTP Server Layer)**:
    a.  Receives the `CallToolResponse` from the `mcpClient`.
    b.  Formats an HTTP response (e.g., JSON) containing the result from the MCP server (or an error).
    c.  Sends the HTTP response back to the External Application.

## 6. Disconnecting

**Client Requirements**: User action (e.g., "Disconnect" button - FR.UI.LAYOUT.03) or application shutdown.
**MCP Concepts**: `DisposeAsync` or closing transport

**Interaction Flow**:
1.  **User Action (Client)**: User clicks a "Disconnect" button for an active profile, or closes the application.
2.  **Client (Internal)**:
    a.  Identifies the active `IMcpClient` instance for that connection.
    b.  Calls `await mcpClient.DisposeAsync();`. This should gracefully close the connection and release resources. Some transports might have a specific `CloseAsync()` method. Refer to SDK documentation for the preferred way.
3.  **SDK & Transport Layer**: Handles sending any necessary MCP disconnect messages and closes the underlying communication channel (e.g., TCP socket, stdio pipes).
4.  **MCP Server**: Detects the disconnection and cleans up any server-side state associated with that client.
5.  **Client (Internal)**:
    a.  Updates the connection status for the profile in the UI to "Offline" (FR.CONFIG.UI.02).
    b.  Nullifies or removes the stored `IMcpClient` instance.

These patterns illustrate how your C# WPF client's features translate into concrete interactions with an MCP server using the C# SDK. The key is to map user actions or automated client events to appropriate `IMcpClient` method calls and to handle responses or errors correctly.
