using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace G4.Abstraction.Logging
{
    /// <summary>
    /// Extension methods for configuring G4™ logger with Microsoft.Extensions.Logging.
    /// </summary>
    public static class G4LoggerExtensions
    {
        /// <summary>
        /// Adds G4™ logger to the logging pipeline.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <returns>The logging builder with G4™ logger added.</returns>
        public static ILoggingBuilder AddG4Logger(this ILoggingBuilder builder)
        {
            return AddLogger(builder);
        }

        /// <summary>
        /// Adds G4™ logger to the logging pipeline with configuration.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="configure">The action to configure G4™ logger settings.</param>
        /// <returns>The logging builder with G4™ logger added and configured.</returns>
        public static ILoggingBuilder AddG4Logger(this ILoggingBuilder builder, Action<G4LoggerSettings> configure)
        {
            // Add G4™ logger to the logging pipeline.
            AddLogger(builder);

            // Configure G4™ logger settings.
            builder.Services.Configure(configure);

            // Return the logging builder.
            return builder;
        }

        // Adds G4™ logger provider to the logging builder.
        private static ILoggingBuilder AddLogger(ILoggingBuilder builder)
        {
            // Add logging configuration.
            builder.AddConfiguration();

            // Add G4™ logger provider to the services.
            builder
                .Services
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, G4LoggerProvider>());

            // Register G4™ logger provider options.
            LoggerProviderOptions.RegisterProviderOptions<G4LoggerSettings, G4LoggerProvider>(builder.Services);

            // Return the logging builder.
            return builder;
        }

        /// <summary>
        /// Finds a logger of type <typeparamref name="T"/> within the provided <paramref name="logger"/>.
        /// </summary>
        /// <typeparam name="T">The type of logger to find.</typeparam>
        /// <param name="logger">The logger instance.</param>
        /// <returns>The logger of type <typeparamref name="T"/> if found; otherwise, the default value of <typeparamref name="T"/>.</returns>
        public static T FindLogger<T>(this ILogger logger)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

            // Get the 'Loggers' property of the provided logger
            var loggersProperty = (IEnumerable)logger.GetType()?.GetProperty(name: "Loggers", bindingAttr: Flags)?.GetValue(logger);

            // Cast the loggers property to IEnumerable<object>
            var loggers = loggersProperty?.Cast<object>();

            // Return default value if no loggers found
            if (loggers?.Any() != true)
            {
                return default;
            }

            // Find the logger of type T
            var underlineLogger = loggers
                .Select(i => i.GetType().GetProperty("Logger", Flags)?.GetValue(obj: i))
                .FirstOrDefault(i => i.GetType() == typeof(T));

            // Return the found logger, or default value if not found
            return underlineLogger == default ? default : (T)underlineLogger;
        }
    }
}
