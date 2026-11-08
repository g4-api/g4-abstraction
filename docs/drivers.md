# G4 drivers

This page lists every driver that `G4.Abstraction.WebDriver` can create. A driver is chosen by the `driver` value in `driverParameters`. The match is case-insensitive.

## How a driver is chosen

| Step | What happens | Source |
| --- | --- | --- |
| 1 | `DriverFactory` reads `driver` (default `ChromeDriver`), `driverBinaries` (default `.`), `commandTimeout` (default `60000` ms), `service`, `timeouts`, `capabilities` and `firstMatch`. | `src/G4.Abstraction.WebDriver/DriverFactory.cs:97-125` |
| 2 | If `driverBinaries` starts with `http://` or `https://`, the driver is created as a **remote** driver. Otherwise it is created as a **local** driver. | `DriverFactory.cs:137-140` |
| 3 | The factory finds the plugin class whose `[G4DriverPlugin(driver: ...)]` name matches. | `DriverFactory.cs:200` |
| 4 | If no plugin matches, it throws `NotImplementedException`. | `DriverFactory.cs:203-214` |

The error text is:

```text
Unable to find or implement the required driver plugin. Driver: <name>. Make sure the plugin is correctly registered and implemented.
```

## Driver names

| Driver name | Target | Package / server | Key parameters | Notes |
| --- | --- | --- | --- | --- |
| `ChromeDriver` | Google Chrome | `G4.WebDriver.Remote.Chrome`; local `chromedriver` or a grid | `driverBinaries`, `capabilities`, `commandTimeout` | Source: `Drivers/ChromeDriverPlugin.cs`. |
| `MicrosoftEdgeDriver` | Microsoft Edge | `G4.WebDriver.Remote.Edge`; local `msedgedriver` or a grid | `driverBinaries`, `capabilities` | The name is `MicrosoftEdgeDriver`, not `EdgeDriver`. Source: `Drivers/EdgeDriverPlugin.cs`. |
| `FirefoxDriver` | Mozilla Firefox | `G4.WebDriver.Remote.Firefox`; local `geckodriver` or a grid | `driverBinaries`, `capabilities` | Source: `Drivers/FirefoxDriverPlugin.cs`. |
| `AndroidDriver` | Android devices | `G4.WebDriver.Remote.Android`; Appium server | `driverBinaries`, `capabilities` | Source: `Drivers/AndroidDriverPlugin.cs`. |
| `UiaDriver` | Windows desktop apps (UI Automation) | `G4.WebDriver.Remote.Uia`; [UIA driver server](../../uia-driver-server/README.md) | `driverBinaries`, `capabilities` with `uia:options` | Local mode passes `driverBinaries` to the `UiaDriver` constructor. Remote mode needs a URL. Source: `Drivers/UiaDriverPlugin.cs`. |
| `SapDriver` | SAP GUI for Windows | `G4.WebDriver.Remote.Sap`; [SAP driver server](../../sap-driver-server/docs/capabilities.md) | `driverBinaries`, `capabilities` with `sap:options` | Local mode starts `Sap.DriverServer.exe` from the binaries path. Source: `Drivers/SapDriverPlugin.cs`. |
| `SimulatorDriver` | No real app. A simulated driver. | `G4.WebDriver.Simulator` | `driverBinaries`, `capabilities` | Local and remote both create a `SimulatorDriver`. Source: `Drivers/SimulatorDriverPlugin.cs`. |
| `NoDriver` | No real app. A simulated driver. | `G4.WebDriver.Simulator` | `driverBinaries`, `capabilities` | Same behaviour as `SimulatorDriver`, under another name. Source: `Drivers/NoDriverPlugin.cs`. |

There is no plugin for Opera in this repository, although `g4-web-driver-client` has an Opera package.

## driverBinaries: folder or URL

| Value | Mode | What happens |
| --- | --- | --- |
| A folder path, for example `C:\drivers` | Local | The plugin builds a driver service from the path (for Chrome, Edge, Firefox, Android, Sap) and starts the driver process. |
| `http://host:port/wd/hub` | Remote | A `WebDriverCommandInvoker` talks to the URL. Use this for Selenium Grid and for the UIA and SAP driver servers. |

For the plugins that use the shared helpers, the path is trimmed before use. Source: `src/G4.Abstraction.WebDriver/DriverPluginBase.cs:26-45, 95-125`.

## Capabilities and session merge

The plugin builds its own options and then merges your capabilities on top. `alwaysMatch` entries you send replace the plugin entries with the same key. Your `firstMatch` entries are appended. Empty `firstMatch` entries are dropped. Source: `DriverPluginBase.cs:177-200`.

## Timeouts

After the driver starts, the factory sets the implicit, page load and script timeouts from `timeouts`. Errors while setting them are ignored, because not every driver supports all three. Source: `DriverFactory.cs:160-171`.

## Example

```json
{
    "driverParameters": {
        "driver": "UiaDriver",
        "driverBinaries": "http://localhost:5555/wd/hub",
        "capabilities": {
            "alwaysMatch": {
                "browserName": "Uia",
                "uia:options": {}
            }
        }
    }
}
```

This example opens a desktop session. An empty `uia:options`, or no `app`, selects a desktop session. A non-empty `app` makes the server start a process with that name, so `"app": "Desktop"` would try to start a program called Desktop. Source: `uia-driver-server/src/Uia.DriverServer.Domain/SessionsRepository.cs:187-192, 404-410`.

The `uia:options` shape comes from the [UIA driver server README](../../uia-driver-server/README.md). The `sap:options` shape comes from the [SAP capabilities page](../../sap-driver-server/docs/capabilities.md).
