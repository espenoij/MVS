using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace SensorMonitor
{
    public class SerialPortSetup
    {
        public SerialPortSetup()
        {
            // Default verdier
            portName = "COM1";
            baudRate = 9600;
            dataBits = 8;
            stopBits = StopBits.One;
            parity = Parity.None;
            handshake = Handshake.None;

            packetHeader = string.Empty;
            packetEnd = "\\r\\n";
            packetDelimiter = string.Empty;
            packetCombineFields = SerialPortSetup.CombineFields.None;

            fixedPosData = false;
            fixedPosStart = 0;
            fixedPosTotal = 0;

            dataField = string.Empty;
            decimalSeparator = DecimalSeparator.Point;
            autoExtractValue = false;

            calculationSetup = new List<CalculationSetup>();
            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                calculationSetup.Add(new CalculationSetup());
        }

        public SerialPortSetup(SerialPortSetup sps)
        {
            // Default verdier
            portName = sps.portName;
            baudRate = sps.baudRate;
            dataBits = sps.dataBits;
            stopBits = sps.stopBits;
            parity = sps.parity;
            handshake = sps.handshake;

            packetHeader = sps.packetHeader;
            packetEnd = sps.packetEnd;
            packetDelimiter = sps.packetDelimiter;
            packetCombineFields = sps.packetCombineFields;

            fixedPosData = sps.fixedPosData;
            fixedPosStart = sps.fixedPosStart;
            fixedPosTotal = sps.fixedPosTotal;

            dataField = sps.dataField;
            decimalSeparator = sps.decimalSeparator;
            autoExtractValue = sps.autoExtractValue;

            calculationSetup = new List<CalculationSetup>();
            foreach (var item in sps.calculationSetup)
                calculationSetup.Add(new CalculationSetup(item));
        }

        // Port Setup
        public string portName { get; set; }
        public int baudRate { get; set; }
        public int dataBits { get; set; }
        public StopBits stopBits { get; set; }
        public Parity parity { get; set; }
        public Handshake handshake { get; set; }

        // Data Extraction Setup
        public string packetHeader { get; set; }
        public string packetEnd { get; set; }
        public string packetDelimiter { get; set; }
        public CombineFields packetCombineFields { get; set; }
        public bool fixedPosData { get; set; }
        public int fixedPosStart { get; set; }
        public int fixedPosTotal { get; set; }
        public string dataField { get; set; }
        public DecimalSeparator decimalSeparator { get; set; }
        public bool autoExtractValue { get; set; }

        // Data Calculations Setup
        public List<CalculationSetup> calculationSetup { get; set; }

        public enum CombineFields
        {
            None,
            Even,
            Odd
        }
    }
}
