using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitor
{
    class Constants
    {
        public const int PacketDataFields = 100;        // Korresponderer til felt i XAML fil (lvPacketDataGridView)
        public const long DataTimerInterval = 250;      // Millisekund
    }

    static public class INIFILE
    {
        // File location
        public const string Location = "Settings.ini";

        // Section
        public const string SerialPortConfigurationSection = "Serial Port Configuration";

        // Serial Port Configuration
        public const string PortName = "Port Name";
        public const string BaudRate = "Baud Rate";
        public const string DataBits = "Data Bits";
        public const string StopBits = "Stop Bits";
        public const string DataParity = "Data Parity";
        public const string HandShake = "Hand Shake";
        public const string RawDataAutoScroll = "Raw Data Auto Scroll";

        // Packet Selection
        public const string SelectedPacketsStart = "Selected Packets Start";
        public const string SelectedPacketsEnd = "Selected Packets End";
        public const string SelectedPacketsAutoScroll = "Selected Packets Auto Scroll";

        // Packet Data
        public const string PacketDataDelimiter = "Packet Data Delimiter";
        public const string PacketDataAutoScroll = "Packet Data Auto Scroll";

        // Data Selection
        public const string SelectedDataField = "Selected Data Field";
        public const string SelectedDataAutoExtraction = "Selected Data Auto Extraction";
        public const string SelectedDataAutoScroll = "Selected Data Auto Scroll";

        // Data Processing
        public const string ProcessingType = "Processing Type";
        public const string ProcessingParameter = "Processing Parameter";
        public const string ProcessingAutoScroll = "Processing Auto Scroll";

        // Common Controls
        public const string ShowControlChars = "Show Control Chars";
    }
}
