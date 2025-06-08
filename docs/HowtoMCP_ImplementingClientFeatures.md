# How to Implement Client Features with Model Context Protocol

This document provides guidance on implementing the features of your C# WPF client (as defined in `ProjectRequirements*.md`) using the Model Context Protocol (MCP) and its official C# SDK.

Refer to:
- Official MCP Documentation: [https://modelcontextprotocol.io/introduction](https://modelcontextprotocol.io/introduction)
- C# SDK GitHub: [https://github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)
- C# SDK API Documentation: [https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html)

## 1. Core Setup and SDK Integration

Before implementing specific features, ensure your WPF project is set up to use the MCP C# SDK.
- **Install NuGet Package**: Add the `ModelContextProtocol` NuGet package (and potentially `Microsoft.Extensions.Hosting` for dependency injection and service lifetime management if you follow SDK examples).
  ```bash
  dotnet add package ModelContextProtocol --prerelease
  dotnet add package Microsoft.Extensions.Hosting
  ```
- **Service Configuration**: Consider using `Microsoft.Extensions.DependencyInjection` to manage services like the `IMcpClient` instance.

## 2. Implementing Core UI Features (UR.COREUI)

### FR.UI.LAYOUT.01: Main Window Layout
- Design your WPF window with the three vertical sections. This is standard WPF XAML layout work.

### FR.UI.LAYOUT.02 & FR.UI.LAYOUT.03: Menu Bar & Button Bar
- Implement standard WPF Menu and ToolBar controls.
- Actions triggered by these controls (e.g., "Connect", "Disconnect") will invoke methods that interact with the `IMcpClient`.

### FR.UI.CENTER.01: Main Output Display
- Use a WPF control suitable for displaying a continuous stream of text (e.g., `TextBox` with `IsReadOnly=True`, `ScrollViewer`, or a `RichTextBox` if formatting is needed).
- When the `IMcpClient` receives data from the server (e.g., responses from `CallToolAsync`, server-pushed messages if the transport supports them), append it to this display.
- **Data Handling**: You'll need a mechanism (e.g., events, callbacks, reactive extensions) to update the UI from the networking layer where MCP interactions occur to avoid UI freezes (NFR.UI.PERF.01).

### FR.UI.CENTER.02: Command Input
- A simple WPF `TextBox` for user input.
- On Enter/Send button click:
    1. Retrieve the text from the input field.
    2. This text will often be the primary input for an MCP `Tool` or part of a more complex request.
    3. Process this input through the Alias system (see section 4.1).
    4. If an `IMcpClient` is connected, use the input to form and execute an MCP request, typically `await mcpClient.CallToolAsync(...)`. (See `HowtoMCP_UtilizingTheCSharpSDK.md` for more on `CallToolAsync`).
    5. The result from `CallToolAsync` (or any errors) should be displayed in the Main Output.

### FR.UI.CENTER.03: Command History
- Implement a client-side list or queue to store recently sent commands.
- Handle up/down arrow keys in the command input `TextBox` to navigate this history.

## 3. Configuration & Session Management (UR.CONFIG)

### FR.CONFIG.UI.01: Connections Tab
- Use a WPF `TabControl` for the right sidebar, with one `TabItem` for "Connections".
- Inside this tab, use a `ListBox` or `DataGrid` to display saved server profiles.

### FR.CONFIG.UI.02: Connection Status
- Each item in the profile list should display Name, Server Address, and Status.
- The status ("Online", "Offline", "MCP Active") will be updated based on the state of the `IMcpClient` associated with that profile.
    - "Offline": No active `IMcpClient` or connection attempt failed.
    - "Connecting": `McpClientFactory.CreateAsync(...)` called, awaiting result.
    - "MCP Active" / "Online": `IMcpClient` successfully created and connected.

### FR.CONFIG.UI.03: Profile Management Dialog
- Create a separate WPF `Window` or UserControl for adding/editing/deleting profiles.
- Store profile data (FR.CONFIG.PROFILE.01: Name, Hostname/IP, Port) persistently (e.g., JSON file, XML, settings).
- **Connecting**:
    - When the user clicks "Connect" for a profile:
        1. Retrieve profile details (hostname, port, any other transport settings).
        2. Construct the appropriate `IClientTransport` (e.g., `StdioClientTransport` if it's a local command-line server, or an equivalent for TCP/HTTP if connecting to a networked server). The C# SDK's documentation should provide details on available transports or how to implement custom ones.
        3. Call `IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);`.
        4. Store the active `mcpClient` instance, associated with the profile, perhaps in a dictionary.
        5. Update status to "MCP Active".
- **Disconnecting**:
    1. If an `IMcpClient` is active for the profile:
       `await mcpClient.DisposeAsync();` (or `client.CloseAsync()` if available and appropriate for the transport).
    2. Update status to "Offline".

## 4. Logging System (UR.LOG)

### FR.LOG.UI.01 & FR.LOG.UI.02: Log Tab & Filtered Channels
- The left sidebar `TabControl` will host `TabItem`s for "All", "Chat", "Combat", "System", "MCP Debug".
- Each `TabItem` can contain its own scrollable text display control.
- **Message Categorization**: This is a client-side responsibility. When messages arrive from the MCP server:
    1. Display in "All" and the main output.
    2. Implement logic (e.g., regex matching on message content, checking message properties if the MCP server sends structured data that allows categorization) to decide which other channels (`Chat`, `Combat`, `System`) it belongs to.
    3. "MCP Debug" could show raw request/response data (if feasible and desired for debugging) or specific diagnostic messages from the SDK/client.
- **Performance (NFR.LOG.PERF.01)**: For high-volume logs, consider UI virtualization techniques or only keeping a limited backlog in memory for display controls.

## 5. Advanced Features (UR.ADV)

### Aliases (FR.ADV.ALIAS.01, FR.ADV.ALIAS.02, FR.ADV.ALIAS.03)
- **Management UI (FR.ADV.ALIAS.03)**: A tab in the right sidebar with controls to add, edit, delete aliases (store persistently).
- **Processing**:
    1. When the user enters text in the command input (FR.UI.CENTER.02):
    2. Before sending to the server, check if the input matches a defined alias.
    3. If it does, replace the input text with the alias's defined command.
    4. For parameterized aliases (FR.ADV.ALIAS.02), parse arguments from the user's input (e.g., `%1`, `%2`) and substitute them into the alias command.
    5. The resulting command is then used for the `CallToolAsync` operation.

### Triggers (FR.ADV.TRIG.01, FR.ADV.TRIG.02, FR.ADV.TRIG.03, FR.ADV.TRIG.04)
- **Management UI (FR.ADV.TRIG.04)**: A tab in the right sidebar for CRUD operations on triggers (store persistently). Each trigger needs: a regex pattern, and action(s).
- **Processing**:
    1. For every message received from the MCP server and displayed in the main output:
    2. Iterate through all active triggers.
    3. Check if the message text matches the trigger's regex (FR.ADV.TRIG.01).
    4. If a match occurs, perform the defined action (FR.ADV.TRIG.02):
        - **`Send Command`**: Construct a new command string. If the regex used capture groups, substitute these into the command string (FR.ADV.TRIG.03). Send this new command to the MCP server using `await mcpClient.CallToolAsync(...)`.
        - **`Highlight Line`**: Change the display properties (e.g., background color) of the matched line in the UI.
        - **`Play Sound`**: Use a .NET sound playing API (e.g., `System.Media.SoundPlayer`).

### External API (FR.ADV.API.01, FR.ADV.API.02, FR.ADV.API.03)
- **Local HTTP Endpoint (FR.ADV.API.01)**:
    - Use `System.Net.HttpListener` or a lightweight web framework like ASP.NET Core Minimal APIs (if you want to include that dependency) to host an HTTP server within your WPF application.
    - Define endpoints (e.g., `/send_command`, `/get_status`).
    - When an API endpoint is called, it should interact with the active `IMcpClient` to send commands or retrieve information.
    - **Security**: Be mindful of security if exposing an API, even locally. Consider authentication/authorization if needed.
- **Headless Mode (FR.ADV.API.02)**:
    - Architect your application so that the core logic for MCP communication and feature processing (aliases, triggers, API handling) is separate from the WPF UI code.
    - Your `Main` method could conditionally start the WPF UI or run in a console mode that only initializes the API endpoint and MCP client connections.
- **API Management UI (FR.ADV.API.03)**: A tab in the right sidebar to:
    - Start/Stop the HTTP server.
    - Configure port number (store persistently).
    - Display API status and endpoint information.

## 6. General Considerations
- **Error Handling**: Wrap MCP SDK calls (`CreateAsync`, `CallToolAsync`, etc.) in try-catch blocks. Display errors to the user in the main output or a status bar.
- **Asynchronous Operations**: Use `async` and `await` for all MCP operations and other I/O-bound tasks to keep the UI responsive (NFR.UI.PERF.01).
- **Threading**: Ensure UI updates are marshaled back to the UI thread (e.g., using `Dispatcher.Invoke` or `Dispatcher.BeginInvoke` in WPF).

This document provides a roadmap for translating your project's requirements into a functional MCP client. The specifics of SDK usage will be further detailed in `HowtoMCP_UtilizingTheCSharpSDK.md`.
