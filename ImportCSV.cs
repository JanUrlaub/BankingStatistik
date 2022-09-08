using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using CSVRows = System.Collections.Generic.Dictionary<string, string>;
using BankingStatistik.ImportHandler;
using System;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Configuration;

namespace BankingStatistik
{
    public class ImportCSV
    {
        private static MySqlConnection Connection;

        public List<CSVRows> RawData { get; }
        public string[] Header { get; }

		public FileInfo fileInfo { get; }

        static ImportCSV()
        {
            string server = ConfigurationManager.AppSettings["DBserver"];
            string user = ConfigurationManager.AppSettings["DBuser"];
            string password = ConfigurationManager.AppSettings["DBpassword"];
            string database = ConfigurationManager.AppSettings["DBdatabase"];

            Connection = new MySqlConnection("server="+server+";uid="+user+";pwd="+password+";database="+database);
        }

        public ImportCSV(FileInfo file)
        {
            TextFieldParser csvReader = new(file.FullName, Encoding.GetEncoding("ISO-8859-1"));
            csvReader.CommentTokens = new string[] { "#" };
            csvReader.SetDelimiters(new string[] { ";" });
            csvReader.HasFieldsEnclosedInQuotes = true;

            CSVRows globalValues = new();
            this.fileInfo = file;

            // Skip the row with the column names
            Header = csvReader.ReadFields();
            while (Header.Length == 3)
            {
                if(!Header[0].StartsWith("Kontostand"))
                {
                    globalValues.Add(Header[0], Header[1]);
                }
                
                Header = csvReader.ReadFields();
            }
            
            List<CSVRows> converted = new();


            while (!csvReader.EndOfData)
            {
                // Read current line fields, pointer moves to the next line.
                CSVRows row = new();
                string[] fields = csvReader.ReadFields();
                foreach(KeyValuePair<string, string> entry in globalValues)
                {
                    row.Add(entry.Key, entry.Value);
                }
                for (int key = 0; key < fields.Length; ++key)
                {
                    row.Add(Header[key], fields[key]);

                }
                converted.Add(row);
                
            }

            Header = globalValues.Keys.ToArray().Concat(Header).ToArray();
            // @todo workaround or solution?
            if (Header[^1] == "")
            {
                Header= Header.SkipLast(1).ToArray();
            }

            this.RawData = converted;
        }

        internal static void SetImported(AbstractImport file)
        {
            Connection.Open();
            SHA256 SHA256 = SHA256.Create();

            FileStream fileStream = File.OpenRead(file.FileInfo.FullName) ;
            string sqlInsert = "INSERT INTO banking.importe (sha1, filename) VALUES (@sha1,@filename)";
            MySqlCommand commandUpdate = new(sqlInsert, Connection);
            commandUpdate.Parameters.AddWithValue("@sha1" , Convert.ToBase64String(SHA256.ComputeHash(fileStream)));
            commandUpdate.Parameters.AddWithValue("@filename", file.FileInfo.Name);
            commandUpdate.Prepare();
            commandUpdate.ExecuteNonQuery();
            Connection.Close();
        }

        internal static bool IsImported(AbstractImport file)
        {
            Connection.Open();
            SHA256 SHA256 = SHA256.Create();

            FileStream fileStream = File.OpenRead(file.FileInfo.FullName);
            string sqlInsert = "SELECT * FROM  banking.importe WHERE sha1=@sha1";
            using MySqlCommand commandUpdate = new(sqlInsert, Connection);
            commandUpdate.Parameters.AddWithValue("@sha1", Convert.ToBase64String(SHA256.ComputeHash(fileStream)));

            MySqlDataReader reader = commandUpdate.ExecuteReader();
            bool result = reader.HasRows;
            Connection.Close();
            return result;
        }

        public static List<AbstractImport> GetImportFiles(DirectoryInfo directory)
        {
            List<AbstractImport> result = new();
            string keysSparkasseGiro = string.Join(",", SparkasseGiro.translateColumns2.Keys.ToArray());
            string keysSparkasseKredit = string.Join(",", SparkasseKredit.translateColumns2.Keys.ToArray());
            string keysDKBGiro = string.Join(",", DKBGiro.translateColumns2.Keys.ToArray());
            string keysDKBKredit = string.Join(",", DKBKredit.translateColumns2.Keys.ToArray());


            foreach (FileInfo file in directory.GetFiles())
            {
                ImportCSV csv = new(file);
                string foundHeader = string.Join(",", csv.Header);

                if (foundHeader == keysSparkasseGiro)
                {
                    result.Add(new SparkasseGiro(csv));
                }
                else if (foundHeader == keysSparkasseKredit)
                {
                    result.Add(new SparkasseKredit(csv));
                }
                else if (foundHeader == keysDKBGiro)
                {
                    result.Add(new DKBGiro(csv));
                }
                else if(foundHeader == keysDKBKredit)
                {
                    result.Add(new DKBKredit(csv));
                }
                else
                {
                    throw new Exception("unbekannter Dateityp: " + string.Join(",", csv.Header)+" in Datei "+ file.FullName);
                }
            }
            return result;

        }

    }
}
