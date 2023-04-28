using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    class FixedValueWindowVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private Config config;

        public FixedValueWindowVM(Config config)
        {
            this.config = config;

            totalDataLinesString = config.ReadWithDefault(ConfigKey.TotalDataLines, ConfigSection.FileReaderConfig, Constants.GUIDataLinesDefault).ToString();
        }

        // Antal linjer som skal vises på skjerm
        /////////////////////////////////////////////////////////////////////////////
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
                config.Write(ConfigKey.TotalDataLines, totalDataLinesString, ConfigSection.FileReaderConfig);

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

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
