using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace HMS_Server
{
    public class ModbusSetup
    {
        public ModbusSetup()
        {
            // Default verdier
            portName = "COM1";
            baudRate = 9600;
            dataBits = 8;
            stopBits = StopBits.One;
            parity = Parity.None;
            handshake = Handshake.None;

            tcpAddress = "127.0.0.1";
            tcpPort = 502;

            slaveID = 1;
            startAddress = Constants.ModbusInputRegisterMin;
            totalAddresses = 10;
            dataAddress = Constants.ModbusInputRegisterMin;

            calculationSetup = new List<CalculationSetup>();
            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                calculationSetup.Add(new CalculationSetup());
        }

        public ModbusSetup(ModbusSetup modbusSetup)
        {
            // Default verdier
            portName = modbusSetup.portName;
            baudRate = modbusSetup.baudRate;
            dataBits = modbusSetup.dataBits;
            stopBits = modbusSetup.stopBits;
            parity = modbusSetup.parity;
            handshake = modbusSetup.handshake;

            tcpAddress = modbusSetup.tcpAddress;
            tcpPort = modbusSetup.tcpPort;

            slaveID = modbusSetup.slaveID;
            startAddress = modbusSetup.startAddress;
            totalAddresses = modbusSetup.totalAddresses;
            dataAddress = modbusSetup.dataAddress;

            calculationSetup = new List<CalculationSetup>();
            foreach (var item in modbusSetup.calculationSetup)
                calculationSetup.Add(new CalculationSetup(item));
        }

        // Serie Port Setup
        public string portName { get; set; }
        public int baudRate { get; set; }
        public int dataBits { get; set; }
        public StopBits stopBits { get; set; }
        public Parity parity { get; set; }
        public Handshake handshake { get; set; }

        // TCP Setup
        public string tcpAddress { get; set; }
        public int tcpPort { get; set; }

        // Slave
        public byte slaveID { get; set; }

        // Data Sample
        public int startAddress { get; set; }
        public int totalAddresses { get; set; }

        // Data Extraction Setup
        public int dataAddress { get; set; }

        // Data Calculations Setup
        public List<CalculationSetup> calculationSetup { get; set; }
    }
}
