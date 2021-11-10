using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SensorMonitor
{
    class ModbusSetupWindowVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Config config;

        public ModbusSetupWindowVM(Config config)
        {
            this.config = config;
        }

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
                config.Write(ConfigKey.TotalDataLines, totalDataLinesString, ConfigSection.SerialPortConfig);

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
    }
}
