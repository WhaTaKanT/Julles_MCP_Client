using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input; // For ICommand
using System; // For Action

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class MainViewModel : ViewModelBase
    {
        // Services
        private readonly ProfileService _profileService;
        private readonly AliasService _aliasService;
        private readonly TriggerService _triggerService;

        // Sub-ViewModels
        public ConnectionProfileViewModel ConnectionProfileVM { get; private set; }
        public AliasViewModel AliasVM { get; private set; }
        public TriggerViewModel TriggerVM { get; private set; }

        // Logging Properties
        public ObservableCollection<string> ServerOutputMessages { get; private set; }
        // TODO: Add ObservableCollections for other log channels (AllLogMessages, ChatLogMessages, etc.)
        // and initialize them if MainViewModel is to manage them.

        // Command Input Properties
        private string _commandInputText;
        public string CommandInputText
        {
            get => _commandInputText;
            set
            {
                if (SetProperty(ref _commandInputText, value))
                {
                    ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
                }
            }
        }
        public ICommand SendCommand { get; }
        private List<string> _commandHistory = new List<string>();
        private int _commandHistoryIndex = 0; // Points to the next spot to add, or current spot when navigating

        public MainViewModel()
        {
            // Instantiate services
            _profileService = new ProfileService();
            _aliasService = new AliasService();
            _triggerService = new TriggerService();

            // Instantiate sub-ViewModels (pass necessary services, other ViewModels, and LogMessage action)
            AliasVM = new AliasViewModel(_aliasService, LogMessage);
            TriggerVM = new TriggerViewModel(_triggerService, _aliasService, AliasVM, LogMessage);
            ConnectionProfileVM = new ConnectionProfileViewModel(_profileService, LogMessage);

            // Initialize collections
            ServerOutputMessages = new ObservableCollection<string>();
            // Initialize other log channel collections here if managed by MainViewModel

            // Initialize commands
            SendCommand = new RelayCommand(async _ => await ExecuteSendCommandAsync(), _ => CanExecuteSendCommand());

            // Load initial data for sub-ViewModels (they load their own data in their constructors)
             _ = LoadAllInitialDataAsync(); // Fire-and-forget
        }

        private async Task LoadAllInitialDataAsync()
        {
            // Sub-ViewModels (ConnectionProfileVM, AliasVM, TriggerVM) already call their
            // respective LoadDataAsync methods in their constructors.
            // This method can be used for any additional overarching initializations or
            // to orchestrate loading if there were dependencies between them not handled by injection.
            // For now, just an initial message.
            // A small delay can sometimes help ensure UI is ready before messages fly, but not a robust solution.
            await Task.Delay(150); // Allow some time for sub-VMs to potentially finish loading.
            ServerOutputMessages.Add("INFO: MainViewModel initialized. All systems ready.");
            // Note: Scrolling to end for ServerOutputMessages will be handled by the View (ListView)
            // if it's configured for auto-scroll or via an attached behavior.
            // Programmatic scroll from ViewModel is not ideal MVVM.
        }

        public void LogMessage(string message)
        {
            // Ensure this is called on the UI thread if messages can originate from other threads.
            // For now, all calls are expected to be from UI thread (ViewModel command handlers).
            ServerOutputMessages.Add(message);
            // Consider adding a timestamp or source prefix if desired.
            // Scrolling to end should be handled by the View.
        }

        private async Task ExecuteSendCommandAsync()
        {
            if (!CanExecuteSendCommand()) return;

            string originalCommand = CommandInputText;

            // Add to history before clearing, so up arrow can retrieve it if input is then cleared.
            if (!string.IsNullOrWhiteSpace(originalCommand)) // Avoid adding empty entries if Send is triggered somehow
            {
                 _commandHistory.Add(originalCommand);
                 _commandHistoryIndex = _commandHistory.Count; // Point to just after the last item
            }
            CommandInputText = string.Empty; // Clear input field immediately

            ServerOutputMessages.Add($"> {originalCommand}");

            string processedCommand = _aliasService.ProcessCommand(originalCommand, AliasVM.Aliases.ToList());
            if (!originalCommand.Equals(processedCommand, System.StringComparison.Ordinal))
            {
                ServerOutputMessages.Add($"ALIAS: '{originalCommand}' -> '{processedCommand}'");
            }

            ServerOutputMessages.Add($"SENT: {processedCommand}"); // Simulate sending

            // Simulate server response and trigger processing
            // In a real app, this response would come from a server connection service
            await Task.Delay(50); // Simulate network latency
            string simulatedServerResponse = $"SERVER ECHO: {processedCommand}";
            ServerOutputMessages.Add(simulatedServerResponse);

            string commandFromTrigger = TriggerVM.ProcessLineForTrigger(simulatedServerResponse);
            if (!string.IsNullOrEmpty(commandFromTrigger))
            {
                ServerOutputMessages.Add($"TRIGGER FIRED! Sending command: '{commandFromTrigger}'");
                _commandHistory.Add(commandFromTrigger); // Add triggered command to history
                _commandHistoryIndex = _commandHistory.Count;
                ServerOutputMessages.Add($"SENT (from trigger): {commandFromTrigger}");
            }
        }

        private bool CanExecuteSendCommand()
        {
            return !string.IsNullOrWhiteSpace(CommandInputText);
        }

        public void NavigateCommandHistory(bool up)
        {
            if (_commandHistory.Count == 0) return;

            if (up)
            {
                if (_commandHistoryIndex > 0)
                {
                    _commandHistoryIndex--;
                    CommandInputText = _commandHistory[_commandHistoryIndex];
                }
            }
            else // Down
            {
                if (_commandHistoryIndex < _commandHistory.Count - 1)
                {
                    _commandHistoryIndex++;
                    CommandInputText = _commandHistory[_commandHistoryIndex];
                }
                else if (_commandHistoryIndex == _commandHistory.Count - 1 || _commandHistoryIndex == _commandHistory.Count)
                {
                    // If at the last item or already at the "new command" slot, clear for new input
                    _commandHistoryIndex = _commandHistory.Count;
                    CommandInputText = string.Empty;
                }
            }
        }
    }
}
