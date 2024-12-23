using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace G4.Abstraction.Cli
{
    /// <summary>
    /// A factory for generating command-line interfaces (CLI) and related patterns.
    /// </summary>
    public class CliFactory
    {
        #region *** Properties ***
        /// <summary>
        /// Gets the regular expression pattern for extracting the CLI template from a larger string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected virtual string CliTemplatePattern => "(?<={{[$]).*(?=(}}))";

        /// <summary>
        /// Gets the regular expression pattern for extracting individual CLI arguments from the CLI template.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected virtual string ArgumentPattern => @"(?<=--)(.*?)(?=\s+--[\w,/,\.,\$,\*]|$)";

        /// <summary>
        /// Gets the regular expression pattern for extracting keys from individual CLI arguments.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected virtual string ArgumentKeyPattern => "(?si)^[^:]*";

        /// <summary>
        /// Gets the regular expression pattern for extracting values from individual CLI arguments.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected virtual string ArgumentValuePattern => "(?<=(:)).*$";

        /// <summary>
        /// Gets the regular expression pattern for extracting nested CLI expressions within the template.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected virtual string NestedCliExpressionPattern => @"\{\{\$.*?(?<={{[$]).*}}";

        /// <summary>
        /// Gets a value indicating whether the object is compliant with the Command-Line Interface (CLI) standard or format.
        /// </summary>
        public bool IsCliCompliant { get; }
        #endregion

        #region *** Methods    ***
        /// <summary>
        /// Confirms the validity of a Command-Line Interface (CLI) against the current CLI template pattern.
        /// </summary>
        /// <param name="cli">The CLI to confirm.</param>
        /// <returns>True if the CLI is valid against the current CLI template pattern, otherwise false.</returns>
        public bool ConfirmCli(string cli)
        {
            // Ensure the CLI is not null
            cli ??= string.Empty;

            // Check if the CLI matches the specified template pattern
            if (!Regex.IsMatch(cli, CliTemplatePattern, RegexOptions.Singleline))
            {
                return false;
            }

            // The provided CLI is valid according to the specified template pattern,
            // so return true to confirm its validity.
            return true;
        }

        /// <summary>
        /// Converts a Command-Line Interface (CLI) string into a dictionary of key-value pairs using default patterns.
        /// </summary>
        /// <param name="cli">The CLI string to convert.</param>
        /// <returns>A dictionary of parsed CLI arguments with case-insensitive keys.</returns>
        public IDictionary<string, string> ConvertToDictionary(string cli)
        {
            // Delegate the conversion to the ConvertToDictionary method with default patterns.
            return ConvertToDictionary(
                cli,
                cliPattern: CliTemplatePattern,
                argumentPattern: ArgumentPattern,
                expressionPattern: NestedCliExpressionPattern,
                keyPattern: ArgumentKeyPattern,
                valuePattern: ArgumentValuePattern);
        }

        // Parses a Command-Line Interface (CLI) string into a dictionary of key-value pairs.
        private static Dictionary<string, string> ConvertToDictionary(
            string cli,
            string cliPattern,
            string argumentPattern,
            string expressionPattern,
            string keyPattern,
            string valuePattern)
        {
            // Check if the 'cli' string is null or empty.
            // If 'cli' is null or empty, return an empty dictionary with case-insensitive key comparison.
            if (string.IsNullOrEmpty(cli))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            // Extract the clean CLI string by matching the CLI pattern.
            var cleanCli = Regex.Match(cli, cliPattern, RegexOptions.Singleline).Value.Trim();

            // Replace nested patterns with placeholders and store the mapping.
            var nestedExpressionMap = ExportNestedExpressions(cleanCli, expressionPattern);
            foreach (var item in nestedExpressionMap)
            {
                cleanCli = cleanCli.Replace(item.Key, item.Value);
            }

            // Extract individual CLI arguments.
            var argumentMatches = Regex.Matches(cleanCli, argumentPattern, RegexOptions.Singleline);
            var argumentsList = argumentMatches
                .Cast<Match>()
                .Select(match => match.Value.Trim())
                .Where(arg => !string.IsNullOrEmpty(arg));

            // Create a dictionary to store the parsed CLI arguments.
            var arguments = ExportKeyValues(argumentsList, keyPattern, valuePattern);

            // Serialize the dictionary to JSON for processing nested patterns.
            var argumentsJson = JsonSerializer.Serialize(arguments);

            // Replace the placeholders with their original nested patterns.
            foreach (var item in nestedExpressionMap)
            {
                argumentsJson = argumentsJson.Replace(item.Value, item.Key);
            }

            // Deserialize the JSON back into a dictionary and return it.
            var collection = JsonSerializer.Deserialize<IDictionary<string, string>>(argumentsJson);

            // Create a new dictionary with case-insensitive key comparison and return it.
            return new Dictionary<string, string>(collection, StringComparer.OrdinalIgnoreCase);
        }

        // Extracts nested Command-Line Interface (CLI) expressions and encodes them for mapping.
        private static Dictionary<string, string> ExportNestedExpressions(string cli, string expressionPattern)
        {
            // Use regular expressions to find nested CLI expressions and select them.
            var nestedExpressions = Regex.Matches(cli, expressionPattern).Select(match => match.Value);

            // Create a dictionary to store the nested expressions and their encoded values.
            var expressionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var expression in nestedExpressions)
            {
                // Encode the nested expression as a Base64 string and add it to the dictionary.
                expressionMap[expression] = Convert.ToBase64String(Encoding.UTF8.GetBytes(expression));
            }

            // Return the dictionary containing nested expression mappings.
            return expressionMap;
        }

        // Extracts key-value pairs from a collection of arguments based on specified key and value patterns.
        private static Dictionary<string, string> ExportKeyValues(
            IEnumerable<string> arguments, string keyPattern, string valuePattern)
        {
            // Local function to convert a string to PascalCase
            static string ConvertToPascalCase(string input)
            {
                // Regular expressions to match different patterns
                var invalidCharsRegex = new Regex("[^_a-zA-Z0-9]");
                var whiteSpaceRegex = new Regex(@"(?<=\s)");
                var startsWithLowerCaseRegex = new Regex("^[a-z]");
                var firstCharFollowedByUpperCasesOnlyRegex = new Regex("(?<=[A-Z])[A-Z0-9]+$");
                var lowerCaseNextToNumberRegex = new Regex("(?<=[0-9])[a-z]");
                var upperCaseInsideRegex = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

                // Convert the input string to PascalCase
                var pascalCase = invalidCharsRegex.Replace(whiteSpaceRegex.Replace(input, "_"), string.Empty)
                    // Split by underscores
                    .Split("_", StringSplitOptions.RemoveEmptyEntries)
                    // Set first letter to uppercase
                    .Select(word => startsWithLowerCaseRegex.Replace(word, match => match.Value.ToUpper()))
                    // Replace second and all following uppercase letters to lowercase if there is no next lowercase (ABC -> Abc)
                    .Select(word => firstCharFollowedByUpperCasesOnlyRegex.Replace(word, match => match.Value.ToLower()))
                    // Set uppercase the first lowercase following a number (Ab9cd -> Ab9Cd)
                    .Select(word => lowerCaseNextToNumberRegex.Replace(word, match => match.Value.ToUpper()))
                    // Lower second and next uppercase letters except the last if it follows by any lowercase (ABcDEf -> AbcDef)
                    .Select(word => upperCaseInsideRegex.Replace(word, match => match.Value.ToLower()));

                // Concatenate the result and return
                return string.Concat(pascalCase);
            }

            // Local function to extract a value from an argument using a pattern
            static string ExtractValue(string argument, string pattern)
            {
                return Regex
                    .Match(argument, pattern, RegexOptions.Singleline)
                    .Value ?? string.Empty;
            }

            // Create a dictionary to store results with case-insensitive key comparison
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Group the arguments by their key using the specified key pattern
            foreach (var group in arguments.GroupBy(i => Regex.Match(i.ToUpper(), keyPattern).Value))
            {
                // Get the key for the current group of arguments
                var key = ConvertToPascalCase(group.Key);

                // Check if the group has no elements (arguments)
                if (!group.Any())
                {
                    // If the group is empty, set the result for the key to an empty string
                    results[key] = string.Empty;

                    // Continue to the next group
                    continue;
                }

                // Determine whether to serialize the values as a single value or as an array
                // Assign the extracted value to the corresponding key in the results dictionary
                results[key] = group.Count() == 1
                    ? ExtractValue(argument: group.First(), pattern: valuePattern)
                    : JsonSerializer.Serialize(group.Select(i => ExtractValue(argument: i, pattern: valuePattern)));
            }

            // Return the populated results dictionary containing extracted key-value pairs
            return results;
        }
        #endregion
    }
}
