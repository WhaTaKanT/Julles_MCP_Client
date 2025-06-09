using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // For MessageBox
using System.Windows.Input;
using System; // For Action

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class ConnectionProfileViewModel : ViewModelBase
    {
        private readonly ProfileService _profileService;
        private readonly Action<string> _logMessageAction;

        public ObservableCollection<ConnectionProfile> Profiles { get; private set; }

        private ConnectionProfile _selectedProfile;
        public ConnectionProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                {
                    // When selection changes, update edit fields or enable/disable commands
                    ((RelayCommand)OpenEditProfileDialogCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteProfileCommand).RaiseCanExecuteChanged();
                    if (_selectedProfile != null)
                    {
                        // Optionally pre-fill edit fields if dialog is not modal or part of same view
                    }
                }
            }
        }

        // Properties for dialog input
        private string _editProfileName;
        public string EditProfileName { get => _editProfileName; set => SetProperty(ref _editProfileName, value); }

        private string _editServerHost;
        public string EditServerHost { get => _editServerHost; set => SetProperty(ref _editServerHost, value); }

        private int _editServerPort;
        public int EditServerPort { get => _editServerPort; set => SetProperty(ref _editServerPort, value); }

        public ICommand OpenAddProfileDialogCommand { get; }
        public ICommand OpenEditProfileDialogCommand { get; }
        public ICommand DeleteProfileCommand { get; }

        public ConnectionProfileViewModel(ProfileService profileService, Action<string> logMessageAction)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _logMessageAction = logMessageAction ?? throw new ArgumentNullException(nameof(logMessageAction));
            Profiles = new ObservableCollection<ConnectionProfile>();

            OpenAddProfileDialogCommand = new RelayCommand(ExecuteOpenAddProfileDialog);
            OpenEditProfileDialogCommand = new RelayCommand(ExecuteOpenEditProfileDialog, CanEditOrDeleteProfile);
            DeleteProfileCommand = new RelayCommand(async () => await ExecuteDeleteProfileAsync(), CanEditOrDeleteProfile);

            _ = LoadProfilesDataAsync();
        }

        private async Task LoadProfilesDataAsync()
        {
            // Consider IsLoading flag for UI
            var loadedProfiles = await _profileService.LoadProfilesAsync();
            Profiles.Clear();
            foreach (var profile in loadedProfiles)
            {
                Profiles.Add(profile);
            }
            if (!Profiles.Any())
            {
                _logMessageAction?.Invoke("INFO: No connection profiles found or file was empty/corrupt.");
            }
        }

        private bool CanEditOrDeleteProfile(object parameter = null)
        {
            return SelectedProfile != null;
        }

        private void ExecuteOpenAddProfileDialog(object parameter = null)
        {
            var dialogData = new ProfileDialogData
            {
                ProfileName = "New Profile", // Default values for new profile
                ServerHost = "localhost",
                ServerPort = 10100,
                IsNewProfile = true
            };

            ProfileManagementDialog dialog = new ProfileManagementDialog(dialogData);

            if (dialog.ShowDialog() == true)
            {
                // FormData in dialogData is updated by the dialog on save
                _ = AddProfileAsync(dialog.FormData);
            }
        }

        private async Task AddProfileAsync(ProfileDialogData data)
        {
            if (Profiles.Any(p => p.ProfileName.Equals(data.ProfileName, System.StringComparison.OrdinalIgnoreCase)))
            {
                _logMessageAction?.Invoke($"ERROR: A profile with the name '{data.ProfileName}' already exists.");
                MessageBox.Show($"A profile with the name '{data.ProfileName}' already exists. Please choose a different name.", "Add Profile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newProfile = new ConnectionProfile
            {
                ProfileName = data.ProfileName,
                ServerHost = data.ServerHost,
                ServerPort = data.ServerPort,
                Status = "Offline"
            };
            Profiles.Add(newProfile);
            bool success = await _profileService.SaveProfilesAsync(Profiles.ToList());
            if (success)
            {
                _logMessageAction?.Invoke($"INFO: Profile '{newProfile.ProfileName}' added successfully.");
                SelectedProfile = newProfile;
            }
            else
            {
                _logMessageAction?.Invoke($"ERROR: Failed to save new profile '{newProfile.ProfileName}'.");
                // Optionally remove the profile from the collection if save failed, or mark it as unsaved.
                // For now, it remains in collection but user is notified of save failure.
            }
        }

        private void ExecuteOpenEditProfileDialog(object parameter = null)
        {
            if (SelectedProfile == null) return;

            var dialogData = new ProfileDialogData
            {
                ProfileName = SelectedProfile.ProfileName,
                ServerHost = SelectedProfile.ServerHost,
                ServerPort = SelectedProfile.ServerPort,
                IsNewProfile = false
            };

            ProfileManagementDialog dialog = new ProfileManagementDialog(dialogData);

            if (dialog.ShowDialog() == true)
            {
                 _ = UpdateProfileAsync(dialog.FormData);
            }
        }

        private async Task UpdateProfileAsync(ProfileDialogData data)
        {
            if (SelectedProfile == null) return;

            // ProfileName is not editable via dialog if IsNewProfile is false and TextBox is disabled.
            // If it were editable and changed, complex logic for updating keys might be needed.
            // For now, assume ProfileName from dialogData is the same as SelectedProfile.ProfileName.
            SelectedProfile.ServerHost = data.ServerHost;
            SelectedProfile.ServerPort = data.ServerPort;

            bool success = await _profileService.SaveProfilesAsync(Profiles.ToList());
            if (success)
            {
                _logMessageAction?.Invoke($"INFO: Profile '{SelectedProfile.ProfileName}' updated successfully.");
            }
            else
            {
                _logMessageAction?.Invoke($"ERROR: Failed to save updates for profile '{SelectedProfile.ProfileName}'.");
            }
            // Force refresh of the selected item in the list if properties don't auto-update via INPC
            var tempSelected = SelectedProfile;
            int index = Profiles.IndexOf(SelectedProfile);
            if(index >= 0)
            {
                // This is a common way to force UI refresh for an item if it doesn't implement INPC
                // However, if ConnectionProfile is a class, changes to its properties are reflected.
                // This line might not be strictly necessary if the object instance is the same and bound properties were updated.
                // OnPropertyChanged(nameof(SelectedProfile)); // Already called by SelectedProfile setter if value changes.
                // The list itself doesn't change, but its item's properties did.
                // Forcing a refresh of the list view might be needed if INPC is not on ConnectionProfile.
                // For now, we rely on the fact that the bound object's properties were updated.
            }
        }

        private async Task ExecuteDeleteProfileAsync()
        {
            if (SelectedProfile == null) return;
            string profileNameToDelete = SelectedProfile.ProfileName;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the profile '{profileNameToDelete}'?",
                                                       "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Profiles.Remove(SelectedProfile);
                bool success = await _profileService.SaveProfilesAsync(Profiles.ToList());
                if(success)
                {
                    _logMessageAction?.Invoke($"INFO: Profile '{profileNameToDelete}' deleted successfully.");
                }
                else
                {
                    _logMessageAction?.Invoke($"ERROR: Failed to save profile deletion for '{profileNameToDelete}'. Profiles list might be out of sync with saved data.");
                    // Consider re-adding the profile to the collection or other error handling.
                }
                SelectedProfile = null;
            }
        }
    }
}
