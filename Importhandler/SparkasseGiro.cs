using Import;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CSVHeaders = System.Collections.Generic.Dictionary<string, string>;

namespace BankingStatistik.ImportHandler
{
    public class SparkasseGiro : AbstractImport
    {
        public static CSVHeaders translateColumns2  = new()
            {
                { "Auftragskonto", TransferData.SourceIBAN},
                { "Buchungstag", TransferData.Buchungstag},
                { "Valutadatum", null },
                { "Buchungstext", TransferData.Buchungstext },
                { "Verwendungszweck", TransferData.Verwendungszweck },
                { "Glaeubiger ID", null },
                { "Mandatsreferenz", null },
                { "Kundenreferenz (End-to-End)", null },
                { "Sammlerreferenz", null },
                { "Lastschrift Ursprungsbetrag", null },
                { "Auslagenersatz Ruecklastschrift", null },
                { "Beguenstigter/Zahlungspflichtiger", TransferData.Empfänger },
                { "Kontonummer/IBAN", TransferData.TargetIBAN },
                { "BIC (SWIFT-Code)", null },
                { "Betrag", TransferData.Betrag },
                { "Waehrung", TransferData.Währung },
                { "Info", TransferData.Info },
            };

        internal static void getImportData(DirectoryInfo target, string spkUser, string spkPassword)
        {
            IWebDriver driver = Selenium.getWebdriver();
            try
            {
                string url = "https://www.sparkasse-holstein.de/de/home/onlinebanking/nbf/finanzuebersicht.html";
                driver.Navigate().GoToUrl(url);

                // Anmelden
                //driver.FindElement(By.ClassName("nav-login")).Click();
                string loginUserID = driver.FindElement(By.XPath("//*[text()='Anmeldename']")).GetAttribute("for");
                string loginPasswordID = driver.FindElement(By.XPath("//*[text()='Passwort/PIN']")).GetAttribute("for");
                driver.FindElement(By.Id(loginUserID)).SendKeys(spkUser);
                driver.FindElement(By.Id(loginPasswordID)).SendKeys(spkPassword);
                driver.FindElement(By.XPath("//*[@title='Anmelden']")).Click();

                // Störer Prüfung
                try
                {
                    driver.FindElement(By.ClassName("cbox-eyecatcher")).FindElement(By.ClassName("asdasd")).Click();
                }
                catch (NoSuchElementException) { }

                // Anmeldung abwarten
                WebDriverWait wait2 = new WebDriverWait(driver, new TimeSpan(0, 0, 40));
                var elements = wait2.Until(driver => driver.FindElements(By.ClassName("mkp-card-bank-account")));

                // Konto CSV Download Element 1
                var element = elements.ElementAt(0);
                element.Click();
                driver.FindElement(By.ClassName("nbf-druckExportLabel")).Click();
                driver.FindElement(By.XPath("//*[@title='Excel (CSV-CAMT)']")).Click();
                
                // Zurück zur Startseite
                driver.Navigate().Back();
                Thread.Sleep(2000);
                driver.FindElement(By.ClassName("nbf-guided-tour-navigation_finanzuebersicht")).Click();

                // Konto CSV Download Element 2
                WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 40));
                elements = wait.Until(driver => driver.FindElements(By.ClassName("mkp-card-bank-account")));
                element = elements.ElementAt(1);
                element.Click();
                driver.FindElement(By.ClassName("nbf-druckExportLabel")).Click();
                driver.FindElement(By.XPath("//*[@title='CSV-Export']")).Click();

                // Download Abspeichern
                string[] files = Directory.GetFiles(Path.GetTempPath(), "*-umsatz.CSV", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string targetFile = Path.Combine(target.FullName, fileName);
                    File.Move(file, targetFile);
                }
                string[] files2 = Directory.GetFiles(Path.GetTempPath(), "umsatz*.CSV", SearchOption.AllDirectories);
                foreach (string file in files2)
                {
                    string fileName = Path.GetFileName(file);
                    string targetFile = Path.Combine(target.FullName, fileName);
                    File.Move(file, targetFile);
                }


            }
            catch (Exception exception)
            {
                Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
                ss.SaveAsFile("screen.png", ScreenshotImageFormat.Png);
                Console.WriteLine(exception.Message);
                Console.ReadLine();
                /*var cookies = driver.Manage().Cookies.AllCookies;
                foreach (var cookie in cookies)
                {
                    Console.WriteLine(cookie.Name + " " + cookie.Value);
                    //driver.Manage().Cookies.AddCookie(cookie);
                }*/
            }
            finally
            {
                driver.Quit();
            }
        }

        public SparkasseGiro(ImportCSV csv) : base(csv)
        {
            importTable = "banking.import_sparkasse_giro";
            translateColumns = translateColumns2;
        }

        public override string convert(string value, string colum)
        {
            return value;
        }

        public override DateTime convertDate(string value)
        {
            return DateTime.ParseExact(value, "dd.MM.yy", new CultureInfo("de-DE"));
        }

    }
}
