using System;
using System.Collections.Generic;
using System.Text;

namespace Log2Console.Configuration
{
    /// <summary>
    /// To provide configurations settings
    /// </summary>
    public class Configuration : IConfiguration
    {
        public new ConfigurationSettings Configurations { get; set; }
    }
}
