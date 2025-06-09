namespace MCP_DevSolution_1_FrontendClient_ModelContextProtocol
{
    public class Alias
    {
        public string AliasPhrase { get; set; }
        public string ReplacementText { get; set; }

        public Alias()
        {
            AliasPhrase = string.Empty;
            ReplacementText = string.Empty;
        }

        // Optional: Constructor for convenience
        public Alias(string phrase, string replacement)
        {
            AliasPhrase = phrase;
            ReplacementText = replacement;
        }
    }
}
