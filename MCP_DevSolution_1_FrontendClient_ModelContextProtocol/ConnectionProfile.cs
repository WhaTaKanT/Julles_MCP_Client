namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    // Deriving from ViewModelBase to get INotifyPropertyChanged implementation
    public class ConnectionProfile : ViewModelBase
    {
        private string _profileName;
        public string ProfileName
        {
            get => _profileName;
            set => SetProperty(ref _profileName, value);
        }

        private string _serverHost;
        public string ServerHost
        {
            get => _serverHost;
            set => SetProperty(ref _serverHost, value);
        }

        private int _serverPort;
        public int ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ConnectionProfile()
        {
            // Initialize with defaults, SetProperty will also invoke OnPropertyChanged if used here,
            // but direct field assignment is fine for constructor defaults if no one is subscribed yet.
            _profileName = "New Profile";
            _serverHost = "localhost";
            _serverPort = 10100;
            _status = "Offline";
        }
    }
}
