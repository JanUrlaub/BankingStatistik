using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import
{
    internal class Selenium
    {

        internal static FirefoxDriver getWebdriver()
        {
            
            FirefoxOptions options = new();
            options.SetPreference("javascript.enabled", true);
            options.SetPreference("browser.download.dir", Path.GetTempPath());
            options.SetPreference("browser.download.folderList", 2);
            options.SetPreference("browser.download.manager.showWhenStarting", false);
            options.SetPreference("browser.helperApps.neverAsk.saveToDisk", "text/csv,text/comma-separated-values;charset=ISO-8859-1");
            options.AddArguments("--headless");
            options.SetLoggingPreference(LogType.Browser, LogLevel.Severe);
            FirefoxDriver driver = new FirefoxDriver(options);

            return driver;
        }
    }
}
