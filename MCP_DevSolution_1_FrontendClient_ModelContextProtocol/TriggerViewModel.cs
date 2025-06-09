using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows; // For MessageBox
using System.Windows.Input;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class TriggerViewModel : ViewModelBase
    {
        private readonly TriggerService _triggerService;
        private readonly AliasService _aliasService;
        private readonly AliasViewModel _aliasViewModel;
        private readonly Action<string> _logMessageAction;

        public ObservableCollection<Trigger> Triggers { get; private set; }

        private Trigger _selectedTrigger;
        public Trigger SelectedTrigger
        {
            get => _selectedTrigger;
            set
            {
                if (SetProperty(ref _selectedTrigger, value))
                {
                    UpdateEditFieldsFromSelectedTrigger();
                    RaiseCanExecuteChangedForCommands();
                }
            }
        }

        private string _editPattern;
        public string EditPattern { get => _editPattern; set { if (SetProperty(ref _editPattern, value)) RaiseCanExecuteChangedForCommands(); } }

        private string _editActionType;
        public string EditActionType { get => _editActionType; set { if (SetProperty(ref _editActionType, value)) RaiseCanExecuteChangedForCommands(); } }

        private string _editActionValue;
        public string EditActionValue { get => _editActionValue; set => SetProperty(ref _editActionValue, value); }

        private bool _editIsEnabled;
        public bool EditIsEnabled { get => _editIsEnabled; set => SetProperty(ref _editIsEnabled, value); }

        public ObservableCollection<string> ActionTypes { get; }

        public ICommand AddUpdateTriggerCommand { get; }
        public ICommand DeleteTriggerCommand { get; }
        public ICommand ClearTriggerFieldsCommand { get; }

        public TriggerViewModel(TriggerService triggerService, AliasService aliasService, AliasViewModel aliasViewModel, Action<string> logMessageAction)
        {
            _triggerService = triggerService ?? throw new ArgumentNullException(nameof(triggerService));
            _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
            _aliasViewModel = aliasViewModel ?? throw new ArgumentNullException(nameof(aliasViewModel));
            _logMessageAction = logMessageAction ?? throw new ArgumentNullException(nameof(logMessageAction));

            Triggers = new ObservableCollection<Trigger>();
            ActionTypes = new ObservableCollection<string> { "Send Command", "Highlight Line", "Play Sound" };
            EditIsEnabled = true; // Default for new trigger

            AddUpdateTriggerCommand = new RelayCommand(async _ => await AddUpdateTriggerAsync(), _ => CanAddUpdateTrigger());
            DeleteTriggerCommand = new RelayCommand(async _ => await DeleteTriggerAsync(), _ => CanDeleteTrigger());
            ClearTriggerFieldsCommand = new RelayCommand(_ => ClearEditFields());

            _ = LoadTriggersDataAsync();
        }

        private async Task LoadTriggersDataAsync()
        {
            var loadedTriggers = await _triggerService.LoadTriggersAsync();
            Triggers.Clear();
            foreach (var trigger in loadedTriggers)
            {
                Triggers.Add(trigger);
            }
            if (!Triggers.Any())
            {
                _logMessageAction?.Invoke("INFO: No triggers found or trigger file was empty/corrupt.");
            }
        }

        private async Task AddUpdateTriggerAsync()
        {
            if (!CanAddUpdateTrigger()) return;

            string pattern = EditPattern.Trim();
            string actionType = EditActionType;
            string actionValue = EditActionValue; // Whitespace might be significant

            try
            {
                new Regex(pattern);
            }
            catch (ArgumentException ex)
            {
                _logMessageAction?.Invoke($"ERROR: Invalid Regex pattern for trigger '{pattern}': {ex.Message}");
                MessageBox.Show($"Invalid Regex pattern: {ex.Message}", "Regex Error", MessageBoxButton.OK, MessageBoxImage.Error); // Keep MessageBox for critical validation
                return;
            }

            var existingTrigger = Triggers.FirstOrDefault(t => t.Pattern.Equals(pattern, StringComparison.Ordinal) && t.ActionType == actionType);
            if (existingTrigger != null)
            {
                existingTrigger.ActionValue = actionValue;
                existingTrigger.IsEnabled = EditIsEnabled;
                _logMessageAction?.Invoke($"INFO: Trigger for pattern '{pattern}' ({actionType}) updated.");
            }
            else
            {
                Triggers.Add(new Trigger { Pattern = pattern, ActionType = actionType, ActionValue = actionValue, IsEnabled = EditIsEnabled });
            }

            bool success = await _triggerService.SaveTriggersAsync(Triggers.ToList());
            if(success)
            {
                if(existingTrigger != null)
                {
                    _logMessageAction?.Invoke($"INFO: Trigger for pattern '{pattern}' ({actionType}) updated successfully.");
                }
                else
                {
                    _logMessageAction?.Invoke($"INFO: Trigger for pattern '{pattern}' ({actionType}) added successfully.");
                    ClearEditFields(); // Clear only if it was a new add
                }
            }
            else
            {
                _logMessageAction?.Invoke($"ERROR: Failed to save trigger for pattern '{pattern}'.");
            }
        }

        private bool CanAddUpdateTrigger(object parameter = null)
        {
            return !string.IsNullOrWhiteSpace(EditPattern) && !string.IsNullOrEmpty(EditActionType);
        }

        private async Task DeleteTriggerAsync()
        {
            if (!CanDeleteTrigger()) return;

            string deletedPattern = SelectedTrigger.Pattern;

            Triggers.Remove(SelectedTrigger);
            bool success = await _triggerService.SaveTriggersAsync(Triggers.ToList());
            if(success)
            {
                _logMessageAction?.Invoke($"INFO: Trigger for pattern '{deletedPattern}' deleted successfully.");
            }
            else
            {
                 _logMessageAction?.Invoke($"ERROR: Failed to save trigger deletion for '{deletedPattern}'. Trigger list may be out of sync.");
            }
            // ClearEditFields() is called by SelectedTrigger setter when it becomes null
        }

        private bool CanDeleteTrigger(object parameter = null)
        {
            return SelectedTrigger != null;
        }

        private void ClearEditFields(object parameter = null)
        {
            EditPattern = string.Empty;
            EditActionType = ActionTypes.FirstOrDefault(); // Default to first action type or null
            EditActionValue = string.Empty;
            EditIsEnabled = true; // Default for new
            SelectedTrigger = null; // This will also clear fields via UpdateEditFieldsFromSelectedTrigger
        }

        private void UpdateEditFieldsFromSelectedTrigger()
        {
            if (SelectedTrigger != null)
            {
                EditPattern = SelectedTrigger.Pattern;
                EditActionType = SelectedTrigger.ActionType;
                EditActionValue = SelectedTrigger.ActionValue;
                EditIsEnabled = SelectedTrigger.IsEnabled;
            }
            else
            {
                // If selection is cleared, reset edit fields to defaults for adding a new one
                EditPattern = string.Empty;
                EditActionType = ActionTypes.FirstOrDefault();
                EditActionValue = string.Empty;
                EditIsEnabled = true;
            }
        }

        private void RaiseCanExecuteChangedForCommands()
        {
            ((RelayCommand)AddUpdateTriggerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteTriggerCommand).RaiseCanExecuteChanged();
        }

        public string ProcessLineForTrigger(string line)
        {
            if (_aliasViewModel == null || _aliasService == null) return null; // Dependencies not ready

            return _triggerService.ProcessIncomingLine(
                line,
                Triggers.Where(t => t.IsEnabled).ToList(),
                _aliasViewModel.Aliases.ToList(), // Get current aliases from AliasViewModel
                _aliasService.ProcessCommand      // Pass the alias processing method from AliasService
            );
        }
    }
}
