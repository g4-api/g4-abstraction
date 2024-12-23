using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Concurrent;

namespace G4.Abstraction.Logging
{
    /// <summary>
    /// Provides instances of the G4™ logger.
    /// </summary>
    public class G4LoggerProvider : ILoggerProvider
    {
        #region *** Fields       ***
        // The disposable token for monitoring changes to the logger configuration.
        private readonly IDisposable _onChangeToken;

        // The collection of loggers, keyed by name.
        private readonly ConcurrentDictionary<string, G4Logger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        // The current configuration settings for the G4™ logger.
        private G4LoggerSettings _currentConfiguration;
        #endregion

        #region *** Constructors ***
        /// <summary>
        /// Initializes a new instance of the <see cref="G4LoggerProvider"/> class.
        /// </summary>
        /// <param name="configuration">The options monitor for G4™ logger settings.</param>
        public G4LoggerProvider(IOptionsMonitor<G4LoggerSettings> configuration)
        {
            // Initialize the current configuration with the current value from the options monitor.
            _currentConfiguration = configuration.CurrentValue;

            // Register a callback to update the current configuration when it changes.
            _onChangeToken = configuration.OnChange(updatedConfig => _currentConfiguration = updatedConfig);
        }
        #endregion

        #region *** Methods      ***
        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, name => new G4Logger(name, GetCurrentConfiguration));

        /// <inheritdoc />
        public void Dispose()
        {
            // Dispose of managed resources.
            Dispose(true);

            // Suppress finalization to prevent the finalizer from being called.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the managed resources used by the logger provider.
        /// </summary>
        /// <param name="disposing">True to dispose of managed resources, false to dispose of unmanaged resources only.</param>
        protected virtual void Dispose(bool disposing)
        {
            // If not disposing, return without further action.
            if (!disposing)
            {
                return;
            }

            // Clear the collection of loggers.
            _loggers.Clear();

            // Dispose of the change token.
            _onChangeToken.Dispose();
        }

        // Retrieves the current configuration settings for the G4™ logger.
        private G4LoggerSettings GetCurrentConfiguration() => _currentConfiguration;
        #endregion
    }
}
