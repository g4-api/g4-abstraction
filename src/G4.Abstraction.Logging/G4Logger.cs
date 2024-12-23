/*
 * WORK ITEMS
 * 
 * [ ] TODO: Add support for limiting the size of log files and implementing log file rotation.
 */
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace G4.Abstraction.Logging
{
    /// <summary>
    /// Represents a logger implementation specific to G4.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    [ProviderAlias("G4.Api")]
    public class G4Logger : ILogger
    {
        #region *** Fields       ***
        /// <summary>
        /// The default logger instance.
        /// </summary>
        public static readonly ILogger Instance = NewLogger("G4.Api");

        // Serializer options for JSON log entries.
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        // The current configuration settings for the logger.
        private readonly G4LoggerSettings _settings;
        #endregion

        #region *** Events       ***
        /// <summary>
        /// Event triggered before creating a log entry.
        /// </summary>
        public event EventHandler<IDictionary<string, object>> LogCreating;

        /// <summary>
        /// Event triggered after creating a log entry.
        /// </summary>
        public event EventHandler<IDictionary<string, object>> LogCreated;

        /// <summary>
        /// Event triggered when an error occurs during logging.
        /// </summary>
        public event EventHandler<Exception> LogError;
        #endregion

        #region *** Constructors ***
        /// <summary>
        /// Initializes a new instance of the <see cref="G4Logger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="getCurrentConfig">Function to get the current configuration settings for the logger.</param>
        public G4Logger(string name, Func<G4LoggerSettings> getCurrentConfig)
        {
            // Retrieve the current configuration settings.
            _settings = getCurrentConfig();

            // Set the logger name to the provided name.
            Name = name;

            // Set up trace listener using provided name and output directory from configuration.
            InitializeTraceListener(instanceName: name, inDirectory: _settings.G4Logger.OutputDirectory);
        }
        #endregion

        #region *** Properties   ***
        /// <summary>
        /// Gets the name of the logger.
        /// </summary>
        public string Name { get; }
        #endregion

        #region *** Methods      ***
        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => default!;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => ConfirmLogLevel(_settings, Name, logLevel);

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                // Check if the specified log level is enabled based on the current configuration settings.
                if (!ConfirmLogLevel(_settings, Name, logLevel))
                {
                    return;
                }

                // Verify if the provided event ID matches the configured event ID.
                if (_settings.EventId != 0 && _settings.EventId != eventId.Id)
                {
                    return;
                }

                // Construct a dictionary to hold log message details.
                var logMessage = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Logger"] = Name,
                    ["LogLevel"] = $"{logLevel}",
                    ["TimeStamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"),
                    ["MachineName"] = Environment.MachineName,
                    ["Message"] = formatter != null ? formatter(state, default) : $"{state}",
                    ["Exception"] = exception
                };

                // Format additional fields from the state object and add them to the log message.
                FormatFields(state, logMessage);

                // Trigger the LogCreating event to allow external handling or modification of the log entries.
                LogCreating?.Invoke(sender: this, e: logMessage);

                // Determine the format of the log entry based on the configured log type.
                var logEntry = _settings.G4Logger.Type.ToUpper() switch
                {
                    "XML" => "XML log type is not implemented. Please use 'JSON', 'Text' or 'Simple'.",
                    "JSON" => JsonSerializer.Serialize(logMessage, s_jsonOptions),
                    "SIMPLE" => $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.ffffffK} {GetLogLevel(logLevel)} - {state}",
                    _ => ConvertToReadableString(logLevel, logMessage)
                };

                // Write the formatted log entry to the trace listener for further processing or storage.
                WriteTrace(logEntry, logLevel);

                // Trigger the LogCreated event to notify that the log entry has been successfully created.
                LogCreated?.Invoke(sender: this, e: logMessage);
            }
            catch (Exception e)
            {
                // If an error occurs during the logging process, trigger the LogError event to handle the exception.
                LogError?.Invoke(sender: this, e);
            }
        }

        // Appends the default log message properties to the log entry.
        private static void AddDefaults(Dictionary<string, object> logMessage, int maxLength, StringBuilder log)
        {
            // Appends a log message property to the log entry.
            static void AddKey(IDictionary<string, object> logMessage, int maxLength, string key, StringBuilder log)
            {
                // Append the log message property to the log entry.
                log
                    .Append("    ")                    // Add indentation.
                    .Append(key)                       // Append the property key.
                    .Append(GetIndentation(key, maxLength)) // Add indentation padding.
                    .Append(": ")                      // Add separator.
                    .Append(logMessage[key])           // Append the property value.
                    .AppendLine();                     // Add line break.
            }

            // Define constant keys for standard log message properties.
            const string Logger = "Logger";
            const string LogLevel = "LogLevel";
            const string TimeStamp = "TimeStamp";
            const string MachineName = "MachineName";
            const string Message = "Message";

            // Append each default log message property if present.
            if (logMessage.ContainsKey(Logger))
            {
                // Append the logger property to the log entry.
                AddKey(logMessage, maxLength, Logger, log);
            }
            if (logMessage.ContainsKey(LogLevel))
            {
                // Append the log level property to the log entry.
                AddKey(logMessage, maxLength, LogLevel, log);
            }
            if (logMessage.ContainsKey(TimeStamp))
            {
                // Append the timestamp property to the log entry.
                AddKey(logMessage, maxLength, TimeStamp, log);
            }
            if (logMessage.ContainsKey(MachineName))
            {
                // Append the machine name property to the log entry.
                AddKey(logMessage, maxLength, MachineName, log);
            }
            if (logMessage.ContainsKey(Message))
            {
                // Append the message property to the log entry.
                AddKey(logMessage, maxLength, Message, log);
            }
        }

        // Checks if the specified log level is enabled in the configuration.
        private static bool ConfirmLogLevel(G4LoggerSettings settings, string logName, LogLevel logLevel)
        {
            // If log level is None, it is disabled.
            if (logLevel == LogLevel.None)
            {
                return false;
            }

            // Retrieve the log level configuration.
            var logConfiguration = settings.LogLevel ?? new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
            {
                ["Default"] = LogLevel.Information
            };

            // Check if the log level is specified for the logger or defaults to the default log level.
            var isKey = logConfiguration.ContainsKey(logName) && logConfiguration[logName].Equals(logLevel);
            var isDefault = logConfiguration.ContainsKey("Default") && logConfiguration["Default"].Equals(logLevel);
            var isCompliant = isKey || isDefault;

            // If the log level is compliant or greater than or equal to Information, it is enabled.
            return isCompliant || logLevel >= LogLevel.Information;
        }

        // Converts a log message and its details to a human-readable string.
        private static string ConvertToReadableString(LogLevel logLevel, Dictionary<string, object> logMessage)
        {
            // Appends the exception entry to the log entry.
            static StringBuilder AddExceptionEntry(IDictionary<string, object> logMessage, StringBuilder log)
            {
                // Append a separator and the exception header.
                log.AppendLine("----------------");
                log.AppendLine("- Exception(s) -");
                log.AppendLine("----------------");

                // Append the exception details.
                log.Append(logMessage["Exception"]);

                // Return the log entry string builder with the exception entry appended.
                return log.AppendLine();
            }

            // Exclude standard log message properties from the detailed output.
            var exclude = new[]
            {
                "Logger",
                "LogLevel",
                "TimeStamp",
                "MachineName",
                "Exception",
                "Message"
            };

            // Get all keys from the log message.
            var keys = logMessage.Keys.ToArray();

            // Get the maximum length of the keys for indentation.
            var maxLength = keys.Max(i => i.Length);

            // Filter out standard log message properties.
            var pairs = logMessage.Where(i => !exclude.Contains(i.Key));

            // Get the log level in string representation.
            var level = GetLogLevel(logLevel);

            // Initialize the log message string builder.
            var log = new StringBuilder($"{level} - {logMessage["TimeStamp"]}{Environment.NewLine}");

            // Append standard log message properties.
            AddDefaults(logMessage, maxLength, log);

            // Append detailed log message properties.
            foreach (var pair in pairs)
            {
                var indent = GetIndentation(pair.Key, maxLength);
                var message = $"{pair.Value}".Replace(Environment.NewLine, string.Empty);
                log.Append("    ").Append(pair.Key).Append(indent).Append(": ").AppendLine(message);
            }

            // Check if an exception is included in the log message.
            return logMessage.TryGetValue("Exception", out object value) && value != null
                ? AddExceptionEntry(logMessage, log).ToString()
                : log.ToString();
        }

        // Formats and populates the provided fields dictionary based on the <paramref name="state"/>.
        private static void FormatFields<TState>(TState state, Dictionary<string, object> fields)
        {
            // Determine if the state object is a read-only list of key-value pairs.
            var isDictionary = state is IReadOnlyList<KeyValuePair<string, object>>;

            // Check if the string representation of the state matches the pattern "$(...)"
            var isCollection = Regex.IsMatch($"{state}", @"^\$\(.+\)$");

            // If the state is not a dictionary or does not match the collection pattern, exit the method.
            if (!isDictionary || !isCollection)
            {
                return;
            }

            // Cast the state object to a read-only list of key-value pairs for iteration.
            if (state is not IReadOnlyList<KeyValuePair<string, object>> keyValuePairs)
            {
                return;
            }

            // Iterate through each key-value pair in the state.
            foreach (var entry in keyValuePairs)
            {
                // Skip the entry with the key "{OriginalFormat}" as it is used for formatting purposes.
                if (entry.Key.Equals("{OriginalFormat}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Add or update the corresponding key-value pair in the fields dictionary.
                fields[entry.Key] = entry.Value;
            }

            // Ensure the "Message" field is properly formatted.
            fields["Message"] = fields.TryGetValue("Message", out object value)
                ? $"{value}"
                : string.Empty;
        }

        // Gets the indentation padding for a log message property.
        private static string GetIndentation(string key, int maxLength)
        {
            // Calculate the indentation by subtracting the length of the key from the maximum length.
            var indent = maxLength - key.Length;

            // Return a string of spaces with the calculated length.
            return new string(' ', indent);
        }

        // Gets the string representation of a log level.
        private static string GetLogLevel(LogLevel level) => level switch
        {
            LogLevel.Critical => "CRT",
            LogLevel.Debug => "DBG",
            LogLevel.Error => "ERR",
            LogLevel.Information => "INF",
            LogLevel.Trace => "TRC",
            LogLevel.Warning => "WRN",
            _ => "TRC" // Default to Trace if the log level is unrecognized.
        };

        // Initializes a trace listener with the specified instance name and directory.
        private static void InitializeTraceListener(string instanceName, string inDirectory)
        {
            // If instance name is not provided, default to "G4.Api".
            instanceName = string.IsNullOrEmpty(instanceName) ? "G4.Api" : instanceName;

            // Check if a trace listener with the specified instance name already exists.
            var exists = Trace
                .Listeners
                .Cast<TraceListener>()
                .Any(l => l.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase));

            // If the trace listener already exists, return.
            if (exists)
            {
                return;
            }

            // If the directory is not provided or is ".", default to the current directory.
            var directory = string.IsNullOrEmpty(inDirectory) || inDirectory.Equals(".")
                ? Environment.CurrentDirectory
                : inDirectory;

            // Create the directory if it doesn't exist.
            Directory.CreateDirectory(directory);

            // Construct the log file name.
            var fileName = Path.Combine(directory, $"{instanceName}.{DateTime.UtcNow:yyyyMMdd}.log");

            // Open or create the log file stream.
            var stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            // Create a text writer trace listener with the stream and instance name.
            var listener = new TextWriterTraceListener(stream, name: instanceName);

            // Enable auto flush for the trace listener.
            Trace.AutoFlush = true;

            // Add the trace listener to the Trace listeners collection.
            Trace.Listeners.Add(listener);
        }

        // Create a new logger instance with the specified category name.
        private static ILogger NewLogger(string categoryName)
        {
            try
            {
                // Load configuration settings from appsettings.json and environment variables
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Retrieve logging configuration settings
                var addDebug = configuration.GetSection("Logging:G4Logger:AddDebug").Get<bool>();
                var addConsole = configuration.GetSection("Logging:G4Logger:AddConsole").Get<bool>();

                // Create a logger factory with configured logging providers
                var factory = LoggerFactory.Create(builder =>
                {
                    builder?.AddConfiguration(configuration.GetSection("Logging"));
                    builder?.AddG4Logger();
                    if (addConsole)
                    {
                        builder?.AddConsole();
                    }
                    if (addDebug)
                    {
                        builder?.AddDebug();
                    }
                });

                // Create and return a logger associated with the specified type
                return factory?.CreateLogger(categoryName);
            }
            catch
            {
                // Return default logger instance in case of any exception
                return default;
            }
        }

        // Writes the log entry to the trace listener based on the log level.
        private static void WriteTrace(string logEntry, LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Warning:
                    {
                        // Write the log entry as a warning.
                        Trace.TraceWarning(logEntry);
                        break;
                    }
                case LogLevel.Error:
                case LogLevel.Critical:
                    {
                        // Write the log entry as an error or critical.
                        Trace.TraceError(logEntry);
                        break;
                    }
                default:
                    {
                        // Write the log entry as information.
                        Trace.TraceInformation(logEntry);
                        break;
                    }
            }
        }
        #endregion
    }
}
