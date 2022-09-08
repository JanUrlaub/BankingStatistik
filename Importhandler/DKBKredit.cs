using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVHeaders = System.Collections.Generic.Dictionary<string, string>;

namespace BankingStatistik.ImportHandler
{
    public class DKBKredit : AbstractImport
    {
        public static CSVHeaders translateColumns2  = new()
        {
            { "Kreditkarte:", TransferData.SourceIBAN },
            { "Von:", null },
            { "Bis:", null },
            { "Saldo:", null },
            { "Datum:", null },
            { "Umsatz abgerechnet und nicht im Saldo enthalten", null },
            { "Wertstellung", null },
            { "Belegdatum", TransferData.Buchungstag},
            { "Beschreibung", TransferData.Empfänger },
            { "Betrag (EUR)", TransferData.Betrag },
            { "Ursprünglicher Betrag", null }
        };

        public DKBKredit(ImportCSV csv) : base(csv)
        {
            importTable = "banking.import_dkb_kredit";
            translateColumns = translateColumns2;
        }
        public override string convert(string value, string colum)
        {
            return value;
        }

        public override DateTime convertDate(string value)
        {
            return DateTime.ParseExact(value, "dd.MM.yyyy", new CultureInfo("de-DE"));
        }

    }
}
