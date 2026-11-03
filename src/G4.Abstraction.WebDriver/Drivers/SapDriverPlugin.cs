using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Remote.Sap;

using System;

namespace G4.Abstraction.WebDriver.Drivers
{
    [G4DriverPlugin(
        driver: "SapDriver",
        Description =
            "`SapDriver` WebDriver, which is an implementation specifically designed for SAP GUI for Windows. " +
            "The SapDriver drives SAP GUI through SAP GUI Scripting by attaching to a running SAP GUI session, " +
            "selected with the `sap:options` capability, enabling automated testing of SAP transactions.")]
    public class SapDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Start Sap.DriverServer.exe from the binaries path and return a new local instance of the SapDriver.
            return NewLocalDriver<SapDriver, SapWebDriverService, SapOptions>(
                binariesPath.Trim(),
                session,
                timeout);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            // Create and return a new remote instance of the SapDriver, sending the "Sap" browser name for grid routing.
            return NewRemoteDriver<SapDriver, SapOptions>(binariesPath.Trim(), session, timeout);
        }
    }
}
