using G4.Abstraction.WebDriver;

var driverParameters = new Dictionary<string, object>
{
    ["driverBinaries"] = "http://localhost/wd/hub",
    ["driver"] = "UiaDriver",
    ["capabilities"] = new Dictionary<string, object>
    {
        ["alwaysMatch"] = new Dictionary<string, object>
        {
            ["browserName"] = "Uia",
            ["uia:options"] = new Dictionary<string, object>
            {
                ["app"] = "notepad.exe"
            }
        }
    },
    ["firstMatch"] = new List<Dictionary<string, object>>
    {
        new()
        {
            ["label"] = "UIA"
        }
    }
};

var driver = new DriverFactory(driverParameters).NewDriver();

driver.Dispose();

driverParameters = new Dictionary<string, object>
{
    ["driverBinaries"] = "http://localhost/wd/hub",
    ["driver"] = "MicrosoftEdgeDriver"
};

driver = new DriverFactory(driverParameters).NewDriver();
driver.Dispose();

driverParameters = new Dictionary<string, object>
{
    ["driverBinaries"] = "http://localhost/wd/hub",
    ["driver"] = "ChromeDriver"
};

driver = new DriverFactory(driverParameters).NewDriver();
driver.Dispose();

driverParameters = new Dictionary<string, object>
{
    ["driverBinaries"] = "http://localhost/wd/hub",
    ["driver"] = "Firefox"
};

driver = new DriverFactory(driverParameters).NewDriver();
driver.Dispose();
