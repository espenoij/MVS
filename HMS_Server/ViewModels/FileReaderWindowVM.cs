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

        public FileReaderWindowVM()
        {
            _fileFolder = string.Empty;
            _fileName = string.Empty;
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
                return string.Format($"{0}/{1}", fileFolder, fileName);
            }
        }


        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
