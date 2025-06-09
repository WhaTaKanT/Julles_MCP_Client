namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class ConnectionProfile
    {
        public string ProfileName { get; set; }
        public string ServerHost { get; set; }
        public int ServerPort { get; set; }
        public string Status { get; set; } // Later: Online, Offline, Connecting, Error, etc.

        public ConnectionProfile()
        {
            ProfileName = "New Profile";
            ServerHost = "localhost";
            ServerPort = 10100; // Default port
            Status = "Offline"; // Default status
        }
    }
}
