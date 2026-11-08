# How to write a custom driver plugin

A driver plugin tells `DriverFactory` how to create one kind of driver. G4 ships plugins for the browsers, Android, the UIA and SAP servers, and a simulator. This page shows how to add your own. The built-in list is in the [drivers table](drivers.md).

## Goal

Make `"driver": "MyDriver"` in `driverParameters` create your own `IWebDriver`.

## Before you start

- A class library or application that targets `net10.0`.
- A reference to the `G4.Abstraction.WebDriver` package.
- A driver class that implements `IWebDriver` from `G4.WebDriver.Remote`, or a built-in driver you want to wrap.

## Steps

### 1. Derive from `DriverPluginBase`

`DriverPluginBase` has two protected methods you can override. Override the one for each mode you support.

| Method | Called when | If you do not override it |
| --- | --- | --- |
| `OnNewLocalDriver(binariesPath, session, timeout)` | `driverBinaries` is not a URL | Throws `NotImplementedException`: `The method OnNewLocalDriver is not implemented.` |
| `OnNewRemoteDriver(binariesPath, session, timeout)` | `driverBinaries` starts with `http://` or `https://` | Throws `NotImplementedException`: `The method OnNewRemoteDriver is not implemented.` |

Source: `src/G4.Abstraction.WebDriver/DriverPluginBase.cs:79-83, 166-170`.

### 2. Add the `[G4DriverPlugin]` attribute

The attribute carries the driver name and a description. The name is what users put in `driver`. The match ignores case.

```csharp
using G4.Abstraction.WebDriver;
using G4.Attributes;
using G4.WebDriver.Models;
using G4.WebDriver.Remote;
using G4.WebDriver.Simulator;

namespace Demo
{
    [G4DriverPlugin(driver: "MyDriver", Description = "Demo driver that wraps the simulator.")]
    public class MyDriverPlugin : DriverPluginBase
    {
        protected override IWebDriver OnNewLocalDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return new SimulatorDriver(binariesPath, session);
        }

        protected override IWebDriver OnNewRemoteDriver(string binariesPath, SessionModel session, TimeSpan timeout)
        {
            return NewRemoteDriver<G4.WebDriver.Remote.Chrome.ChromeOptions>(binariesPath.Trim(), session, timeout);
        }
    }
}
```

The attribute is sealed, applies to classes only, and allows one per class. Source: `g4-engine/src/G4.Attributes/G4DriverPluginAttribute.cs`.

### 3. Use the helpers for common cases

`DriverPluginBase` has static helpers that the built-in plugins use. They build the options for you and merge the caller's capabilities on top.

| Helper | Creates | Used by |
| --- | --- | --- |
| `NewLocalDriver<TDriver, TService, TOption>` | A driver that starts a local driver service from the binaries path. | `ChromeDriverPlugin`, `SapDriverPlugin` |
| `NewRemoteDriver<TOption>` | A `RemoteWebDriver` that talks to the URL. | `ChromeDriverPlugin` |
| `NewRemoteDriver<TDriver, TOption>` | Your driver type, created from a command invoker and the merged session. `TDriver` needs a constructor with those two arguments. | `SapDriverPlugin` |

Source: `DriverPluginBase.cs:26-57, 95-114, 120-137`; `Drivers/ChromeDriverPlugin.cs`; `Drivers/SapDriverPlugin.cs`.

The merge rule: the caller's `alwaysMatch` entries replace the plugin's entries with the same key. The caller's `firstMatch` entries are added at the end, and empty entries are dropped. Source: `DriverPluginBase.cs:174-200`.

### 4. Make the plugin discoverable

`DriverFactory` takes every type that has the attribute **and** derives from `DriverPluginBase`. It gets the types from the engine's type cache, which loads the `.dll` files in the engine's folder and the folders below it. Source: `Extensions/LocalExtensions.cs:34-43`, `DriverFactory.cs:32-35`, `g4-engine/src/G4.Cache/AssembliesLoader.cs:87-125`.

- In your own application, put the plugin in the same project or copy its `.dll` beside the application.
- In a G4 Hub or engine host, copy the `.dll` into the engine folder or a folder below it, then restart the host.

## Verify

Create the driver from JSON:

```csharp
using G4.Abstraction.WebDriver;

var driver = new DriverFactory("""{"driver":"MyDriver","driverBinaries":"."}""").NewDriver();

Console.WriteLine(driver.GetType().FullName);
```

With the plugin above the output is `G4.WebDriver.Simulator.SimulatorDriver`.

## If it fails

| Symptom | Cause | Fix |
| --- | --- | --- |
| `NotImplementedException: Unable to find or implement the required driver plugin. Driver: MyDriver.` | The class is missing the attribute, does not derive from `DriverPluginBase`, or its assembly is not in a folder the loader scans. | Check the attribute and base class. Copy the `.dll` beside the host. Source: `DriverFactory.cs:203-214`. |
| `NotImplementedException: The method OnNewRemoteDriver is not implemented.` | `driverBinaries` is a URL, but the plugin only overrides the local method. | Override both methods, or use a folder path. |
| A different exception from deep inside the driver | The factory throws the base exception of the failure. | Read the message and the inner types of your driver class. Source: `DriverPluginBase.cs:59-70`. |

## Related

- [Drivers table](drivers.md)
- [G4.Abstraction.WebDriver package page](packages-readme/G4.Abstraction.WebDriver.md)
