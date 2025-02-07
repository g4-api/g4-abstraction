using G4.Abstraction.WebDriver;
using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace G4.Extensions
{
    /// <summary>
    /// Provides extension methods and utilities for the framework.
    /// </summary>
    internal static class LocalExtensions
    {
        // JSON serialization options used across the extensions
        private readonly static JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Confirms whether the specified type is a valid driver plugin by checking if it has the DriverPluginAttribute
        /// and if it derives from DriverPluginBase.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns>True if the type is a valid driver plugin, otherwise false.</returns>
        public static bool ConfirmDriverPlugin(this Type type)
        {
            // Check if the type has the DriverPluginAttribute
            var isDriverPlugin = type.GetCustomAttribute<G4DriverPluginAttribute>() != null;

            // Check if the type is derived from DriverPluginBase
            var isDriverPluginBase = typeof(DriverPluginBase).IsAssignableFrom(type);

            // Return true if both conditions are met, otherwise false
            return isDriverPlugin && isDriverPluginBase;
        }

        /// <summary>
        /// Converts a dictionary to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to convert to.</typeparam>
        /// <param name="dictionary">The dictionary to convert.</param>
        /// <returns>An object of the specified type.</returns>
        public static T ConvertToObject<T>(this IDictionary<string, object> dictionary)
        {
            // Serialize the dictionary to JSON string
            var json = JsonSerializer.Serialize(dictionary, s_jsonOptions);

            // Deserialize the JSON string to an object of the specified type
            return JsonSerializer.Deserialize<T>(json, s_jsonOptions);
        }

        /// <summary>
        /// Converts a <see cref="SessionModel"/> to an instance of <typeparamref name="T"/>, which is a subclass of <see cref="WebDriverOptionsBase"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="WebDriverOptionsBase"/> to convert to.</typeparam>
        /// <param name="model">The <see cref="SessionModel"/> containing the desired and always-match capabilities.</param>
        /// <returns>An instance of <typeparamref name="T"/> with properties set based on the capabilities in the <paramref name="model"/>.</returns>
        public static T ConvertToOptions<T>(this SessionModel model) where T : WebDriverOptionsBase, new()
        {
            // Define BindingFlags and StringComparison to be used
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            const StringComparison Compare = StringComparison.OrdinalIgnoreCase;

            // Ensure the model is not null
            model ??= new SessionModel();

            // Create an instance of the specified type
            var instance = new T();

            // Get all public instance properties of the specified type
            var targetProperties = instance
                .GetType()
                .GetProperties(Flags)
                .Where(i => i.SetMethod != null);

            // Set properties based on capabilities always matching
            foreach (var item in model.Capabilities.AlwaysMatch)
            {
                var property = targetProperties.FirstOrDefault(i => i.Name.Equals(item.Key, Compare));
                property?.SetValue(instance, item.Value);
            }

            // Return the instance with properties set
            return instance;
        }

        /// <summary>
        /// Finds a value in a nested dictionary structure by a specified path.
        /// </summary>
        /// <typeparam name="T">The type of the value to find.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="path">The path to the value.</param>
        /// <param name="defaultValue">The default value to return if the value is not found.</param>
        /// <returns>The value found at the specified path, or the default value if not found.</returns>
        public static T Find<T>(this IDictionary<string, object> dictionary, string path, T defaultValue)
        {
            try
            {
                // Serialize the dictionary to JSON string
                var json = JsonSerializer.Serialize(dictionary, s_jsonOptions);

                // Parse the JSON string into a JToken
                var token = Newtonsoft.Json.Linq.JToken.Parse(json);

                // Select the token corresponding to the specified path
                var value = token.SelectToken(path);

                // If no value is found at the specified path, return the default value
                if (value == null)
                {
                    return defaultValue;
                }

                // Check if the value is in JSON format
                var isJson = $"{value}".AssertJson();

                // Deserialize the value to the specified type if it is in JSON format
                if (isJson)
                {
                    return JsonSerializer.Deserialize<T>($"{value}", s_jsonOptions);
                }

                // Otherwise, convert the value to the specified type
                return value.ToObject<T>();
            }
            catch
            {
                // If an exception occurs, return the default value
                return defaultValue;
            }
        }

        /// <summary>
        /// Finds and returns a JsonDocument by name with case-insensitive comparison.
        /// </summary>
        /// <param name="document">The JsonDocument to search.</param>
        /// <param name="name">The name to search for.</param>
        /// <returns>A JsonDocument with the specified name or null if not found.</returns>
        public static JsonDocument FindByName(this JsonDocument document, string name)
        {
            // Call the overload with case-insensitive comparison
            return FindByName(document, name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Finds and returns a JsonDocument by name with the specified string comparison.
        /// </summary>
        /// <param name="document">The JsonDocument to search.</param>
        /// <param name="name">The name to search for.</param>
        /// <param name="stringComparison">The string comparison to use.</param>
        /// <returns>A JsonDocument with the specified name or null if not found.</returns>
        public static JsonDocument FindByName(
            this JsonDocument document,
            string name,
            StringComparison stringComparison)
        {
            // Check if the document is null and throw an ArgumentNullException if so
            ArgumentNullException.ThrowIfNull(document);

            // Configure Newtonsoft.Json settings for serialization
            var newtonsoftSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.None,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            // Convert the JsonDocument to a string
            var json = document.ToString();

            // Parse the string into a JObject
            var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(json);

            // Convert the JObject to a Dictionary
            var collection = jsonObject.ToObject<Dictionary<string, object>>();

            // If the collection is null, return the original document
            if (collection == null)
            {
                return document;
            }

            // Find the first pair in the collection with the specified name and string comparison
            var pair = collection.FirstOrDefault(i => i.Key.Equals(name, stringComparison));

            // If no pair is found, return null
            if (EqualityComparer<KeyValuePair<string, object>>.Default.Equals(pair, default))
            {
                return default;
            }

            // Create a new dictionary with the found pair
            var token = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [pair.Key] = pair.Value
            };

            // Serialize the dictionary to JSON
            var tokenJson = Newtonsoft
                .Json
                .JsonConvert
                .SerializeObject(token, newtonsoftSettings);

            // Parse the JSON string into a new JsonDocument and return it
            return JsonDocument.Parse(tokenJson);
        }
    }
}
