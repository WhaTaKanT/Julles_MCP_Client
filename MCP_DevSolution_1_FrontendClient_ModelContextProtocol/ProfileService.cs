using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class ProfileService
    {
        private string _profileFilePath = "ConnectionProfiles.json";
        private List<ConnectionProfile> _profiles = new List<ConnectionProfile>();

        public async Task<List<ConnectionProfile>> LoadProfilesAsync()
        {
            if (File.Exists(_profileFilePath))
            {
                try
                {
                    string jsonString = await Task.Run(() => File.ReadAllText(_profileFilePath));
                    var loadedProfiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(jsonString);
                    if (loadedProfiles != null)
                    {
                        _profiles = loadedProfiles;
                    }
                    else
                    {
                        _profiles = new List<ConnectionProfile>();
                    }
                }
                catch (JsonException)
                {
                    _profiles = new List<ConnectionProfile>(); // Error in JSON format
                }
                catch (IOException)
                {
                    _profiles = new List<ConnectionProfile>(); // Error reading file
                }
            }
            else
            {
                _profiles = new List<ConnectionProfile>(); // File doesn't exist
            }
            // Return a copy
            return _profiles.Select(p => new ConnectionProfile
                                {
                                    ProfileName = p.ProfileName,
                                    ServerHost = p.ServerHost,
                                    ServerPort = p.ServerPort,
                                    Status = p.Status
                                }).ToList();
        }

        public async Task<bool> SaveProfilesAsync(List<ConnectionProfile> profilesToSave)
        {
            List<ConnectionProfile> profilesToSaveCopy;
            if (profilesToSave == null)
            {
                // Decided that null is not a valid list to "successfully" save.
                // Alternatively, could treat as saving an empty list. For now, treat as failure.
                return false;
            }
            else
            {
                profilesToSaveCopy = profilesToSave.Select(p => new ConnectionProfile
                {
                    ProfileName = p.ProfileName,
                    ServerHost = p.ServerHost,
                    ServerPort = p.ServerPort,
                    Status = p.Status ?? "Offline"
                }).ToList();
            }

            _profiles = new List<ConnectionProfile>(profilesToSaveCopy);

            try
            {
                string jsonString = JsonSerializer.Serialize(profilesToSaveCopy, new JsonSerializerOptions { WriteIndented = true });
                await Task.Run(() => File.WriteAllText(_profileFilePath, jsonString));
                return true;
            }
            catch (JsonException)
            {
                // Log to a debug output or internal log if necessary, but not UI log.
                return false;
            }
            catch (IOException)
            {
                // Log to a debug output or internal log.
                return false;
            }
        }
    }
}
