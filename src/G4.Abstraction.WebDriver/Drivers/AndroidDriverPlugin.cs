using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Remote.Android;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "AndroidDriver",
        Description =
            "`AppiumDriver` is a separate executable program that acts as a bridge between `G4™ WebDriver Client` and the Android devices. " +
            "It is specifically designed to control and automate the Android device's actions, such as opening pages, interacting with " +
            "elements, and executing actions.")]
    public class AndroidDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewLocalDriver<AndroidDriver, AndroidWebDriverService, AndroidOptions>(
                binariesPath.Trim(),
                session,
                timeout);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewRemoteDriver<AndroidDriver, AndroidOptions>(binariesPath.Trim(), session, timeout);
        }
    }
}
