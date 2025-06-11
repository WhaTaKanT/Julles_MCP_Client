using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class LlmConfigurationService
    {
        private string _configFilePath = "LlmConfigs.json";
        // No in-memory list _configs here; Load will return a new list, Save will take a list.
        // This makes the service stateless and reliant on the ViewModel to hold the current state.

        public async Task<List<LlmConfiguration>> LoadConfigurationsAsync()
        {
            if (!File.Exists(_configFilePath))
            {
                return new List<LlmConfiguration>(); // Return empty list if file doesn't exist
            }

            try
            {
                string jsonString = await Task.Run(() => File.ReadAllText(_configFilePath));
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return new List<LlmConfiguration>(); // Return empty list if file is empty
                }

                var loadedConfigs = JsonSerializer.Deserialize<List<LlmConfiguration>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Good practice for robustness
                });

                return loadedConfigs ?? new List<LlmConfiguration>();
            }
            catch (JsonException ex)
            {
                // Log this error appropriately (e.g., to a debug output or a dedicated error log)
                // For now, just printing to console for visibility during development.
                System.Diagnostics.Debug.WriteLine($"Error deserializing LlmConfigs.json: {ex.Message}");
                return new List<LlmConfiguration>(); // Return empty list on error
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading LlmConfigs.json: {ex.Message}");
                return new List<LlmConfiguration>(); // Return empty list on error
            }
        }

        public async Task<bool> SaveConfigurationsAsync(List<LlmConfiguration> configsToSave)
        {
            if (configsToSave == null)
            {
                // It's debatable whether saving a null list should be an error or save an empty list.
                // For clarity, let's consider it an operation that shouldn't happen / indicate failure.
                System.Diagnostics.Debug.WriteLine("Attempted to save a null list of LLM configurations.");
                return false;
            }

            try
            {
                // Create a list of clones to ensure we're saving the current state of the objects,
                // especially if they are bound to UI and could change.
                // LlmConfiguration must have a Clone() method for this to work.
                var clonedConfigs = configsToSave.Select(c => c.Clone()).ToList();

                string jsonString = JsonSerializer.Serialize(clonedConfigs, new JsonSerializerOptions {
                    WriteIndented = true,
                    // Optional: Add other options like PropertyNamingPolicy if needed for specific format
                });

                await Task.Run(() => File.WriteAllText(_configFilePath, jsonString));
                return true;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error serializing LLM configurations to JSON: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing LLM configurations to LlmConfigs.json: {ex.Message}");
                return false;
            }
            catch (System.Exception ex) // Catch other potential errors during cloning or serialization
            {
                System.Diagnostics.Debug.WriteLine($"An unexpected error occurred while saving LLM configurations: {ex.Message}");
                return false;
            }
        }
    }
}
