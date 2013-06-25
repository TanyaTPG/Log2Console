using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Log2Console.Configuration
{
    /// <summary>
    /// Common class to contain all input parameters for Parse function used in Receiver class
    /// </summary>
    public class ConfigurationSettings
    {
         public StreamReader FileReader { get; set; }
         public string ConversionPattern { get; set; }
         public Log2Console.Settings.FieldType[] FieldList { get; set; }
         public string LogTableName { get; set; }
         public string ConnectionString { get; set; }
         public int RowsCount { get; set; }
    }
}
