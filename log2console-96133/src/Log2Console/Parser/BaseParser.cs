using System;
using System.ComponentModel;

using Log2Console.Log;
using System.IO;
using System.Collections.Generic;

namespace Log2Console.Parser
{
    [Serializable]
    public abstract class BaseParser : IParser
    {
        #region IParser Function
        /// <summary>
        /// Function to parse log object to get all logs
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public virtual List<LogMessage> Parse(Log2Console.Configuration.Configuration config)
        {
            List<LogMessage> lst = new List<LogMessage>();
            return lst;
        }
        #endregion
    }
}
