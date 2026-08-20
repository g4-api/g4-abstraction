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
        #region *** Constants    ***
        // Retains the published nested-expression pattern for subclass overrides and malformed-input fallback.
        private const string DefaultNestedCliExpressionPattern = @"\{\{\$.*?(?<={{[$]).*}}";

        // Marks the delimiter that closes one nested CLI expression level.
        private const string NestedCliExpressionEnd = "}}";

        // Marks the delimiter that opens one nested CLI expression level.
        private const string NestedCliExpressionStart = "{{$";
        #endregion

        #region *** Properties   ***
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
        /// <remarks>
        /// Overrides continue to use regular-expression extraction. The built-in pattern uses balanced delimiter
        /// scanning and retains this pattern as a compatibility fallback for malformed input.
        /// </remarks>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected virtual string NestedCliExpressionPattern => DefaultNestedCliExpressionPattern;

        /// <summary>
        /// Gets a value indicating whether the object is compliant with the Command-Line Interface (CLI) standard or format.
        /// </summary>
        public bool IsCliCompliant { get; }
        #endregion

        #region *** Methods      ***
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
        /// <param name="normalize">Indicates whether to normalize the keys in the resulting dictionary default is <c>true</c>.</param>
        /// <returns>A dictionary of parsed CLI arguments with case-insensitive keys.</returns>
        public IDictionary<string, string> ConvertToDictionary(string cli, bool normalize)
        {
            // Delegate the conversion to the ConvertToDictionary method with default patterns.
            return ConvertToDictionary(
                cli,
                cliPattern: CliTemplatePattern,
                argumentPattern: ArgumentPattern,
                expressionPattern: NestedCliExpressionPattern,
                keyPattern: ArgumentKeyPattern,
                valuePattern: ArgumentValuePattern,
                normalize);
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
                valuePattern: ArgumentValuePattern,
                normalize: true);
        }

        // Parses a Command-Line Interface (CLI) string into a dictionary of key-value pairs.
        private static Dictionary<string, string> ConvertToDictionary(
            string cli,
            string cliPattern,
            string argumentPattern,
            string expressionPattern,
            string keyPattern,
            string valuePattern,
            bool normalize)
        {
            // Check if the 'cli' string is null or empty.
            // If 'cli' is null or empty, return an empty dictionary with case-insensitive key comparison.
            if (string.IsNullOrEmpty(cli))
            {
                return new(StringComparer.OrdinalIgnoreCase);
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
            var arguments = ExportKeyValues(
                argumentsList,
                keyPattern,
                valuePattern,
                normalize);

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
            // Select complete built-in expressions without merging adjacent values, while retaining custom regex behavior.
            var nestedExpressions = GetNestedExpressions(cli, expressionPattern);

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
            IEnumerable<string> arguments,
            string keyPattern,
            string valuePattern,
            bool normalize)
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
            foreach (var group in arguments.GroupBy(i => Regex.Match(i, keyPattern).Value))
            {
                // Convert the group key to uppercase for consistent processing
                var groupKey = group.Key.ToUpper();

                // Get the key for the current group of arguments
                var key = normalize
                    ? ConvertToPascalCase(groupKey)
                    : group.Key;

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

        // Finds the exclusive end index of one balanced nested expression without allocating intermediate substrings.
        // The scan owns no state outside the call and reports an unmatched opening delimiter through a negative index.
        private static int GetNestedExpressionEnd(string cli, int expressionStart)
        {
            // Start after the known opening delimiter so every later opener contributes one nested level.
            var depth = 1;
            var index = expressionStart + NestedCliExpressionStart.Length;

            while (index < cli.Length)
            {
                // Track inner expressions so their closing delimiters remain part of the outer parameter value.
                if (cli.AsSpan(index).StartsWith(NestedCliExpressionStart, StringComparison.Ordinal))
                {
                    depth++;
                    index += NestedCliExpressionStart.Length;
                    continue;
                }

                // Close one expression level and return only when the original opening delimiter is balanced.
                if (cli.AsSpan(index).StartsWith(NestedCliExpressionEnd, StringComparison.Ordinal))
                {
                    depth--;
                    index += NestedCliExpressionEnd.Length;

                    if (depth == 0)
                    {
                        return index;
                    }

                    continue;
                }

                index++;
            }

            // Signal malformed input so the caller can preserve the parser's historical regex behavior.
            return -1;
        }

        // Selects each outermost nested CLI value through balanced delimiters for the built-in parser pattern.
        // Custom patterns and malformed built-in expressions retain regex extraction for compatibility.
        private static IEnumerable<string> GetNestedExpressions(string cli, string expressionPattern)
        {
            // Preserve the public subclass extension point by routing every custom pattern through its original matcher.
            if (!string.Equals(expressionPattern, DefaultNestedCliExpressionPattern, StringComparison.Ordinal))
            {
                return Regex.Matches(cli, expressionPattern).Select(match => match.Value);
            }

            var nestedExpressions = new List<string>();
            var searchIndex = 0;

            while (searchIndex < cli.Length)
            {
                // Find the next delimiters after the previous complete expression.
                var expressionStart = cli.IndexOf(NestedCliExpressionStart, searchIndex, StringComparison.Ordinal);
                var unexpectedExpressionEnd = cli.IndexOf(NestedCliExpressionEnd, searchIndex, StringComparison.Ordinal);

                // Preserve historical parsing when a closing delimiter appears outside a nested expression.
                var hasUnmatchedEnd = unexpectedExpressionEnd >= 0 &&
                    (expressionStart < 0 || unexpectedExpressionEnd < expressionStart);

                if (hasUnmatchedEnd)
                {
                    return Regex.Matches(cli, expressionPattern).Select(match => match.Value);
                }

                if (expressionStart < 0)
                {
                    return nestedExpressions;
                }

                // Match this value independently so later sibling arguments cannot be absorbed into it.
                var expressionEnd = GetNestedExpressionEnd(cli, expressionStart);

                if (expressionEnd < 0)
                {
                    // Preserve historical parsing for incomplete templates instead of exposing new top-level arguments.
                    return Regex.Matches(cli, expressionPattern).Select(match => match.Value);
                }

                nestedExpressions.Add(cli[expressionStart..expressionEnd]);
                searchIndex = expressionEnd;
            }

            return nestedExpressions;
        }
        #endregion
    }
}
