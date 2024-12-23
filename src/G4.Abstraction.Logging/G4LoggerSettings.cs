using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;

namespace G4.Abstraction.Logging
{
    /// <summary>
    /// Represents the settings configuration for the G4™ logger.
    /// </summary>
    public class G4LoggerSettings
    {
        #region *** Fields       ***
        // Retrieves the "Logging" section from the configuration sources.
        private static readonly IConfigurationSection s_settings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Logging");
        #endregion

        #region *** Constructors ***
        /// <summary>
        /// Initializes a new instance of the <see cref="G4LoggerSettings"/> class.
        /// Loads and applies logger settings from the configuration.
        /// </summary>
        public G4LoggerSettings()
        {
            // Retrieve the log levels from the configuration. If not found, retain the default levels.
            var logLevel = s_settings.GetSection("LogLevel").Get<Dictionary<string, LogLevel>>();
            var outputDirectory = s_settings.GetSection("G4Logger:OutputDirectory").Get<string>();

            // Set the log levels for different loggers. If not specified, use the default.
            LogLevel = logLevel ?? LogLevel;

            // Configure the logger to add console logging based on the configuration.
            G4Logger.AddConsole = s_settings.GetSection("G4Logger:AddConsole").Get<bool>();

            // Configure the logger to add debug logging based on the configuration.
            G4Logger.AddDebug = s_settings.GetSection("G4Logger:AddDebug").Get<bool>();

            // Set the output directory for log files. If not specified or set to ".", use the default.
            G4Logger.OutputDirectory = string.IsNullOrEmpty(outputDirectory) || outputDirectory == "."
                ? G4Logger.OutputDirectory
                : outputDirectory;
        }
        #endregion

        #region *** Properties   ***
        /// <summary>
        /// Gets or sets the event identifier for logging purposes.
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// Gets or sets the G4™ logger settings model, which includes console and debug logging options.
        /// </summary>
        public G4LoggerSettingsModel G4Logger { get; set; } = new();

        /// <summary>
        /// Gets or sets the log levels for different loggers.
        /// </summary>
        public Dictionary<string, LogLevel> LogLevel { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Default"] = Microsoft.Extensions.Logging.LogLevel.Information
        };
        #endregion

        #region *** Nested Types ***
        /// <summary>
        /// Represents the specific settings for the G4™ logger, including console and debug options.
        /// </summary>
        public class G4LoggerSettingsModel
        {
            /// <summary>
            /// Gets or sets a value indicating whether to add console logging.
            /// </summary>
            public bool AddConsole { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to add debug logging.
            /// </summary>
            public bool AddDebug { get; set; } = true;

            /// <summary>
            /// Gets or sets the output directory for log files.
            /// </summary>
            public string OutputDirectory { get; set; } = Environment.CurrentDirectory;

            /// <summary>
            /// Gets or sets the type of logging to be used. Supported types are "Text", "Json", and "Xml".
            /// </summary>
            public string Type { get; set; } = "Text";
        }
        #endregion
    }
}
