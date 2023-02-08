using Import;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using CSVHeaders = System.Collections.Generic.Dictionary<string, string>;

namespace BankingStatistik.ImportHandler
{
    public class DKBGiro : AbstractImport
    {
        public static CSVHeaders translateColumns2  = new()
        {
            { "Kontonummer:", TransferData.SourceIBAN },
            { "Von:", null },
            { "Bis:", null },
            { "Buchungstag", TransferData.Buchungstag},
            { "Wertstellung", null },
            { "Buchungstext", TransferData.Buchungstext },
            { "Auftraggeber / Begünstigter", TransferData.Empfänger },
            { "Verwendungszweck", TransferData.Verwendungszweck },
            { "Kontonummer", TransferData.TargetIBAN },
            { "BLZ", null },
            { "Betrag (EUR)", TransferData.Betrag },
            { "Gläubiger-ID", null },
            { "Mandatsreferenz", null },
            { "Kundenreferenz", null },
        };

        public DKBGiro(ImportCSV csv) : base(csv)
        {
            importTable = "banking.import_dkb_giro";
            translateColumns = translateColumns2;
        }
        public override string convert(string value, string colum)
        {
            if (colum == TransferData.SourceIBAN)
            {
                return value.Replace(" / Girokonto", "");
            }

            return value;
        }

        public override DateTime convertDate(string value)
        {
            return DateTime.ParseExact(value, "dd.MM.yyyy", new CultureInfo("de-DE"));
        }

        /**
         * Gibt den Path der neuesten Importdatei zurück
         * @todo Direkt Inhalt zurückgeben
         * 
         */
        public static string getImportData(DirectoryInfo target, string username, string password)
        {
            string targetFile = Path.Combine(target.FullName, "1065134361_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".csv");
            IWebDriver driver = Selenium.getWebdriver();
            try
            {
                driver.Navigate().GoToUrl("https://www.dkb.de/banking");

                // Cookie-Meldung
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("return document.getElementById('privacy-overlay').remove();");
                js.ExecuteScript("return document.getElementById('privacy-container').remove();");

                //Anmeldung
                driver.FindElement(By.Id("loginInputSelector")).SendKeys(username);
                driver.FindElement(By.Id("pinInputSelector")).SendKeys(password);
                driver.FindElement(By.Id("buttonlogin")).Click();

                // Anmeldung abwarten Umsätze aufrunfen
                IWebElement element = null;
                int count = 0;
                do
                {
                    try
                    {
                        js.ExecuteScript("return document.getElementById('privacy-overlay').remove();");
                        js.ExecuteScript("return document.getElementById('privacy-container').remove();");
                        element = driver.FindElement(By.ClassName("evt-paymentTransaction"));
                    }
                    catch (NoSuchElementException) { }
                    Thread.Sleep(2000);
                    count++;
                } while (element == null && count < 20);
                element.Click();

                js.ExecuteScript("return document.getElementById('privacy-overlay').remove();");
                js.ExecuteScript("return document.getElementById('privacy-container').remove();");

                // Export CSV
                driver.FindElement(By.ClassName("iconExport0")).Click();

                // logout
                driver.FindElement(By.Id("logout")).Click();

                File.Move(Path.Combine(Path.GetTempPath(), "1065134361.csv"), targetFile);

            }
            catch(Exception exception)
            {
                Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
                ss.SaveAsFile("screen.png", ScreenshotImageFormat.Png);
                Console.WriteLine(exception.Message);
            }
            finally
            {
                driver.Quit();
            }
            
           
            return targetFile;
        }

    }
}
