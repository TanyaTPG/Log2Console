using System;
using System.Collections.Generic;
using System.Text;
using Log2Console.Log;
using System.IO;
using Log2Console.Receiver;
using System.Globalization;
using Log2Console.Configuration;

namespace Log2Console.Parser
{
    /// <summary>
    /// Class to parse flat log file written using pattern layout
    /// </summary>
    class FlatFileParser : BaseParser
    {
        /// <summary>
        /// Function to parse flat log file
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public override List<LogMessage> Parse(Log2Console.Configuration.Configuration config)
        {
            ConfigurationSettings setting = config.Configurations;//Get parameters which has been passed from Receiver.
            string line;
            var sb = new StringBuilder();
            List<LogMessage> logMsgs = new List<LogMessage>();

            /////////////////////////////////////////////////////////////
            //To get conversion pattern string arrayk
            LoggerFormatParser formatparser = new LoggerFormatParser();
            formatparser.ParseLog4J4NetFormatString(setting.ConversionPattern);
            ////////////////////////////////////////////////////////////

            while ((line = setting.FileReader.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    int errorCount = 0;
                    LogMessage logMsg = new LogMessage();
                    try
                    {
                        //////////////////////////////////////////////////////
                        foreach (var item in formatparser.ColInfoList)
                        {
                            string[] stringSeparators = new string[] { item.TrailingSeparator };
                            string separator = stringSeparators[0].ToString();
                            string[] strResult = new string[0];
                            string dataValue;

                            strResult = line.Split(stringSeparators, StringSplitOptions.None);
                            if (!string.IsNullOrEmpty(item.FormatString))//for datetime string cut value exactly same as formatstring instead separator
                            {
                                if (strResult[0].Length <= 10)
                                    dataValue = strResult[0] + separator + strResult[1];
                                else
                                    dataValue = strResult[0];
                            }
                            else
                            {

                                dataValue = strResult[0];
                            }


                            errorCount = LoggerFormatParser.SetLogMsgObj(dataValue, item.Header.ToLower(), logMsg, item.FormatString);//Get logMsg object filled.
                            if (errorCount > 0)//Case of stack trace where next complete line is the part of previous message.
                                break;

                            int count = 0;

                            if (!string.IsNullOrEmpty(line))
                            {
                                if (!string.IsNullOrEmpty(item.FormatString))//for datetime string cut exactly same as formatstring
                                {
                                    count = (item.FormatString + separator).Length;
                                    line = line.Remove(0, count);
                                }
                                else if (item.MinLength > 0)//case %-5level, where length is defined for level column value is 5
                                {
                                    count = item.MinLength + separator.Length;
                                    line = line.Remove(0, count);
                                }
                                else
                                    line = line.Replace(strResult[0] + separator, "");//Simple case where we need to cut already processed area from text line
                            }
                        }
                        //////////////////////////////////////////////////////
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    if (errorCount > 0)//Case of stack trace where next complete line is the part of previous message.
                        logMsgs[logMsgs.Count - 1].Message += Environment.NewLine + line;//Append this line into previous message.
                    else
                        logMsgs.Add(logMsg);
                }
            }

            return logMsgs;
        }
    }
    /// <summary>
    /// Utill class to provide utility functions
    /// </summary>
    public class LoggerFormatParser
    {
        private static readonly string log4JNetDefaultDateFormat = "yyyy-MM-dd HH:mm:ss,fff";
        //private static readonly string nlogDateFormat = "MM/dd/yyyy HH:mm:ss";
        //private static readonly string nlogLongDateFormat = "yyyy-MM-dd HH:mm:ss.ffff";

        public LoggerFormatParser()
        {
            this.ColInfoList = new List<ColumnInfo>();
        }

        /// <summary>
        /// To convert Conversion pattern into string array as per given configuration
        /// </summary>
        /// <param name="format"></param>
        public void ParseLog4J4NetFormatString(string format)
        {
            this.ColInfoList = new List<ColumnInfo>();
            ColumnInfo item = new ColumnInfo();
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            foreach (char ch in format)
            {
                if (flag2)
                {
                    if (ch == '}')
                    {
                        flag = false;
                        flag2 = false;
                    }
                    else
                    {
                        item.FormatString = item.FormatString + ch;
                    }
                }
                else if (ch == '%')
                {
                    flag3 = true;
                    if (item.HasValue)
                    {
                        this.ColInfoList.Add(item);
                        item = new ColumnInfo();
                    }
                    flag = true;
                    flag2 = false;
                }
                else if (!flag)
                {
                    if (flag3)
                    {
                        item.TrailingSeparator = item.TrailingSeparator + ch;
                    }
                    else
                    {
                        item.LeadingSeparator = item.LeadingSeparator + ch;
                    }
                }
                else if (ch == '{')
                {
                    flag2 = true;
                }
                else if (((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z')))
                {
                    item.AddHeaderChar(ch);
                }
                else
                {
                    switch (ch)
                    {
                        case '-':
                            item.PadRight = true;
                            break;

                        case '.':
                            item.Truncate = true;
                            break;

                        default:
                            if ((ch >= '0') && (ch <= '9'))
                            {
                                item.AddNumberChar(ch);
                            }
                            else
                            {
                                item.TrailingSeparator = item.TrailingSeparator + ch;
                                flag = false;
                                flag2 = false;
                            }
                            break;
                    }
                }
            }
            if (item.HasValue)
            {
                this.ColInfoList.Add(item);
            }
            int index = -1;
            for (int i = 0; i < this.ColInfoList.Count; i++)
            {
                if ((this.ColInfoList[i].MinLength > this.ColInfoList[i].MaxLength) && (this.ColInfoList[i].MaxLength != -1))
                {
                    this.ColInfoList[i].MinLength = this.ColInfoList[i].MaxLength;
                }
                if (((this.ColInfoList[i].Header == "date") || (this.ColInfoList[i].Header == "d")) || (this.ColInfoList[i].Header == "utcdate"))
                {
                    if (this.ColInfoList[i].FormatString.Length == 0)
                    {
                        this.ColInfoList[i].FormatString = log4JNetDefaultDateFormat;
                        this.ColInfoList[i].MinLength = this.ColInfoList[i].FormatString.Length;
                    }
                }
                else if ((this.ColInfoList[i].Header == "newline") || (this.ColInfoList[i].Header == "n"))
                {
                    index = i;
                    break;
                }
            }
            if ((index != -1) && (index > 0))
            {
                this.ColInfoList.RemoveRange(index, this.ColInfoList.Count - index);
                char[] trimChars = new char[] { ' ', '\t' };
                this.ColInfoList[this.ColInfoList.Count - 1].TrailingSeparator = this.ColInfoList[this.ColInfoList.Count - 1].TrailingSeparator.TrimEnd(trimChars);
            }
        }

        /// <summary>
        /// To Fill logMsg object from text line
        /// </summary>
        /// <param name="value"></param>
        /// <param name="displayName"></param>
        /// <param name="logMsg"></param>
        /// <param name="formatString"></param>
        public static int SetLogMsgObj(string value, string displayName, LogMessage logMsg, string formatString)
        {
            try
            {
                switch (displayName)
                {
                    case "message":
                    case "m":
                        logMsg.Message = value;
                        break;

                    case "level":
                        logMsg.Level = LogLevels.Instance[value];
                        break;

                    case "logger":
                        logMsg.LoggerName = value;
                        break;

                    case "date":
                    case "d":
                    case "longdate":
                    case "utcdate":
                        DateTime dateVal;
                        if (formatString.Contains("fff"))
                        {
                            IFormatProvider culture = new CultureInfo("en-US", true);
                            dateVal = DateTime.ParseExact(value, formatString, culture);
                        }
                        else
                        {
                            dateVal = Convert.ToDateTime(value);
                        }

                        logMsg.TimeStamp = dateVal;
                        break;

                    case "thread":
                    case "threadid":
                        logMsg.ThreadName = value;
                        break;
                }
                return 0;
            }
            catch (FormatException ex)
            {
                if (ex.Message.ToLower().Contains("string was not recognized as a valid datetime."))
                    return 1;//Case where stack tarce kind of data inside message field.
                else
                    throw;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<ColumnInfo> ColInfoList { get; set; }

        public class ColumnInfo
        {
            private bool padLeft;
            private bool padRight;
            private bool truncate;

            public ColumnInfo()
            {
                this.Header = "";
                this.LeadingSeparator = "";
                this.TrailingSeparator = "";
                this.FormatString = "";
                this.NumberValue = "";
                this.MinLength = -1;
                this.MaxLength = -1;
            }

            public void AddHeaderChar(char nextChar)
            {
                this.Header = this.Header + nextChar;
                this.UpdateMinMax();
            }

            public void AddNumberChar(char nextChar)
            {
                if ((!this.PadLeft && !this.PadRight) && !this.Truncate)
                {
                    this.PadLeft = true;
                }
                this.NumberValue = this.NumberValue + nextChar;
            }

            private void UpdateMinMax()
            {
                if ((this.PadLeft || this.PadRight) && (this.NumberValue.Length > 0))
                {
                    bool flag;
                    this.MinLength = Convert.ToInt16(this.NumberValue);
                    this.Truncate = flag = false;
                    this.PadLeft = this.PadRight = flag;
                    this.NumberValue = "";
                }
                else if (this.Truncate && (this.NumberValue.Length > 0))
                {
                    bool flag3;
                    this.MaxLength = Convert.ToInt16(this.NumberValue);
                    this.Truncate = flag3 = false;
                    this.PadLeft = this.PadRight = flag3;
                    this.NumberValue = "";
                }
            }

            public string FormatString { get; set; }

            public bool HasValue
            {
                get
                {
                    if ((this.TrailingSeparator.Length <= 0) && (this.Header.Length <= 0))
                    {
                        return false;
                    }
                    return true;
                }
            }

            public string Header { get; set; }

            public string LeadingSeparator { get; set; }

            public int MaxLength { get; set; }

            public int MinLength { get; set; }

            public string NumberValue { get; set; }

            public bool PadLeft
            {
                get
                {
                    return this.padLeft;
                }
                set
                {
                    if (value)
                    {
                        this.UpdateMinMax();
                    }
                    this.padLeft = value;
                }
            }

            public bool PadRight
            {
                get
                {
                    return this.padRight;
                }
                set
                {
                    if (value)
                    {
                        this.UpdateMinMax();
                    }
                    this.padRight = value;
                }
            }

            public string TrailingSeparator { get; set; }

            public bool Truncate
            {
                get
                {
                    return this.truncate;
                }
                set
                {
                    if (value)
                    {
                        this.UpdateMinMax();
                    }
                    this.truncate = value;
                }
            }
        }
    }
}
