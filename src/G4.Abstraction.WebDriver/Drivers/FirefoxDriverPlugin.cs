using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Remote.Firefox;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "FirefoxDriver",
        Description =
            "`geckodriver` is a separate executable program that acts as a bridge between `G4™ WebDriver Client` and the Firefox browser. " +
            "It is specifically designed to control and automate the Firefox browser's actions, such as opening pages, interacting with " +
            "elements, and executing JavaScript code.")]
    public class FirefoxDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewLocalDriver<FirefoxDriver, FirefoxWebDriverService, FirefoxOptions>(
                binariesPath.Trim(),
                session,
                timeout);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewRemoteDriver<FirefoxOptions>(binariesPath.Trim(), session, timeout);
        }
    }
}
