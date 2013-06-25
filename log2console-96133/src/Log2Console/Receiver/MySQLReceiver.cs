using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Log2Console.Log;
using Log2Console.Settings;
using Log2Console.Parser;
using Log2Console.Configuration;

namespace Log2Console.Receiver
{
    /// <summary>
    /// MySQL database log table rows reading.
    /// </summary>
    [Serializable]
    [DisplayName("MySQL Log Table")]
    public class MySQLReceiver : BaseReceiver
    {
        private string _connectionString = String.Empty;
        private string _logTableName = String.Empty;
        private int _rowsCount = 100;


        [Category("Configuration")]
        [DisplayName("Connection String")]
        [Description("Provide connectionstring to connect with database log table.")]
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                if (String.Compare(_connectionString, value, true) == 0)
                    return;

                _connectionString = value;
            }
        }

        [Category("Configuration")]
        [DisplayName("Log Table")]
        [Description("Provide name for the log table in database.")]
        public string LogTableName
        {
            get { return _logTableName; }
            set
            {
                if (String.Compare(_logTableName, value, true) == 0)
                    return;

                _logTableName = value;
            }
        }

        [Category("Configuration")]
        [DisplayName("Rows Count")]
        [Description("The number of rows to fetch from database.")]
        [DefaultValue(100)]
        public int RowsCount
        {
            get { return _rowsCount; }
            set
            {
                _rowsCount = value;
            }
        }

        private FieldType[] _fieldList = new[]
                                             {
                                                 new FieldType(LogMessageField.LoggerName, "Logger"),
                                                 new FieldType(LogMessageField.TimeStamp, "Date"),
                                                 new FieldType(LogMessageField.Level, "Level"),
                                                 new FieldType(LogMessageField.ThreadName, "Thread"),
                                                 new FieldType(LogMessageField.Message, "Message"),
                                                 new FieldType(LogMessageField.Exception, "Exception")
                                             };

        [Category("Configuration")]
        [DisplayName("Database table columns")]
        [Description("Match the database coulmn names with field types.")]
        public FieldType[] FieldList
        {
            get { return _fieldList; }
            set { _fieldList = value; }
        }

        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig
        {
            get
            {
                return @"<log4net>
                    <root>
                        <level value=""DEBUG"" />
                            <appender-ref ref=""ADONetAppender"" />
                    </root>
                    <appender name=""ADONetAppender"" type=""log4net.Appender.ADONetAppender"">
                        <bufferSize value=""100"" />
                        <connectionType value=""System.Data.SqlClient.SqlConnection, System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
                        <connectionString value=""server=servername; uid=Lion; pwd=Lionman; database=databasename"" />
                        <commandText value=""INSERT INTO Log ([Date],[Thread],[Level],[Logger],[Message],[Exception]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception)"" />
                        <parameter>
                            <parameterName value=""@log_date""/>
                            <dbType value=""DateTime""/> 
                            <layout type=""log4net.Layout.RawTimeStampLayout""/>
                        </parameter>
                        <parameter>
                            <parameterName value=""@thread""/>
                            <dbType value=""String""/>
                            <size value=""255""/> 
                            <layout type=""log4net.Layout.PatternLayout"">
                                <conversionPattern value=""%thread""/>
                            </layout>
                        </parameter>
                        <parameter>
                            <parameterName value=""@log_level""/> <dbType value=""String""/>
                            <size value=""50""/>
                            <layout type=""log4net.Layout.PatternLayout"">
                                <conversionPattern value=""%level""/>
                            </layout>
                        </parameter>
                        <parameter>
                            <parameterName value=""@logger""/>
                            <dbType value=""String""/>
                            <size value=""255""/>
                            <layout type=""log4net.Layout.PatternLayout"">
                                <conversionPattern value=""%logger""/>
                            </layout>
                        </parameter>
                        <parameter>
                            <parameterName value=""@message""/>
                            <dbType value=""String""/>
                            <size value=""4000""/> 
                            <layout type=""log4net.Layout.PatternLayout""> 
                                <conversionPattern value=""%message""/> 
                            </layout> 
                        </parameter>
                        <parameter>
                            <parameterName value=""@exception""/> 
                            <dbType value=""String""/> 
                            <size value=""2000""/>
                            <layout type=""log4net.Layout.ExceptionLayout""/> 
                        </parameter>
                    </appender>
                </log4net>";
            }
        }

        public override void Initialize()
        {
        }
        public override void Terminate()
        {
        }

        public override void Attach(ILogMessageNotifiable notifiable)
        {
            base.Attach(notifiable);

            if (!string.IsNullOrEmpty(_connectionString) && !string.IsNullOrEmpty(_logTableName))
            {
                ConnectionString = _connectionString;
                LogTableName = _logTableName;
                ReadLogTable();
            }
        }

        #endregion

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            ReadLogTable();
        }

        /// <summary>
        /// To read data from database log table
        /// </summary>
        private void ReadLogTable()
        {
            //Factory pattern to decide which class object need to invoke for file parsing
            ParserFactory parseFact = new ParserFactory();
            IParser parser = parseFact.GetParser(ParserFactory.ParserFileEnums.MySQL);

            //Now decide which all parameters we need to pass to parse function
            ConfigurationSettings settings = new ConfigurationSettings();
            settings.FieldList = _fieldList;
            settings.LogTableName = LogTableName;
            settings.ConnectionString = ConnectionString;
            settings.RowsCount = RowsCount;

            Log2Console.Configuration.Configuration config = new Log2Console.Configuration.Configuration();
            config.Configurations = settings;


            //At runtime call
            List<LogMessage> logMsgs = parser.Parse(config);


            // Notify the UI with the set of messages
            Notifiable.Notify(logMsgs.ToArray());
        }
    }
}
