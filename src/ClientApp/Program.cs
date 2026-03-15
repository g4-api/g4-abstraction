using G4.Abstraction.Cli;
using G4.Abstraction.WebDriver;

using System.Collections.Generic;


var cli = "{{$ --message:Foo Bar}}";
var a = new CliFactory().ConvertToDictionary(cli,normalize: false);
a = new CliFactory().ConvertToDictionary(cli);


var json = "{\"capabilities\":{\"alwaysMatch\":{\"browserName\":\"chrome\",\"goog:chromeOptions\":{\"args\":[\"--headless=new\",\"--no-sandbox\",\"--disable-dev-shm-usage\",\"--window-size=1920,1080\"]}}},\"driver\":\"ChromeDriver\",\"driverBinaries\":\"http://localhost:4444/wd/hub\",\"firstMatch\":[{}]}";
var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
var d = new DriverFactory(parameters).NewDriver();




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
