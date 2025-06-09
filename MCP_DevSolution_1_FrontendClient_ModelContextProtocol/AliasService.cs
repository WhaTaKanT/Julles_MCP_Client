using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class AliasService
    {
        private string _aliasesFilePath = "Aliases.json";
        // No internal static list needed here if Load/Save always operate on passed/returned list.
        // However, if ProcessCommand needs an in-memory current list without it being passed,
        // then an instance list `_aliases` would be needed, loaded by LoadAliasesAsync.
        // For now, let's stick to the plan: Load returns, Save takes, Process takes.

        public async Task<List<Alias>> LoadAliasesAsync()
        {
            if (!File.Exists(_aliasesFilePath))
            {
                return new List<Alias>();
            }

            try
            {
                string jsonString = await Task.Run(() => File.ReadAllText(_aliasesFilePath));
                var loadedAliases = JsonSerializer.Deserialize<List<Alias>>(jsonString);
                return loadedAliases ?? new List<Alias>();
            }
            catch (JsonException)
            {
                return new List<Alias>(); // Error in JSON format
            }
            catch (IOException)
            {
                return new List<Alias>(); // Error reading file
            }
        }

        public async Task<bool> SaveAliasesAsync(List<Alias> aliasesToSave)
        {
            if (aliasesToSave == null)
            {
                // Consider null as a failure to save, or save an empty list.
                // For consistency with ProfileService, let's return false for null.
                return false;
            }

            var aliasesToSaveCopy = aliasesToSave.Select(a => new Alias(a.AliasPhrase, a.ReplacementText)).ToList();

            try
            {
                string jsonString = JsonSerializer.Serialize(aliasesToSaveCopy, new JsonSerializerOptions { WriteIndented = true });
                await Task.Run(() => File.WriteAllText(_aliasesFilePath, jsonString));
                return true;
            }
            catch (JsonException)
            {
                // Log to debug/internal log
                return false;
            }
            catch (IOException)
            {
                // Log to debug/internal log
                return false;
            }
        }

        public string ProcessCommand(string commandInput, List<Alias> currentAliases)
        {
            if (string.IsNullOrWhiteSpace(commandInput) || currentAliases == null)
            {
                return commandInput;
            }

            string[] parts = commandInput.Split(new[] { ' ' }, 2, System.StringSplitOptions.None);
            string potentialAliasPhrase = parts[0];
            string argumentsString = (parts.Length > 1) ? parts[1] : string.Empty;

            string[] args = string.IsNullOrEmpty(argumentsString)
                            ? new string[0]
                            : argumentsString.Split(' '); // Standard space splitting. Consider StringSplitOptions for more complex cases if needed.

            var matchedAlias = currentAliases.FirstOrDefault(a => a.AliasPhrase.Equals(potentialAliasPhrase, System.StringComparison.OrdinalIgnoreCase));

            if (matchedAlias == null)
            {
                return commandInput; // No alias matched the first word
            }

            string result = matchedAlias.ReplacementText;

            // Substitute numbered parameters %1 to %9
            for (int i = 1; i <= 9; i++)
            {
                string placeholder = $"%{i}";
                if (result.Contains(placeholder)) // Optimization: only replace if placeholder exists
                {
                    if (i - 1 < args.Length)
                    {
                        result = result.Replace(placeholder, args[i - 1]);
                    }
                    else
                    {
                        result = result.Replace(placeholder, string.Empty); // Argument not provided
                    }
                }
            }

            // %* and %0 are deferred for now based on simplified logic.
            // If they were to be implemented with higher precedence or specific rules:
            // string allArgsString = string.Join(" ", args);
            // if (result.Contains("%*")) result = result.Replace("%*", allArgsString);
            // if (result.Contains("%0")) result = result.Replace("%0", allArgsString);


            return result;
        }
    }
}
