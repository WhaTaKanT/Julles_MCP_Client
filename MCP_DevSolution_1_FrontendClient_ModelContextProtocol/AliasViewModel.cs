using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For MessageBox
using System.Windows.Input;
using System; // For Action

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class AliasViewModel : ViewModelBase
    {
        private readonly AliasService _aliasService;
        private readonly Action<string> _logMessageAction;

        public ObservableCollection<Alias> Aliases { get; private set; }

        private Alias _selectedAlias;
        public Alias SelectedAlias
        {
            get => _selectedAlias;
            set
            {
                if (SetProperty(ref _selectedAlias, value))
                {
                    UpdateEditFieldsFromSelectedAlias();
                    // Notify commands that their CanExecute status might have changed
                    ((RelayCommand)DeleteAliasCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string _editAliasPhrase;
        public string EditAliasPhrase
        {
            get => _editAliasPhrase;
            set
            {
                if (SetProperty(ref _editAliasPhrase, value))
                {
                    ((RelayCommand)AddUpdateAliasCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string _editReplacementText;
        public string EditReplacementText
        {
            get => _editReplacementText;
            // No need to raise CanExecuteChanged for AddUpdateAliasCommand here,
            // as it's primarily dependent on EditAliasPhrase.
            set => SetProperty(ref _editReplacementText, value);
        }

        public ICommand AddUpdateAliasCommand { get; }
        public ICommand DeleteAliasCommand { get; }
        public ICommand ClearAliasFieldsCommand { get; }

        public AliasViewModel(AliasService aliasService, Action<string> logMessageAction)
        {
            _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
            _logMessageAction = logMessageAction ?? throw new ArgumentNullException(nameof(logMessageAction));
            Aliases = new ObservableCollection<Alias>();

            AddUpdateAliasCommand = new RelayCommand(async _ => await AddUpdateAliasAsync(), _ => CanAddUpdateAlias());
            DeleteAliasCommand = new RelayCommand(async _ => await DeleteAliasAsync(), _ => CanDeleteAlias());
            ClearAliasFieldsCommand = new RelayCommand(_ => ClearEditFields());

            _ = LoadAliasesDataAsync();
        }

        private async Task LoadAliasesDataAsync()
        {
            var loadedAliases = await _aliasService.LoadAliasesAsync();
            Aliases.Clear();
            foreach (var alias in loadedAliases)
            {
                Aliases.Add(alias);
            }
            if (!Aliases.Any())
            {
                _logMessageAction?.Invoke("INFO: No aliases found or alias file was empty/corrupt.");
            }
        }

        private async Task AddUpdateAliasAsync()
        {
            if (!CanAddUpdateAlias()) return; // Should not happen if CanExecute is working

            string phrase = EditAliasPhrase.Trim(); // Re-trim just in case
            string replacement = EditReplacementText;

            // Replacement text being empty IS allowed by requirements (to clear input)
            // but for this function, let's assume some text is expected.
            // if (string.IsNullOrWhiteSpace(replacement))
            // {
            //     MessageBox.Show("Replacement text cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }

            var existingAlias = Aliases.FirstOrDefault(a => a.AliasPhrase.Equals(phrase, System.StringComparison.OrdinalIgnoreCase));
            if (existingAlias != null)
            {
                existingAlias.ReplacementText = replacement;
                _logMessageAction?.Invoke($"INFO: Alias '{phrase}' updated.");
            }
            else
            {
                Aliases.Add(new Alias { AliasPhrase = phrase, ReplacementText = replacement });
            }

            bool success = await _aliasService.SaveAliasesAsync(Aliases.ToList());
            if (success)
            {
                if (existingAlias != null)
                {
                    _logMessageAction?.Invoke($"INFO: Alias '{phrase}' updated successfully.");
                }
                else
                {
                    _logMessageAction?.Invoke($"INFO: Alias '{phrase}' added successfully.");
                    ClearEditFields(); // Clear only if it was a new add
                }
            }
            else
            {
                _logMessageAction?.Invoke($"ERROR: Failed to save alias '{phrase}'.");
            }
        }

        private bool CanAddUpdateAlias(object parameter = null)
        {
            // Basic validation: phrase must not be empty. Replacement can be empty.
            return !string.IsNullOrWhiteSpace(EditAliasPhrase);
        }

        private async Task DeleteAliasAsync()
        {
            if (!CanDeleteAlias()) return;

            string deletedPhrase = SelectedAlias.AliasPhrase;

            Aliases.Remove(SelectedAlias);
            bool success = await _aliasService.SaveAliasesAsync(Aliases.ToList());
            if(success)
            {
                _logMessageAction?.Invoke($"INFO: Alias '{deletedPhrase}' deleted successfully.");
            }
            else
            {
                _logMessageAction?.Invoke($"ERROR: Failed to save alias deletion for '{deletedPhrase}'. Alias list may be out of sync.");
                // Consider re-adding or other recovery if critical
            }
            // ClearEditFields is called by SelectedAlias setter when it becomes null after Remove
        }

        private bool CanDeleteAlias(object parameter = null)
        {
            return SelectedAlias != null;
        }

        private void ClearEditFields(object parameter = null) // Made it compatible with RelayCommand
        {
            EditAliasPhrase = string.Empty;
            EditReplacementText = string.Empty;
            SelectedAlias = null; // This will also clear fields due to UpdateEditFieldsFromSelectedAlias
        }

        private void UpdateEditFieldsFromSelectedAlias()
        {
            if (SelectedAlias != null)
            {
                EditAliasPhrase = SelectedAlias.AliasPhrase;
                EditReplacementText = SelectedAlias.ReplacementText;
            }
            else
            {
                // If selection is cleared, clear edit fields
                // This might be redundant if ClearEditFields is called explicitly when deselecting.
                // EditAliasPhrase = string.Empty;
                // EditReplacementText = string.Empty;
            }
        }
    }
}
