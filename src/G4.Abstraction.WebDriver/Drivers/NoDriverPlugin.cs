using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Simulator;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "NoDriver",
        Description =
            "`NoDriver` WebDriver, which is a simulated or virtual implementation of the WebDriver interface. " +
            "The NoDriver behaves like a real WebDriver but does not open a physical web browser. It is primarily used for testing " +
            "purposes when you need to simulate browser interactions without the overhead of launching a real browser instance.")]
    public class NoDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Create and return a new local instance of the NoDriver.
            return new SimulatorDriver(binariesPath, session);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Create and return a new remote instance of the NoDriver.
            return new SimulatorDriver(binariesPath, session);
        }
    }
}
