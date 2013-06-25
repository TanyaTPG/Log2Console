using Log2Console.Log;
using System.Collections.Generic;
using System.IO;

namespace Log2Console.Parser
{
    public interface IParser
    {
        /// <summary>
        /// Function to parse log object to get all logs
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        List<LogMessage> Parse(Log2Console.Configuration.Configuration config);
    }
}
