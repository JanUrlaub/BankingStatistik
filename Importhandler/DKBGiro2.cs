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
    public class DKBGiro2 : AbstractImport
    {
        public static CSVHeaders translateColumns2  = new()
        {
            { "Konto", TransferData.SourceIBAN },
            { "Buchungsdatum", TransferData.Buchungstag},
            { "Wertstellung", null },
            { "Status", null },
            { "Zahlungspflichtige*r", null},
            { "Zahlungsempfänger*in", TransferData.Empfänger },
            { "Verwendungszweck", TransferData.Verwendungszweck },
            { "Umsatztyp", null },
            { "Betrag", TransferData.Betrag },
            { "Gläubiger-ID", TransferData.TargetIBAN },
            { "Mandatsreferenz", null },
            { "Kundenreferenz", null },

        };

        public DKBGiro2(ImportCSV csv) : base(csv)
        {
            importTable = "banking.import_dkb_giro2";
            translateColumns = translateColumns2;
        }
        public override string convert(string value, string colum)
        {
            if (colum == TransferData.SourceIBAN)
            {
                return value.Replace("Girokonto ", "");
            }

            if(colum == TransferData.Verwendungszweck && value.StartsWith("Abrechnung "))
            {
                return value.Substring(0,255);
            }

            if(colum == TransferData.Betrag)
            {
                return value.Replace(" €", "");
            }

            return value;
        }

        public override DateTime convertDate(string value)
        {
            return DateTime.ParseExact(value, "dd.MM.yy", new CultureInfo("de-DE"));
        }

    }
}
