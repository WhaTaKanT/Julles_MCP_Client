using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class TriggerService
    {
        private string _triggersFilePath = "Triggers.json";

        public async Task<List<Trigger>> LoadTriggersAsync()
        {
            if (!File.Exists(_triggersFilePath))
            {
                return new List<Trigger>();
            }

            try
            {
                string jsonString = await Task.Run(() => File.ReadAllText(_triggersFilePath));
                var loadedTriggers = JsonSerializer.Deserialize<List<Trigger>>(jsonString);
                return loadedTriggers ?? new List<Trigger>();
            }
            catch (JsonException)
            {
                return new List<Trigger>(); // Error in JSON format
            }
            catch (IOException)
            {
                return new List<Trigger>(); // Error reading file
            }
        }

        public async Task<bool> SaveTriggersAsync(List<Trigger> triggersToSave)
        {
            if (triggersToSave == null)
            {
                return false; // Or save an empty list, returning true. Consistent with others: false.
            }

            var triggersToSaveCopy = triggersToSave.Select(t => new Trigger
            {
                Pattern = t.Pattern,
                ActionType = t.ActionType,
                ActionValue = t.ActionValue,
                IsEnabled = t.IsEnabled
            }).ToList();

            try
            {
                string jsonString = JsonSerializer.Serialize(triggersToSaveCopy, new JsonSerializerOptions { WriteIndented = true });
                await Task.Run(() => File.WriteAllText(_triggersFilePath, jsonString));
                return true;
            }
            catch (JsonException)
            {
                // Log to debug/internal
                return false;
            }
            catch (IOException)
            {
                // Log to debug/internal
                return false;
            }
        }

        public string ProcessIncomingLine(string line, List<Trigger> activeTriggers, List<Alias> currentAliases, Func<string, List<Alias>, string> aliasProcessingMethod)
        {
            if (string.IsNullOrEmpty(line) || activeTriggers == null || currentAliases == null || aliasProcessingMethod == null)
            {
                return null;
            }

            foreach (var trigger in activeTriggers) // Already filtered for IsEnabled by ViewModel
            {
                // ViewModel should pass only IsEnabled triggers, but double check here for safety if called externally
                if (!trigger.IsEnabled) continue;

                try
                {
                    Match match = Regex.Match(line, trigger.Pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (trigger.ActionType == "Send Command")
                        {
                            string commandToSend = trigger.ActionValue;
                            // TODO: Implement Regex group substitution in ActionValue (FR.ADV.TRIG.02)
                            // For now, ActionValue is used directly.

                            string processedCommand = aliasProcessingMethod(commandToSend, currentAliases);
                            return processedCommand;
                        }
                        // Other action types can be added here
                    }
                }
                catch (RegexMatchTimeoutException) { /* Log or handle */ }
                catch (ArgumentException) { /* Log invalid regex pattern or handle */ }
            }
            return null;
        }
    }
}
