using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Remote.Uia;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "UiaDriver",
        Description =
            "`UiaDriver` WebDriver, which is an implementation specifically designed for Windows UI Automation. " +
            "The UiaDriver interacts directly with the UI elements of Windows applications, enabling automated testing " +
            "of native Windows applications.")]
    public class UiaDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Create and return a new local instance of the UiaDriver.
            return new UiaDriver(binariesPath, session);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Create and return a new remote instance of the UiaDriver.
            return new UiaDriver(new Uri(binariesPath), session, timeout);
        }
    }
}
