using BankingStatistik.ImportHandler;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CSVHeaders = System.Collections.Generic.Dictionary<string, string>;

namespace BankingStatistik
{
    class TransferData
    {
        private readonly AbstractImport Csv;
        private static readonly MySqlConnection Connection;
        private static readonly MySqlConnection Connection2;
        private static readonly MySqlConnection Connection3;

        public TransferData(AbstractImport csv)
        {
            Csv = csv;
        }

        static TransferData()
        {
            string server = ConfigurationManager.AppSettings["DBserver"];
            string user = ConfigurationManager.AppSettings["DBuser"];
            string password = ConfigurationManager.AppSettings["DBpassword"];
            string database = ConfigurationManager.AppSettings["DBdatabase"];

            Connection = new MySqlConnection("server=" + server + ";uid=" + user + ";pwd=" + password + ";database=" + database);
            Connection2 = new MySqlConnection("server=" + server + ";uid=" + user + ";pwd=" + password + ";database=" + database);
            Connection3 = new MySqlConnection("server=" + server + ";uid=" + user + ";pwd=" + password + ";database=" + database);
        }

        public const string SourceIBAN = "SourceIBAN";
        public const string TargetIBAN = "TargetIBAN";
        public const string Buchungstag = "Buchungstag";
        public const string Buchungstext = "Buchungstext";
        public const string Verwendungszweck = "Verwendungszweck";
        public const string Empfänger = "Empfeanger";
        public const string Betrag = "Betrag";
        public const string Währung = "Waehrung";
        public const string Info = "Info";
        private const string KategorieHaupt = "KategorieHaupt";
        private const string KategorieSub = "KategorieSub";
        private const string KategorieID = "KategorieID";
        private const string ID = "id";

        public static DataTable FilterRules { get; private set; }

        internal void ImportRawData(AbstractImport import)
        {
            string table = import.importTable;
            CSVHeaders translateColumns = import.translateColumns;

            Connection.Open();

            MySqlCommand truncate = new("TRUNCATE TABLE " + table, Connection);
            truncate.ExecuteNonQuery();

            CSVHeaders hashValues = new();
            foreach (KeyValuePair<string, string> column in translateColumns)
            {
                hashValues[column.Key] = BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(column.Key))).Replace("-","");
            }

            string sql = "INSERT INTO " + table + "(`";
            sql += string.Join("`, `", translateColumns.Keys);
            sql += "`) VALUES (@";
            sql += string.Join(", @", hashValues.Values);
            sql += ");";

            foreach (CSVHeaders row in Csv.rawData)
            {
                MySqlCommand command = new(sql, Connection);

                foreach(KeyValuePair<string,string> column in translateColumns)
                {
                    command.Parameters.AddWithValue("@"+ hashValues[column.Key], row[column.Key]);
                }

                command.Prepare();
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception exception)
                {
                    Exception e2 = new Exception(exception.Message + " in file " + import.FileInfo.FullName);
                    throw e2;
                }
            }

            Connection.Close();
        }

        internal static void ConvertData(AbstractImport import)
        {
            Connection.Open();
            CultureInfo cultureInfo = new("de-DE");
            string table = import.importTable;
            CSVHeaders keys = new();
            foreach(KeyValuePair<string,string> row in import.translateColumns)
            {
                if(row.Value!=null)
                {
                    keys.Add(row.Value, row.Key);
                }
            }

            MySqlCommand dataCommand = new("SELECT * FROM " + table, Connection);
            MySqlDataAdapter da = new(dataCommand);
            DataTable result = new();
            da.Fill(result);

            string sql = "INSERT INTO banking.umsaetze (" + SourceIBAN + "," + TargetIBAN + "," + Buchungstag + "," + Buchungstext + "," + Verwendungszweck + "," + Empfänger + "," + Betrag + "," + Währung + "," + Info + ","+ KategorieHaupt+ "," + KategorieSub+") " +
                "VALUES (@" + SourceIBAN + ", @" + TargetIBAN + ", @" + Buchungstag + ", @" + Buchungstext + ", @" + Verwendungszweck + ", @" + Empfänger + ", @" + Betrag + ", @" + Währung + ", @" + Info + ", @" + KategorieHaupt + ", @" + KategorieSub + ");";

            foreach (DataRow row in result.Rows)
            {
                string sourceIBAN = import.convert(row[keys[SourceIBAN]].ToString(), SourceIBAN);
                DateTime buchungstag = import.convertDate(row[keys[Buchungstag]].ToString());
                decimal betrag = decimal.Parse(row[keys[Betrag]].ToString(), cultureInfo.NumberFormat);

                Dictionary<string, object> dbRow = GetRow(sourceIBAN, buchungstag, betrag);

                if (dbRow != null)
                {
                    continue;
                }

                using MySqlCommand command = new(sql, Connection);
                command.Parameters.AddWithValue("@" + SourceIBAN, sourceIBAN);
                command.Parameters.AddWithValue("@" + TargetIBAN, keys.ContainsKey(TargetIBAN) ? row[keys[TargetIBAN]].ToString() : null);
                command.Parameters.AddWithValue("@" + Buchungstag, buchungstag);
                command.Parameters.AddWithValue("@" + Buchungstext, keys.ContainsKey(Buchungstext) ? row[keys[Buchungstext]].ToString() : null);
                command.Parameters.AddWithValue("@" + Verwendungszweck, keys.ContainsKey(Verwendungszweck) ? row[keys[Verwendungszweck]].ToString() : null);
                command.Parameters.AddWithValue("@" + Empfänger, row[keys[Empfänger]].ToString());
                command.Parameters.AddWithValue("@" + Betrag, betrag);
                command.Parameters.AddWithValue("@" + Währung, keys.ContainsKey(Währung) ? row[keys[Währung]].ToString() : null);
                command.Parameters.AddWithValue("@" + Info, keys.ContainsKey(Info) ? row[keys[Info]].ToString() : null);
                command.Parameters.AddWithValue("@" + KategorieHaupt, null);
                command.Parameters.AddWithValue("@" + KategorieSub, null);

                command.Prepare();
                try
                {
                    command.ExecuteNonQuery();
                }
                catch(MySqlException exception)
                {
                    throw new Exception("Fehler bei Tabelle "+ table+ "=>banking.umsaetze , Datei " + import.FileInfo.FullName+"\n"+exception.Message);
                }
            }

            Connection.Close();
        }

        /**
         * @todo Umzug in Umseatze Klasse
         */
        private static Dictionary<string, object> GetRow(string sourceIBAN, DateTime buchungstag, decimal betrag)
        {
            Connection2.Open();

            MySqlCommand uniqueCommand = new("SELECT " + ID +", " + KategorieHaupt + "," + KategorieSub + " FROM banking.umsaetze WHERE " + SourceIBAN + "= @" + SourceIBAN + " AND " + Buchungstag + "= @" + Buchungstag + " AND " + Betrag + "= @" + Betrag, Connection2);
            uniqueCommand.Parameters.AddWithValue("@" + SourceIBAN, sourceIBAN);
            uniqueCommand.Parameters.AddWithValue("@" + Buchungstag, buchungstag);
            uniqueCommand.Parameters.AddWithValue("@" + Betrag, betrag);
            uniqueCommand.Prepare();
            MySqlDataReader mysqlDataReader = uniqueCommand.ExecuteReader();
            mysqlDataReader.Read();

            Dictionary<string, object> result = new();
            if (mysqlDataReader.HasRows)
            {
                result[ID] = mysqlDataReader.GetInt32(0);
                result[KategorieHaupt] = mysqlDataReader.IsDBNull(1) ? null : mysqlDataReader.GetString(1);
                result[KategorieSub] = mysqlDataReader.IsDBNull(2) ? null : mysqlDataReader.GetString(2);
            }
            else
            {
                result = null;
            }
            Connection2.Close();

            return result;
        }

        internal static void UpdateUmsaetze()
        {
            string sqlUpdate = "UPDATE banking.umsaetze SET " + KategorieHaupt + "= @" + KategorieHaupt + ", " + KategorieSub + "= @" + KategorieSub + ", " + KategorieID + "= @" + KategorieID + " WHERE " + ID + "= @" + ID + " AND " + KategorieHaupt + " IS NULL AND " + KategorieSub + " IS NULL";
            
            Connection.Open();

            MySqlCommand dataCommand = new("SELECT " + ID + ", " + KategorieHaupt + "," + KategorieSub + ", " + TargetIBAN + ", " + Empfänger + ", " + Buchungstext + ", " + Verwendungszweck + " FROM banking.umsaetze", Connection);
            MySqlDataAdapter da = new(dataCommand);
            DataTable umsaetze = new();
            da.Fill(umsaetze);
            



            foreach (DataRow buchung in umsaetze.Rows)
            {
                CSVHeaders foundKategorie = GetKategorie(buchung[TargetIBAN].ToString(), buchung[Empfänger].ToString(), buchung[Buchungstext].ToString(), buchung[Verwendungszweck].ToString());
                if(foundKategorie[KategorieHaupt] == null && foundKategorie[KategorieSub] == null)
                {
                    continue;
                }

                if (buchung[KategorieHaupt] == DBNull.Value && buchung[KategorieSub] == DBNull.Value &&
                        (foundKategorie[KategorieHaupt] != buchung[KategorieHaupt].ToString() || foundKategorie[KategorieSub] != buchung[KategorieSub].ToString()))
                {
                    using MySqlCommand commandUpdate = new(sqlUpdate, Connection);

                    commandUpdate.Parameters.AddWithValue("@" + KategorieHaupt, foundKategorie[KategorieHaupt]);
                    commandUpdate.Parameters.AddWithValue("@" + KategorieSub, foundKategorie[KategorieSub]);
                    commandUpdate.Parameters.AddWithValue("@" + KategorieID, foundKategorie[ID]);
                    commandUpdate.Parameters.AddWithValue("@" + ID, buchung[ID]);
                    commandUpdate.Prepare();
                    commandUpdate.ExecuteNonQuery();
                    Console.WriteLine("Zeile " + buchung[ID] + " aktualisiert zu " + foundKategorie[KategorieHaupt] + " " + foundKategorie[KategorieSub] + " Regel:" + foundKategorie[ID]);
                }else if((foundKategorie[KategorieHaupt] != buchung[KategorieHaupt].ToString() || foundKategorie[KategorieSub] != buchung[KategorieSub].ToString()))
                {
                    Console.WriteLine("Zeile " + buchung[ID] + " Unterschied von "+ buchung[KategorieHaupt].ToString() + " " + buchung[KategorieSub].ToString() + " zu " + foundKategorie[KategorieHaupt] + " " + foundKategorie[KategorieSub] + " Regel:" + foundKategorie[ID]);
                }
            }

            Connection.Close();

        }

        internal static void UpdateRules()
        {

            string sqlUpdate = @"UPDATE filter_rules 
                JOIN(SELECT KategorieID, count(umsaetze.id) as umsaetze_id, max(Buchungstag) maxBuchungstag FROM banking.umsaetze GROUP BY KategorieID) t1 ON t1.KategorieID = filter_rules.ID
                SET LastUsed = maxBuchungstag, Count = umsaetze_id;";
            Connection.Open();
            MySqlCommand truncate = new(sqlUpdate, Connection);
            truncate.ExecuteNonQuery();
            Connection.Close();

        }

        /** @todo returnvale => DataTable */
        private static CSVHeaders GetKategorie(string targetIBAN, string empfänger, string buchungstext, string verwendungszweck)
        {
            if (TransferData.FilterRules == null)
            {
                Connection3.Open();
                MySqlCommand command = new("SELECT " + ID + "," + Empfänger + "," + Buchungstext + "," + TargetIBAN + ", " + Verwendungszweck + ", " + KategorieHaupt + "," + KategorieSub + " FROM banking.filter_rules", Connection3);

                MySqlDataAdapter da = new(command);
                DataTable result = new();
                da.Fill(result);

                TransferData.FilterRules = result;

                Connection3.Close();

            }

            CSVHeaders returnValue = new() { { KategorieHaupt, null }, { KategorieSub, null }, { ID, null } };

            foreach (DataRow filterRow in TransferData.FilterRules.Rows)
            {
                if (
                    (filterRow[Empfänger] == DBNull.Value || empfänger.Contains(filterRow[Empfänger].ToString(), StringComparison.CurrentCultureIgnoreCase)) &&
                    (filterRow[Verwendungszweck] == DBNull.Value || verwendungszweck.Contains(filterRow[Verwendungszweck].ToString(), StringComparison.CurrentCultureIgnoreCase)) &&
                    (filterRow[Buchungstext] == DBNull.Value || buchungstext==filterRow[Buchungstext].ToString()) &&
                    (filterRow[TargetIBAN] == DBNull.Value || targetIBAN == filterRow[TargetIBAN].ToString())
                )
                {
                    if(returnValue[KategorieHaupt]!=null && returnValue[KategorieSub] != null)
                    {
                        // Wenn es zwei Regeln gibt, die sich aber nicht wiedersprechen (Paypal...)
                        // @todo noch notwendig, wenn PayPal Import da?
                        if (returnValue[KategorieHaupt] == filterRow[KategorieHaupt].ToString() && returnValue[KategorieSub] == filterRow[KategorieSub].ToString())
                        {
                            filterRow[KategorieHaupt] = returnValue[KategorieHaupt];
                            filterRow[KategorieSub] = returnValue[KategorieSub];
                        }
                        else
                        {
                            throw new Exception("Kategorie schon gefunden! Gefunden:" +
                                returnValue[KategorieHaupt].ToString() + " " + returnValue[KategorieSub].ToString() + " " + filterRow[ID].ToString() + "\n" +
                                "Schon vorhanden: " + returnValue[KategorieHaupt] + " " + returnValue[KategorieSub] + " " + returnValue[ID] + "\n" +
                                "Durchsuchte Zeile: IBAN " + targetIBAN + " Empfänger " + empfänger + " Buchungstext " + buchungstext + " Verwendungszweck" + verwendungszweck
                                );
                        }
                    }
                    returnValue[KategorieHaupt] = filterRow[KategorieHaupt].ToString();
                    returnValue[KategorieSub] = filterRow[KategorieSub].ToString();
                    returnValue[ID] = filterRow[ID].ToString();
                }
            }

           
            return returnValue;
            

        }
    }
}
