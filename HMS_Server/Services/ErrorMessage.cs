using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class ErrorMessage : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // ID kan unnlates dersom feil ikke er knyttet til en spesifikk sensor verdi.
        // rowID brukes kun ifm henting av data fra databasen.
        public ErrorMessage(DateTime timestamp, ErrorMessageType type, ErrorMessageCategory category, string message, int id = -1, int rowId = 0)
        {
            this.timestamp = timestamp;
            this.type = type;
            this.category = category;
            this.message = message;
            this.id = id;
            this.rowId = rowId;
        }

        public ErrorMessage(ErrorMessage errorMessage)
        {
            this.timestamp = errorMessage.timestamp;
            this.type = errorMessage.type;
            this.category = errorMessage.category;
            this.message = errorMessage.message;
            this.id = errorMessage.id;
            this.rowId = errorMessage.rowId;
        }

        private DateTime _timestamp { get; set; }
        public DateTime timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(timestampString));
            }
        }
        public string timestampString
        {
            get
            {
                if (timestamp.Ticks != 0)
                    return timestamp.ToString(Constants.TimestampFormat, Constants.cultureInfo);
                else
                    return Constants.TimestampNotSet;
            }
        }

        private ErrorMessageType _type { get; set; }
        public ErrorMessageType type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(typeString));
            }
        }
        public string typeString
        {
            get
            {
                return type.ToString();
            }
        }

        private ErrorMessageCategory _category { get; set; }
        public ErrorMessageCategory category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
                OnPropertyChanged();
            }
        }

        private string _message { get; set; }
        public string message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(messageSingleLine));
            }
        }
        public string messageSingleLine
        {
            get
            {
                return TextHelper.RemoveNewLine(message);
            }
        }

        private int _id { get; set; }
        public int id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(idString));
            }
        }
        public string idString
        {
            get
            {
                if (id == -1)
                    return string.Empty;
                else
                    return id.ToString();
            }
        }

        private int _rowId { get; set; }
        public int rowId
        {
            get
            {
                return _rowId;
            }
            set
            {
                _rowId = value;
                OnPropertyChanged();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum ErrorMessageType
    {
        All,
        SerialPort,
        MODBUS,
        Database,
        FileReader,
        Config
    }

    public enum ErrorMessageCategory
    {
        None,           // Ingen melding vises på skjermen
        Admin,          // For feilmeldinger i operatøre delen av grensesnittet. Melding vises på skjerm når admin modus er aktivt
        AdminUser,      // For feilmeldinger i bruker-delen av grensesnittet. Melding vises på skjerm når admin modus er aktivt, og melding legges i bruker-feilmeldingsliste
        User            // Feilmelding som bruker må få melding om

        // NB! Alle meldinger lagres i databasen i bakgrunnen uansett kategori
    }
}
