using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSVHeaders = System.Collections.Generic.Dictionary<string, string>;

namespace BankingStatistik.ImportHandler
{
    public class SparkasseKredit : AbstractImport
    {
        public static CSVHeaders translateColumns2 = new()
        {
            { "Umsatz getätigt von", TransferData.SourceIBAN },
            { "Belegdatum", null },
            { "Buchungsdatum", TransferData.Buchungstag },
            { "Originalbetrag", null },
            { "Originalwährung", null },
            { "Umrechnungskurs", null },
            { "Buchungsbetrag", TransferData.Betrag },
            { "Buchungswährung", TransferData.Währung },
            { "Transaktionsbeschreibung", TransferData.Empfänger },
            { "Transaktionsbeschreibung Zusatz", null },
            { "Buchungsreferenz", TransferData.TargetIBAN },
            { "Gebührenschlüssel", null },
            { "Länderkennzeichen", null },
            { "BAR-Entgelt+Buchungsreferenz", TransferData.Verwendungszweck },
            { "AEE+Buchungsreferenz", TransferData.Buchungstext },
            { "Abrechnungskennzeichen", TransferData.Info },
        };

        public SparkasseKredit(ImportCSV csv) : base(csv)
        {
            importTable = "banking.import_sparkasse_kredit";
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
