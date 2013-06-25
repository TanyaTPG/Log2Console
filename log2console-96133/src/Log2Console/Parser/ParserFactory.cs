using System;
using System.Collections.Generic;
using System.Text;

namespace Log2Console.Parser
{
    /// <summary>
    /// To decide which type of receiver class need to call at run time 
    /// </summary>
    public class ParserFactory
    {
        public enum ParserFileEnums
        {
            Xml,
            Flat,
            SQL,
            Event,
            MySQL,
        }
        public IParser GetParser(ParserFileEnums type)
        {
            IParser parser=null;
            switch (type)
            {
                case ParserFileEnums.Flat:
                    parser= new FlatFileParser();
                    break;
                case ParserFileEnums.SQL:
                    parser = new SQLServerParser();
                    break;
                case ParserFileEnums.MySQL:
                    parser = new MySQLParser();
                    break;
                default:
                    parser= new FlatFileParser();
                    break;
            }
            return parser;
        }
    }
}
