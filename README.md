## Project Title and Introduction
# MCP Frontend Client
This project is a C# WPF (Windows Presentation Foundation) desktop client application designed for interacting with services and servers that implement the Model Context Protocol (MCP). Its primary purpose is to provide a user-friendly graphical interface for sending commands to MCP-enabled endpoints, receiving and displaying data, and managing various client-side features to enhance this interaction.

## Features
*   **Connection Management**: Configure and manage multiple server profiles, including connection details (hostname/IP, port). Connect to and disconnect from MCP servers with visual status indicators.
*   **MCP Command Execution**: Send commands directly to connected MCP servers through a dedicated input area. View raw and formatted responses from the server in a main output display.
*   **Aliases**: Define custom shorthand commands (aliases) that expand into longer, more complex commands. Supports parameterized aliases for dynamic command generation.
*   **Triggers**: Set up automated actions based on patterns detected in messages received from the server. Supported actions include sending a new command, highlighting the triggering line, or playing a sound.
*   **Categorized Logging**: View incoming messages and client activity through a tabbed logging interface, with messages categorized into channels like "All," "Chat," "Combat," "System," and "MCP Debug."
*   **External API**: Expose a local HTTP API that allows external tools or scripts to interact with connected MCP servers through this client application. This enables integration with other developer tools or automation workflows.

## Core Concepts
This application functions as an **MCP Host** in the Model Context Protocol architecture. It utilizes the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) to handle communication with MCP servers.

For more information on the Model Context Protocol itself, please refer to the [official MCP documentation](https://modelcontextprotocol.io/introduction).

## Use Cases
This client can be used in various scenarios, including:
*   **Interacting with Game Servers**: Connecting to MCP-enabled game servers to send commands, receive game state updates, or interact with game mechanics.
*   **Developing and Debugging MCP Services**: Providing a convenient way for developers to test and debug their MCP server implementations by sending raw commands and inspecting responses.
*   **Data Interaction and Prototyping**: Quickly connecting to MCP-based data services to query information or prototype interactions.
*   **Workflow Automation**:
    *   Using **Aliases** to simplify frequently used or complex commands.
    *   Setting up **Triggers** to automate responses to server events (e.g., automatically issue a command when a specific message pattern is detected).
*   **Integration with External Tools**: Leveraging the **External API** to allow other local applications or scripts to send MCP commands or retrieve data through this client, acting as a bridge.

## Getting Started / Setup
This is a .NET WPF desktop application.

**Prerequisites:**
*   .NET SDK (compatible with .NET Framework or .NET Core, depending on the project's target - typically includes .NET Desktop Development workload). Visual Studio with C# support is recommended.

**Building from Source:**
1.  Clone this repository.
2.  Open the solution file (`.sln`, likely within the `MCP_DevSolution_1_FrontendClient_ModelContextProtocol` directory) in Visual Studio, or navigate to the project directory in your terminal.
3.  Build the solution/project. Using the .NET CLI:
    ```bash
    cd MCP_DevSolution_1_FrontendClient_ModelContextProtocol
    dotnet build
    ```
4.  The executable will typically be found in a subfolder like `bin/Debug/netX.X-windows/` or `bin/Release/netX.X-windows/`.

## How to Use (Overview)
The main interface is divided into several key areas:
*   **Left Sidebar**: Contains tabs for categorized logging (`All`, `Chat`, `Combat`, `System`, `MCP Debug`).
*   **Central Area**:
    *   **Main Output**: Displays messages and responses from the MCP server.
    *   **Command Input**: Enter MCP commands here to send to the connected server. Supports command history (Up/Down arrows).
*   **Right Sidebar**: Provides access to configuration and advanced features:
    *   **Connections Tab**: Manage server profiles (add, edit, delete, connect, disconnect).
    *   **Aliases Tab**: Define and manage command aliases.
    *   **Triggers Tab**: Configure and manage automated triggers.
    *   **API Tab**: Control and configure the External API.

**General Workflow:**
1.  Go to the **Connections** tab in the right sidebar.
2.  Add a new server profile, specifying the server's address (hostname/IP) and port.
3.  Select the profile and click "Connect."
4.  Once connected, use the **Command Input** field in the central area to send commands to the MCP server.
5.  Observe responses in the **Main Output**.
6.  Optionally, configure **Aliases** and **Triggers** in their respective tabs to streamline your workflow.

## Project Structure
*   `MCP_DevSolution_1_FrontendClient_ModelContextProtocol/`: Contains the source code for the C# WPF client application.
*   `docs/`: Includes detailed documentation regarding project requirements, design decisions, and advanced feature implementation guides for this client.

## Contributing
Contributions are welcome! If you have suggestions for improvements or encounter any issues, please feel free to open an issue or submit a pull request on the project's repository.
