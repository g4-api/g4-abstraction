using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Remote.Chrome;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "ChromeDriver",
        Description =
            "`chromedriver` is a separate executable program that acts as a bridge between `G4™ WebDriver Client` and the Chrome browser. " +
            "It is specifically designed to control and automate the Chrome browser's actions, such as opening pages, interacting with " +
            "elements, and executing JavaScript code.")]
    public class ChromeDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewLocalDriver<ChromeDriver, ChromeWebDriverService, ChromeOptions>(
                binariesPath.Trim(),
                session,
                timeout);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewRemoteDriver<ChromeOptions>(binariesPath.Trim(), session, timeout);
        }
    }
}
