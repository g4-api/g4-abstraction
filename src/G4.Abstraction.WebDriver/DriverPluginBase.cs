using G4.WebDriver.Extensions;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;

using System;
using System.Linq;

namespace G4.Abstraction.WebDriver
{
    /// <summary>
    /// Base class for driver plugins.
    /// </summary>
    public abstract class DriverPluginBase
    {
        #region *** Methods: Local Driver  ***
        /// <summary>
        /// Creates a new local WebDriver instance with specified driver type, service type, and options.
        /// </summary>
        /// <typeparam name="TDriver">The type of WebDriver driver.</typeparam>
        /// <typeparam name="TService">The type of WebDriver service.</typeparam>
        /// <typeparam name="TOption">The type of WebDriver options.</typeparam>
        /// <param name="binariesPath">The URI to the WebDriver binaries.</param>
        /// <param name="session">The session model.</param>
        /// <param name="timeout">The command timeout duration.</param>
        /// <returns>A new instance of IWebDriver.</returns>
        public static IWebDriver NewLocalDriver<TDriver, TService, TOption>(string binariesPath, SessionModel session, TimeSpan timeout)
            where TDriver : IWebDriver
            where TService : WebDriverService
            where TOption : WebDriverOptionsBase, new()
        {
            // Instantiate the specified WebDriver options type.
            var options = new TOption();

            // Build the user session using specified options and session model.
            var userSession = NewSessionModel(options, session).Build();

            // Instantiate a new WebDriver service.
            var service = Activator.CreateInstance(typeof(TService), binariesPath);

            // Get the driver type.
            var driverType = typeof(TDriver);

            // Create arguments for driver instantiation.
            var arguments = new object[] { service, userSession, timeout };

            // Create an instance of the specified driver type with the service, user session, and timeout.
            return Activator.CreateInstance(driverType, arguments) as IWebDriver;
        }

        /// <summary>
        /// Creates a new instance of the local driver.
        /// </summary>
        /// <param name="binariesPath">The path to the driver binaries.</param>
        /// <param name="session">The session model containing configuration information.</param>
        /// <param name="timeout">The maximum time to wait for the driver to start.</param>
        /// <returns>The newly created <see cref="IWebDriver"/> instance.</returns>
        public IWebDriver NewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            try
            {
                // Call the overridden method to create a new local driver instance.
                return OnNewLocalDriver(binariesPath, session, timeout);
            }
            catch (Exception e)
            {
                // If an exception occurs, throw the base exception for better error handling.
                throw e.GetBaseException();
            }
        }

        /// <summary>
        /// Creates a new instance of the local driver.
        /// Override this method in derived classes to implement driver instantiation logic.
        /// </summary>
        /// <param name="binariesPath">The path to the driver binaries.</param>
        /// <param name="session">The session model containing configuration information.</param>
        /// <param name="timeout">The maximum time to wait for the driver to start.</param>
        /// <returns>The newly created <see cref="IWebDriver"/> instance.</returns>
        protected virtual IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Throw a NotImplementedException with an appropriate error message.
            throw new NotImplementedException("The method OnNewLocalDriver is not implemented.");
        }
        #endregion

        #region *** Methods: Remote Driver ***
        /// <summary>
        /// Creates a new remote WebDriver instance.
        /// </summary>
        /// <typeparam name="TOption">The type of WebDriver options.</typeparam>
        /// <param name="binariesPath">The URI to the WebDriver binaries.</param>
        /// <param name="session">The session model.</param>
        /// <param name="timeout">The command timeout duration.</param>
        /// <returns>A new instance of IWebDriver.</returns>
        public static IWebDriver NewRemoteDriver<TOption>(string binariesPath, SessionModel session, TimeSpan timeout)
            where TOption : WebDriverOptionsBase, new()
        {
            // Instantiate the specified WebDriver options type.
            var options = new TOption();

            // Build the user session using specified options and session model.
            var userSession = NewSessionModel(options, session).Build();

            // Instantiate a new WebDriverCommandInvoker.
            var invoker = new WebDriverCommandInvoker(new Uri(binariesPath), timeout);

            // Return a new RemoteWebDriver instance with the created invoker and user session.
            return new RemoteWebDriver(invoker, session: userSession);
        }

        /// <summary>
        /// Creates a new remote WebDriver instance with specified driver type.
        /// </summary>
        /// <typeparam name="TDriver">The type of WebDriver driver.</typeparam>
        /// <typeparam name="TOption">The type of WebDriver options.</typeparam>
        /// <param name="binariesPath">The URI to the WebDriver binaries.</param>
        /// <param name="session">The session model.</param>
        /// <param name="timeout">The command timeout duration.</param>
        /// <returns>A new instance of IWebDriver.</returns>
        public static IWebDriver NewRemoteDriver<TDriver, TOption>(string binariesPath, SessionModel session, TimeSpan timeout)
            where TOption : WebDriverOptionsBase, new()
        {
            // Instantiate the specified WebDriver options type.
            var options = new TOption();

            // Build the user session using specified options and session model.
            var userSession = NewSessionModel(options, session).Build();

            // Instantiate a new WebDriverCommandInvoker.
            var invoker = new WebDriverCommandInvoker(new Uri(binariesPath), timeout);

            // Create an instance of the specified driver type with the invoker and user session.
            return Activator.CreateInstance(typeof(TDriver), invoker, userSession) as IWebDriver;
        }

        /// <summary>
        /// Creates a new instance of the remote driver.
        /// Override this method in derived classes to implement driver instantiation logic.
        /// </summary>
        /// <param name="binariesPath">The path to the driver binaries.</param>
        /// <param name="session">The session model containing configuration information.</param>
        /// <param name="timeout">The maximum time to wait for the driver to start.</param>
        /// <returns>The newly created <see cref="IWebDriver"/> instance.</returns>
        public virtual IWebDriver NewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            try
            {
                // Call the overridden method to create a new remote driver instance.
                return OnNewRemoteDriver(binariesPath, session, timeout);
            }
            catch (Exception e)
            {
                // If an exception occurs, throw its base exception to simplify error handling.
                throw e.GetBaseException();
            }
        }

        /// <summary>
        /// Creates a new instance of the remote driver.
        /// Override this method in derived classes to implement the specific logic for creating the remote driver instance.
        /// </summary>
        /// <param name="binariesPath">The path to the driver binaries.</param>
        /// <param name="session">The session model containing configuration information.</param>
        /// <param name="timeout">The maximum time to wait for the driver to start.</param>
        /// <returns>The newly created <see cref="IWebDriver"/> instance.</returns>
        protected virtual IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Throw a NotImplementedException with an appropriate error message.
            throw new NotImplementedException("The method OnNewRemoteDriver is not implemented.");
        }
        #endregion

        // Creates a new session model based on the provided options and session.
        private static SessionModel NewSessionModel(WebDriverOptionsBase options, SessionModel session)
        {
            // Convert WebDriver options to capabilities and create a new session model.
            var userSession = options.ConvertToCapabilities().NewSessionModel();

            // Transfer capabilities from the session to the user session.
            foreach (var item in session.Capabilities.AlwaysMatch)
            {
                userSession.Capabilities.AlwaysMatch[item.Key] = item.Value;
            }

            // Transfer desired capabilities from the session to the user session.
            foreach (var item in session.DesiredCapabilities)
            {
                userSession.DesiredCapabilities[item.Key] = item.Value;
            }

            // Initialize the FirstMatch of the session.
            session.Capabilities.FirstMatch ??= [];

            // Initialize the FirstMatch capabilities in the user session.
            userSession.Capabilities.FirstMatch ??= [];

            // Merge the FirstMatch capabilities from the session into the user session.
            userSession.Capabilities.FirstMatch = userSession
                .Capabilities
                .FirstMatch
                .Concat(session.Capabilities.FirstMatch)
                .Where(i => i.Keys.Count > 0);

            // Return the created user session.
            return userSession;
        }
    }
}
