using System;
using System.Collections.Generic;
using System.Text;
using Log2Console.Log;
using System.IO;
using Log2Console.Receiver;
using System.Globalization;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Log2Console.Parser;
using Log2Console.Settings;
using System.Data;
using Log2Console.Configuration;
//using System.Configuration;

namespace Log2Console.Parser
{
    class SQLServerParser : BaseParser
    {
        public const string const_databaseRowsCount = "DatabaseRowsCount";
        /// <summary>
        /// Function to parse dabase log table
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override List<LogMessage> Parse(Log2Console.Configuration.Configuration config)
        {
            try
            {
                ConfigurationSettings setting = config.Configurations;
                List<LogMessage> logMsgs = GetLoggerItems(setting.FieldList, setting.ConnectionString, setting.LogTableName, setting.RowsCount);
                return logMsgs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Db call to fetch records from log table
        /// </summary>
        /// <param name="_fieldList"></param>
        /// <param name="connectionString"></param>
        /// <param name="logTableName"></param>
        /// <returns></returns>
        public List<LogMessage> GetLoggerItems(FieldType[] _fieldList, string connectionString, string logTableName, int rowcount)
        {
            List<LogMessage> logMsgs = new List<LogMessage>();
            SqlConnection connection = null;

            using (connection = this.DbConnect(connectionString))
            {
                if (connection == null)
                {
                    return null;
                }
                try
                {
                    SqlCommand sqlCmd = connection.CreateCommand();
                    sqlCmd.CommandText = GetSqlAllLoggerStatement(_fieldList, logTableName, rowcount);
                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = sqlCmd;
                    DataSet ds = new DataSet();
                    connection.Open();
                    //da.SelectCommand.
                    da.Fill(ds);
                    connection.Close();

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            LogMessage logmsg = new LogMessage();
                            foreach (FieldType item in _fieldList)
                            {
                                ParseFields(ref logmsg, Convert.ToString(row[item.Name]), item);
                            }
                            logMsgs.Add(logmsg);
                        }
                    }
                }
                catch (SqlException exceptionSQL)
                {
                    connection.Close();
                    ApplicationException exception1;
                    if (exceptionSQL.Message.Contains("Invalid column name"))
                    {
                        exception1 = new ApplicationException("Please map log table columns name properly", exceptionSQL);
                    }
                    else
                    {
                        exception1 = new ApplicationException("Error reading logrows from database collecting all logger names", exceptionSQL);
                    }
                    throw exception1;
                }
                catch (Exception exception)
                {
                    //Column '' does not belong to table Table.
                    connection.Close();
                    ApplicationException exception2;
                    if (exception.Message.Contains("does not belong to table Table"))
                    {
                        exception2 = new ApplicationException("Please map log table columns name properly", exception);
                    }
                    else
                        exception2 = new ApplicationException("Error reading logrows from database collecting all logger names", exception);
                    throw exception2;
                }
            }
            return logMsgs;
        }
        /// <summary>
        /// SQL command text
        /// </summary>
        /// <param name="_fieldList"></param>
        /// <param name="logTableName"></param>
        /// <returns></returns>
        public static string GetSqlAllLoggerStatement(FieldType[] _fieldList, string logTableName, int rowscount)
        {
            if (_fieldList.Length > 0)
            {
                //If rows count not set by user then take values from webconfig file.
                if (rowscount <= 0)
                {
                    if (!string.IsNullOrEmpty(System.Configuration.ConfigurationSettings.AppSettings[const_databaseRowsCount]))
                        rowscount = Convert.ToInt16(System.Configuration.ConfigurationSettings.AppSettings[const_databaseRowsCount]);
                }

                StringBuilder builder = new StringBuilder();
                
                //To get last inserted 'n' rows from database table.
                builder.Append("DECLARE @RowsCount INT;select @RowsCount=count(" + _fieldList[0].Name);
                builder.Append(") FROM ");
                builder.Append(logTableName);
                builder.Append(";SELECT ");
                foreach (FieldType item in _fieldList)
                {
                    if (!string.IsNullOrEmpty(item.Name))
                        builder.Append(item.Name + ",");
                }
                builder.Length -= 1;//To delete , as the last charecter
                builder.Append(" FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 0)) AS Row,");
                foreach (FieldType item in _fieldList)
                {
                    if (!string.IsNullOrEmpty(item.Name))
                        builder.Append(item.Name + ",");
                }
                builder.Length -= 1;//To delete , as the last charecter
                builder.Append(" FROM " + logTableName + ") AS LogRows");
                builder.Append(" WHERE (Row between (@RowsCount-" + rowscount + "+1) AND @RowsCount)");

                return builder.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// To provide database connection
        /// </summary>
        /// <param name="_connectionString"></param>
        /// <returns></returns>
        private SqlConnection DbConnect(string _connectionString)
        {
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(_connectionString);
            }
            catch (Exception exception)
            {
                ApplicationException exception2 = new ApplicationException("Error Connection to Datasource using Connection string:" + _connectionString + " Error message: " + exception.Message, exception);
                throw exception2;
            }
            return connection;
        }
        /// <summary>
        /// To parse data according to database fields
        /// </summary>
        /// <param name="logMsg"></param>
        /// <param name="dataValue"></param>
        /// <param name="valueType"></param>
        private void ParseFields(ref LogMessage logMsg, string dataValue, FieldType valueType)
        {
            try
            {
                switch (valueType.Field)
                {
                    case LogMessageField.SequenceNr:
                        logMsg.SequenceNr = ulong.Parse(dataValue);
                        break;
                    case LogMessageField.LoggerName:
                        logMsg.LoggerName = dataValue;
                        break;
                    case LogMessageField.Level:
                        //logMsg.Level = LogLevels.Instance[(Log2Console.Log.LogLevel)Enum.Parse(typeof(Log2Console.Log.LogLevel), dataValue)];
                        logMsg.Level = LogLevels.Instance[dataValue];
                        break;
                    case LogMessageField.Message:
                        logMsg.Message = dataValue;
                        break;
                    case LogMessageField.ThreadName:
                        logMsg.ThreadName = dataValue;
                        break;
                    case LogMessageField.TimeStamp:
                        DateTime time = Convert.ToDateTime(dataValue);
                        logMsg.TimeStamp = time;
                        break;
                    case LogMessageField.Exception:
                        logMsg.ExceptionString = dataValue;
                        break;
                    case LogMessageField.Properties:
                        logMsg.Properties[valueType.Name] = dataValue;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
