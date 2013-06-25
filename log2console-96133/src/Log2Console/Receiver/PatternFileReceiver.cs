using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

using Log2Console.Log;
using Log2Console.Parser;
using Log2Console.Configuration;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;

namespace Log2Console.Receiver
{
    /// <summary>
    /// This receiver watch a given file, like a 'tail' program, with one log event by line.
    /// Ideally the log events should use the Pattern layout.
    /// </summary>
    [Serializable]
    [DisplayName("Log File (Flat Pattern Layout Formatted)")]
    public class PatternFileReceiver : BaseReceiver
    {
        public enum FileFormatEnums
        {
            Flat,
        }


        [NonSerialized]
        private FileSystemWatcher _fileWatcher;
        [NonSerialized]
        private long _lastFileLength;
        [NonSerialized]
        private string _filename;
        [NonSerialized]
        private string _fullLoggerName;

        private string _fileToWatch = String.Empty;

        private FileFormatEnums _fileFormat;
        private bool _showFromBeginning = true;
        private string _loggerName;
        private string _conversionPattern = String.Empty;

        [Category("Configuration")]
        [DisplayName("File to Watch")]
        [EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string FileToWatch
        {
            get { return _fileToWatch; }
            set
            {
                if (String.Compare(_fileToWatch, value, true) == 0)
                    return;

                _fileToWatch = value;
                Restart();
            }
        }

        [Category("Configuration")]
        [DisplayName("Conversion Pattern")]
        [Description("Provide Pattern Layout which must be an exact match to the log file being viewed.")]
        public string ConversionPattern
        {
            get { return _conversionPattern; }
            set { _conversionPattern = value; }
        }

        [Category("Configuration")]
        [DisplayName("File Format (Flat)")]
        public FileFormatEnums FileFormat
        {
            get { return _fileFormat; }
            set { _fileFormat = value; }
        }

        [Category("Configuration")]
        [DisplayName("Show from Beginning")]
        [Description("Show file contents from the beginning (not just newly appended lines)")]
        [DefaultValue(true)]
        public bool ShowFromBeginning
        {
            get { return _showFromBeginning; }
            set
            {
                _showFromBeginning = value;

                if (value && _lastFileLength == 0)
                {
                    ReadFile();
                }
            }
        }

        [Category("Behavior")]
        [DisplayName("Logger Name")]
        [Description("Append the given Name to the Logger Name. If left empty, the filename will be used.")]
        public string LoggerName
        {
            get { return _loggerName; }
            set
            {
                _loggerName = value;

                ComputeFullLoggerName();
            }
        }


        #region IReceiver Members

        [Browsable(false)]
        public override string SampleClientConfig
        {
            get
            {
                return
                    "Configuration for log4net:" + Environment.NewLine +
                    "<appender name=\"FileAppender\" type=\"log4net.Appender.FileAppender\">" + Environment.NewLine +
                    "    <file value=\"log-file.txt\" />" + Environment.NewLine +
                    "    <appendToFile value=\"true\" />" + Environment.NewLine +
                    "    <lockingModel type=\"log4net.Appender.FileAppender+MinimalLock\" />" + Environment.NewLine +
                    "    <layout type=\"log4net.Layout.PatternLayout\" >" + Environment.NewLine +
                    "    <conversionPattern value=\"%message [%thread] %logger %level - %date%newline\" />" + Environment.NewLine +
                    "    </layout>" + Environment.NewLine +
                    "</appender>";
            }
        }

        public override void Initialize()
        {
            if (String.IsNullOrEmpty(_fileToWatch) || String.IsNullOrEmpty(_conversionPattern))
                return;
            if (File.Exists(_fileToWatch))
            {
                StreamReader fileReader;
                using (fileReader = new StreamReader(new FileStream(_fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    _lastFileLength = _showFromBeginning ? 0 : fileReader.BaseStream.Length;
                    ConversionPattern = _conversionPattern;
                }
            }
            string path = Path.GetDirectoryName(_fileToWatch);
            _filename = Path.GetFileName(_fileToWatch);
            _fileWatcher = new FileSystemWatcher(path, _filename);
            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.EnableRaisingEvents = true;

            ComputeFullLoggerName();
        }

        public override void Terminate()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Changed -= OnFileChanged;
                _fileWatcher = null;
            }

            _lastFileLength = 0;
        }

        public override void Attach(ILogMessageNotifiable notifiable)
        {
            base.Attach(notifiable);

            if (_showFromBeginning)
                ReadFile();
        }

        #endregion


        private void Restart()
        {
            Terminate();
            Initialize();
        }

        private void ComputeFullLoggerName()
        {
            _fullLoggerName = String.Format("FileLogger.{0}",
                                            String.IsNullOrEmpty(_loggerName)
                                                ? _filename.Replace('.', '_')
                                                : _loggerName);

            DisplayName = String.IsNullOrEmpty(_loggerName)
                              ? String.Empty
                              : String.Format("Log File [{0}]", _loggerName);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            ReadFile();
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
                return;

            Restart();
        }

        private void OnFileRenamed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Renamed)
                return;

            Restart();
        }

        private void ReadFile()
        {
            if (!File.Exists(_fileToWatch))
            {
                return;
            }

            //Factory pattern to decide which class object need to invoke for file parsing
            ParserFactory parseFact = new ParserFactory();
            IParser parser = parseFact.GetParser(ParserFactory.ParserFileEnums.Flat);

            StreamReader fileReader;
            using (fileReader = new StreamReader(new FileStream(_fileToWatch, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (fileReader.BaseStream.Length == _lastFileLength)
                    return;

                if (fileReader.BaseStream.Length < _lastFileLength)
                {
                    //Log file has been trucated/deleted/renamed
                    _lastFileLength = 0;
                }

                // Seek to the last file length
                fileReader.BaseStream.Seek(_lastFileLength, SeekOrigin.Begin);

                //Now decide which all parameters we need to pass to parse function
                //ConfigurationFactory configFact=new ConfigurationFactory();
                //IConfiguration config = configFact.GetConfiguration(ConfigurationFactory.ConfigurationFileEnums.FlatFile);
                ConfigurationSettings settings = new ConfigurationSettings();
                settings.ConversionPattern = ConversionPattern;
                settings.FileReader = fileReader;
                Log2Console.Configuration.Configuration config = new Log2Console.Configuration.Configuration();
                config.Configurations = settings;

                //At runtime call
                List<LogMessage> logMsgs = parser.Parse(config);//To parse flat file

                //Notify the UI with the set of messages
                Notifiable.Notify(logMsgs.ToArray());

                // Update the last file length
                _lastFileLength = fileReader.BaseStream.Position;
            }
        }
    }
}
