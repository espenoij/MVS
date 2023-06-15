using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    class FileReaderWindowVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private Config config;

        public FileReaderWindowVM(Config config)
        {
            this.config = config;

            fileFolder = string.Empty;
            fileName = string.Empty;
            readFrequency = 0;

            totalDataLinesString = config.ReadWithDefault(ConfigKey.TotalDataLines, ConfigSection.FileReaderConfig, Constants.GUIDataLinesDefault).ToString();
        }

        // Katalog lokasjon
        private string _fileFolder { get; set; }
        public string fileFolder
        {
            get
            {
                return _fileFolder;
            }
            set
            {
                _fileFolder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(filePath));
            }
        }

        // Filnavn
        public string _fileName { get; set; }
        public string fileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                _fileName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(filePath));
            }
        }

        // Path
        public string filePath
        {
            get
            {
                return string.Format($"{0}/{1}", _fileFolder, _fileName);
            }
        }

        // Antal linjer som skal vises på skjerm
        private string _totalDataLinesString { get; set; }
        public string totalDataLinesString
        {
            get
            {
                return _totalDataLinesString;
            }
            set
            {
                // Verifisere inndata
                DataValidation.Double(
                    value,
                    Constants.GUIDataLinesMin,
                    Constants.GUIDataLinesMax,
                    Constants.GUIDataLinesDefault,
                    out double validatedInput);

                _totalDataLinesString = validatedInput.ToString();

                // Lagre ny setting til config fil
                config.Write(ConfigKey.TotalDataLines, _totalDataLinesString, ConfigSection.FileReaderConfig);

                OnPropertyChanged();
                OnPropertyChanged(nameof(totalDataLines));
            }
        }

        public int totalDataLines
        {
            get
            {
                if (int.TryParse(_totalDataLinesString, out int result))
                    return result;
                else
                    return Constants.GUIDataLinesDefault;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timing: File Read Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _readFrequency { get; set; }
        public double readFrequency
        {
            get
            {
                return _readFrequency;
            }
            set
            {
                _readFrequency = value;
                OnPropertyChanged();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
