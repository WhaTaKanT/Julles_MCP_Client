using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel; // Added for ObservableCollection

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class McpSessionManager : ViewModelBase // Inherit for IsNegotiated property notification
    {
        private readonly NetworkService _networkService;
        private readonly McpParserService _mcpParserService;
        private readonly Action<string> _logMessageAction; // For logging to MainViewModel

        private bool _isNegotiated;
        public bool IsNegotiated
        {
            get => _isNegotiated;
            private set => SetProperty(ref _isNegotiated, value);
        }

        public string ServerMcpVersion { get; private set; }
        public List<string> SupportedServerPackages { get; private set; }
        public ObservableCollection<McpTool> AvailableMcpTools { get; private set; }

        // TODO: Add properties for authentication keys if needed by your assumed MCP spec
        // private string _clientAuthKey = "some_default_client_key"; // Example
        // private string _serverAuthKey;

        public McpSessionManager(NetworkService networkService, McpParserService mcpParserService, Action<string> logMessageAction)
        {
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _mcpParserService = mcpParserService ?? throw new ArgumentNullException(nameof(mcpParserService));
            _logMessageAction = logMessageAction ?? throw new ArgumentNullException(nameof(logMessageAction));

            SupportedServerPackages = new List<string>();
            AvailableMcpTools = new ObservableCollection<McpTool>(); // Initialize
            ResetSessionState(); // Initialize state
        }

        public void ResetSessionState()
        {
            IsNegotiated = false;
            ServerMcpVersion = null;
            SupportedServerPackages.Clear();
            AvailableMcpTools.Clear(); // Clear tools
            // _serverAuthKey = null; // Reset auth keys if used
            _logMessageAction?.Invoke("INFO: MCP session state reset.");
        }

        public async Task StartNegotiationAsync()
        {
            if (!_networkService.IsConnected)
            {
                _logMessageAction?.Invoke("ERROR: Cannot start MCP negotiation. Not connected to server.");
                return;
            }

            // ResetSessionState(); // Resetting here might be too aggressive if called multiple times.
                                 // Usually reset on connect/disconnect. Already called in constructor.
                                 // Let's ensure it's clean for a *new* negotiation attempt.
            IsNegotiated = false;
            ServerMcpVersion = null;
            SupportedServerPackages.Clear();

            _logMessageAction?.Invoke("INFO: Starting MCP negotiation...");

            var negotiateMsg = new McpMessage("mcp");
            negotiateMsg.AddArgument("version", "2.1");
            negotiateMsg.AddArgument("to", "2.1");
            // negotiateMsg.AddArgument("key", _clientAuthKey); // Example if using auth keys

            string formattedMsg = _mcpParserService.Format(negotiateMsg);
            if (formattedMsg != null)
            {
                bool sent = await _networkService.SendDataAsync(formattedMsg);
                if (sent)
                {
                    _logMessageAction?.Invoke($"SENT MCP: {formattedMsg}");
                }
                else
                {
                    _logMessageAction?.Invoke($"ERROR: Failed to send MCP negotiation message: {formattedMsg}");
                }
            }
            else
            {
                _logMessageAction?.Invoke("ERROR: Could not format MCP negotiation message.");
            }
        }

        public void HandleIncomingMcpMessage(McpMessage message)
        {
            if (message == null) return;

            // Construct a more readable log for arguments
            string argsString = string.Join(" ", message.Arguments.Select(kv => $"{kv.Key}:\"{kv.Value}\"")); // Quote values for clarity
            _logMessageAction?.Invoke($"RECV MCP: #{message.MessageName} {argsString}");

            if (message.DefinedTool != null)
            {
                var existingTool = AvailableMcpTools.FirstOrDefault(t => t.Name.Equals(message.DefinedTool.Name, StringComparison.OrdinalIgnoreCase));
                if (existingTool != null)
                {
                    // Replace existing tool definition
                    AvailableMcpTools.Remove(existingTool);
                }
                AvailableMcpTools.Add(message.DefinedTool);
                _logMessageAction?.Invoke($"INFO: MCP Tool '{message.DefinedTool.Name}' defined/updated. Total tools: {AvailableMcpTools.Count}");
                OnPropertyChanged(nameof(AvailableMcpTools)); // Notify if anything is bound to the collection instance itself or Count
            }

            switch (message.MessageName.ToLowerInvariant())
            {
                case "mcp":
                    HandleMcpHandshakeResponse(message);
                    break;
                case "mcp-negotiate-can":
                    HandleMcpNegotiateCan(message);
                    break;
                case "mcp-negotiate-end":
                    HandleMcpNegotiateEnd(message);
                    break;
                case "dns-com-vmoo-character": // Note: Changed from dns-com-vmo-character to match example usage
                    HandleDnsCharacterInfo(message);
                    break;
                default:
                    _logMessageAction?.Invoke($"INFO: Unhandled MCP message: {message.MessageName}");
                    break;
            }
        }

        private void HandleMcpHandshakeResponse(McpMessage message)
        {
            string version = message.GetArgument("version");
            if (!string.IsNullOrEmpty(version))
            {
                ServerMcpVersion = version;
                _logMessageAction?.Invoke($"INFO: Server MCP version: {ServerMcpVersion}.");
            }
        }

        private void HandleMcpNegotiateCan(McpMessage message)
        {
            string packageName = message.GetArgument("package");
            string version = message.GetArgument("version"); // Optional version info
            if (!string.IsNullOrEmpty(packageName))
            {
                if (!SupportedServerPackages.Contains(packageName))
                {
                    SupportedServerPackages.Add(packageName);
                }
                _logMessageAction?.Invoke($"INFO: Server supports MCP package: {packageName} (Version: {version ?? "N/A"})");
            }
        }

        private void HandleMcpNegotiateEnd(McpMessage message)
        {
            IsNegotiated = true;
            _logMessageAction?.Invoke("INFO: MCP negotiation complete. Session active.");
        }

        private void HandleDnsCharacterInfo(McpMessage message)
        {
            if (!IsNegotiated)
            {
                _logMessageAction?.Invoke("WARN: Received 'dns-com-vmoo-character' before MCP negotiation complete. Ignoring.");
                return;
            }

            string charName = message.GetArgument("name");
            string roomName = message.GetArgument("room");
            string hp = message.GetArgument("hp");

            _logMessageAction?.Invoke($"MCP DATA [dns-com-vmoo-character]: Name='{charName}', Room='{roomName}', HP='{hp}'");
        }
    }
}
