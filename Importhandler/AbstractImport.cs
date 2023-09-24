using System;
using System.Collections.Generic;
using System.IO;
using CSVHeaders = System.Collections.Generic.Dictionary<string, string>;

namespace BankingStatistik.ImportHandler
{
    public abstract class AbstractImport
    {
        internal CSVHeaders translateColumns;
        internal string importTable;
        internal string importTableWhere;

        public List<CSVHeaders> rawData { get; } 
        public FileInfo FileInfo { get; }

        public AbstractImport(ImportCSV csv)
        {
            this.rawData = csv.RawData;
            this.FileInfo = csv.fileInfo;
        }
        
        public abstract string convert(string value, string colum);

        public abstract DateTime convertDate(string value);
    }
}
