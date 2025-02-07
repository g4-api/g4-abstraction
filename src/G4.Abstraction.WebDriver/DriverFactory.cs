/*
 * WORK ITEMS
 * TODO: Integrate support for service parameters during the initialization of new driver instances.
 */
using G4.Attributes;
using G4.Cache;
using G4.Extensions;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace G4.Abstraction.WebDriver
{
    /// <summary>
    /// Represents an abstraction for creating WebDriver instances.
    /// This class provides a high-level abstraction for creating WebDriver instances
    /// with various configuration parameters, including command timeout, driver type,
    /// driver binaries path, service configuration, and capabilities.
    /// </summary>
    public class DriverFactory
    {
        #region *** Fields       ***
        // Provides access to a collection of driver plugins.
        private static ReadOnlyCollection<Type> s_driverPlugins = new(CacheManager
            .Types
            .Values
            .Where(i => i.ConfirmDriverPlugin())
            .ToList());

        // Represents the default JSON serializer options used for serialization and deserialization.
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            // Use camel case naming policy for dictionary keys
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,

            // Use camel case naming policy for properties
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            // Enable case-insensitive property name handling
            PropertyNameCaseInsensitive = true
        };

        private readonly SessionModel _sessionModel;           // The session model associated with the instance.
        private readonly IDictionary<string, object> _service; // The service associated with the instance.
        private readonly int _commandTimeout;                  // The command timeout value associated with the instance.
        private readonly string _driver;                       // The driver associated with the instance.
        private readonly string _driverBinaries;               // The path to the driver binaries associated with the instance.
        #endregion

        #region *** Constructors ***
        /// <summary>
        /// Initializes a new instance of the <see cref="DriverFactory"/> class with the specified driver parameters
        /// in JSON format.
        /// </summary>
        /// <param name="driverParams">A JSON string containing driver parameters.</param>
        public DriverFactory(string driverParams)
            : this(JsonSerializer.Deserialize<IDictionary<string, object>>(driverParams, s_jsonOptions))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverFactory"/> class with the specified driver parameters.
        /// </summary>
        /// <param name="driverParams">The dictionary containing driver parameters.</param>
        public DriverFactory(IDictionary<string, object> driverParams)
        {
            // Helper method to create a case-insensitive dictionary
            static Dictionary<string, object> NewDictionary() => new(StringComparer.OrdinalIgnoreCase);

            // Set the command timeout, default to 60 seconds if not provided
            _commandTimeout = driverParams.Find("commandTimeout", 60000);

            // Set the driver type, default to "EdgeDriver" if not provided
            _driver = driverParams.Find(path: "driver", defaultValue: "MicrosoftEdgeDriver");

            // Set the path for driver binaries, default to the current directory if not provided
            _driverBinaries = driverParams.Find("driverBinaries", ".");

            // Set the service configuration, default to an empty dictionary if not provided
            _service = driverParams.Find("service", NewDictionary());

            // Set capabilities using provided values or default to empty models
            var capabilities = driverParams.Find("capabilities", new CapabilitiesModel());
            var desiredCapabilities = driverParams.Find("firstMatch", Enumerable.Empty<IDictionary<string, object>>());

            // Create and assign a new session model
            _sessionModel = new SessionModel
            {
                Capabilities = capabilities,
                FirstMatch = desiredCapabilities,
            };
        }
        #endregion

        #region *** Properties   ***
        // Gets the collection of driver plugins.
        // If the collection is empty, it populates the collection first.
        private static ReadOnlyCollection<Type> DriverPlugins
        {
            get
            {
                // If the collection is not populated, fetch and populate it
                if (s_driverPlugins.Count != 0)
                {
                    var types = CacheManager.Types.Values.Where(i => i.ConfirmDriverPlugin()).ToList();
                    s_driverPlugins = new(types);
                }

                // Return the populated collection
                return s_driverPlugins;
            }
        }
        #endregion

        #region *** Methods      ***
        /// <summary>
        /// Creates a new instance of the WebDriver based on the specified parameters.
        /// This method determines whether the driver binaries represent a remote URL,
        /// retrieves the appropriate driver type and method, instantiates the driver if needed,
        /// and invokes the method to create a new WebDriver instance.
        /// </summary>
        /// <returns>The newly created WebDriver instance.</returns>
        public IWebDriver NewDriver()
        {
            // Check if the driver binaries represent a remote URL
            var isRemote = Regex.IsMatch(
                input: _driverBinaries,
                pattern: "^(http(s)?)://",
                RegexOptions.IgnoreCase);

            // Get the driver type and method based on the driver and remote status
            var (type, method) = GetDriverMethod(_driver, isRemote);

            // Instantiate the driver instance if the method is not static
            var instance = method.IsStatic ? null : Activator.CreateInstance(type);

            // Prepare the arguments for the driver creation method
            var arguments = new object[]
            {
                _driverBinaries,
                _sessionModel,
                TimeSpan.FromMilliseconds(_commandTimeout)
            };

            // Invoke the method to create and return a new WebDriver instance
            return (IWebDriver)method.Invoke(instance, arguments);
        }

        // Gets the driver type and method based on the specified driver name and remote status.
        private static (Type Type, MethodInfo Method) GetDriverMethod(string driver, bool isRemote)
        {
            // Use case-insensitive string comparison for consistency
            const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

            // Helper method to assert whether a given driver plugin matches the specified driver name
            static bool AssertDriver(Type driverPlugin, string driver)
            {
                // Retrieve the custom attribute associated with the driver plugin
                var attribute = driverPlugin.GetCustomAttribute<G4DriverPluginAttribute>();

                // Return true if the driver attribute exists and its name matches the specified driver (case-insensitive)
                return attribute?.Driver.Equals(driver, Comparison) ?? false;
            }

            // Define the method name based on the remote status
            var methodName = isRemote
                ? nameof(DriverPluginBase.NewRemoteDriver)
                : nameof(DriverPluginBase.NewLocalDriver);

            // Find the driver plugin type based on the specified driver name
            var type = DriverPlugins.FirstOrDefault(i => AssertDriver(i, driver));

            // Check if the 'type' variable is the default value for its type
            if (type == default)
            {
                // If the type is the default value, it indicates that the driver plugin was not found or not implemented
                var errorMessage = "Unable to find or implement the required driver plugin. " +
                    $"Driver: {driver}. " +
                    "Make sure the plugin is correctly registered and implemented.";

                // Log the detailed error message using TraceError for diagnostic purposes
                Trace.TraceError(errorMessage);

                // Throw a NotImplementedException with the enhanced error message, indicating that the driver plugin is not implemented
                throw new NotImplementedException(errorMessage);
            }

            // Find the method within the driver plugin type
            var method = Array.Find(type.GetMethods(), i => i.Name.Equals(methodName, Comparison));

            // Check if the 'method' variable is null
            if (method == null)
            {
                // If the method is null, it indicates that the driver initialize method was not found
                var errorMessage = "Unable to find the initialize method for the driver. " +
                    $"Driver Type: {driver}. " +
                    "Ensure that the initialize method is correctly implemented and marked as public.";

                // Log the detailed error message using TraceError for diagnostic purposes
                Trace.TraceError(errorMessage);

                // Throw a MissingMethodException with the detailed error message, indicating that the initialize method is missing
                throw new MissingMethodException(errorMessage);
            }

            // Return the tuple containing the driver type and method
            return (type, method);
        }
        #endregion
    }
}
