using BankingStatistik.ImportHandler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;


namespace BankingStatistik
{
    /**
     * @todo Paypal import und Kategorie mappen bei Empfänger Paypal
     * @todo insert/update/meldung delete
     * @todo direktes lesen und importieren von den Seiten statt über Importverzeichniss zu gehen
     */
    class Program
    {
        static void Main(string[] args)
        {
            //string dkbUser = ConfigurationManager.AppSettings["DKBuser"];
            //string dkbPassword = ConfigurationManager.AppSettings["DKBpassword"];
            //DKBGiro.getImportData(new DirectoryInfo(args[0]), dkbUser, dkbPassword);

            //string spkUser = ConfigurationManager.AppSettings["SPKuser"];
            //string spkPassword = ConfigurationManager.AppSettings["SPKpassword"];
            //SparkasseGiro.getImportData(new DirectoryInfo(args[0]), spkUser, spkPassword); 

            List <AbstractImport> files = ImportCSV.GetImportFiles(new DirectoryInfo(args[0]));

            foreach (AbstractImport file in files)
            {
                if(ImportCSV.IsImported(file))
                {
                    continue;
                }

                TransferData data = new(file);

                data.ImportRawData(file);
                TransferData.ConvertData(file);

                ImportCSV.SetImported(file); 
            }


            TransferData.UpdateUmsaetze();
            TransferData.UpdateRules();


        }
    }
}
