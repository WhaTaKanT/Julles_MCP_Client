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

        public async Task<List<ConnectionProfile>> LoadProfilesAsync()
        {
            if (!File.Exists(_profileFilePath))
            {
                return new List<ConnectionProfile>();
            }

            try
            {
                string jsonString = await Task.Run(() => File.ReadAllText(_profileFilePath));
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return new List<ConnectionProfile>();
                }

                // Deserialize directly. ConnectionProfile objects should have default constructors
                // and public setters for JSON deserialization to work correctly.
                var loadedProfiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return loadedProfiles ?? new List<ConnectionProfile>();
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing ConnectionProfiles.json: {ex.Message}");
                return new List<ConnectionProfile>(); // Return empty list on error
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading ConnectionProfiles.json: {ex.Message}");
                return new List<ConnectionProfile>(); // Return empty list on error
            }
        }

        public async Task<bool> SaveProfilesAsync(List<ConnectionProfile> profilesToSave)
        {
            if (profilesToSave == null)
            {
                System.Diagnostics.Debug.WriteLine("Attempted to save a null list of Connection Profiles.");
                return false;
            }

            try
            {
                // Assuming ConnectionProfile has a parameterless constructor and public setters for all properties being serialized.
                // The original code created new ConnectionProfile instances.
                // If ConnectionProfile objects need specific cloning logic beyond what simple property copying achieves (e.g. deep clone),
                // then a Clone() method on ConnectionProfile would be needed, similar to LlmConfiguration.
                // For now, assume direct serialization of the list is fine as JSON serializer creates its own representation.
                // Let's stick to the original behavior of creating copies to be safe, ensuring what's saved is a snapshot.
                var profilesToSaveCopy = profilesToSave.Select(p => new ConnectionProfile
                {
                    ProfileName = p.ProfileName,
                    ServerHost = p.ServerHost,
                    ServerPort = p.ServerPort,
                    Status = p.Status ?? "Offline" // Ensure status is not null
                }).ToList();

                string jsonString = JsonSerializer.Serialize(profilesToSaveCopy, new JsonSerializerOptions { WriteIndented = true });
                await Task.Run(() => File.WriteAllText(_profileFilePath, jsonString));
                return true;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error serializing Connection Profiles to JSON: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing Connection Profiles to ConnectionProfiles.json: {ex.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An unexpected error occurred while saving Connection Profiles: {ex.Message}");
                return false;
            }
        }
    }
}
