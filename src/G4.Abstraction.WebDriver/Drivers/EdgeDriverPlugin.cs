using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Remote.Edge;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "MicrosoftEdgeDriver",
        Description =
            "`msedgedriver` is a separate executable program that acts as a bridge between `G4™ WebDriver Client` and the Edge browser. " +
            "It is specifically designed to control and automate the Edge browser's actions, such as opening pages, interacting with " +
            "elements, and executing JavaScript code.")]
    public class EdgeDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewLocalDriver<EdgeDriver, EdgeWebDriverService, EdgeOptions>(binariesPath.Trim(), session, timeout);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewRemoteDriver<EdgeDriver, EdgeOptions>(binariesPath.Trim(), session, timeout);
        }
    }
}
