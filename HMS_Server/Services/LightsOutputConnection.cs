using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    public class LightsOutputConnection
    {
        public SensorData data;

        private Config config;

        public LightsOutputConnection(Config config)
        {
            this.config = config;

            // Hente lights output data fra fil
            data = new SensorData(config.GetLightsOutputData());

            string typeString = config.ReadWithDefault(ConfigKey.LightsOutputType, OutputConnectionType.MODBUS_RTU.GetDescription());
            type = EnumExtension.GetEnumValueFromDescription<OutputConnectionType>(typeString);

        }

        public OutputConnectionType type { get; set; }

        public void SetType(OutputConnectionType type)
        {
            this.type = type;

            // Lagre til fil
            config.Write(ConfigKey.LightsOutputType, type.GetDescription());

            if (data != null)
            {
                // Finne gamle connection data
                string portNameOld;
                int baudRateOld;
                int dataBitsOld;
                StopBits stopBitsOld;
                Parity parityOld;
                Handshake handshakeOld;
                string adamAddressOld = "0";

                switch (data.type)
                {
                    case SensorType.SerialPort:
                        portNameOld = data.serialPort.portName;
                        baudRateOld = data.serialPort.baudRate;
                        dataBitsOld = data.serialPort.dataBits;
                        stopBitsOld = data.serialPort.stopBits;
                        parityOld = data.serialPort.parity;
                        handshakeOld = data.serialPort.handshake;

                        adamAddressOld = data.serialPort.packetHeader;
                        break;

                    case SensorType.ModbusRTU:
                        portNameOld = data.modbus.portName;
                        baudRateOld = data.modbus.baudRate;
                        dataBitsOld = data.modbus.dataBits;
                        stopBitsOld = data.modbus.stopBits;
                        parityOld = data.modbus.parity;
                        handshakeOld = data.modbus.handshake;
                        break;

                    default:
                        portNameOld = "COM1";
                        baudRateOld = 9600;
                        dataBitsOld = 8;
                        stopBitsOld = StopBits.One;
                        parityOld = Parity.None;
                        handshakeOld = Handshake.None;
                        adamAddressOld = "0";
                        break;
                }

                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        // Sette ny type
                        data.type = SensorType.ModbusRTU;

                        // Sette tilbake gamle connection data
                        data.modbus.portName = portNameOld;
                        data.modbus.baudRate = baudRateOld;
                        data.modbus.dataBits = dataBitsOld;
                        data.modbus.stopBits = stopBitsOld;
                        data.modbus.parity = parityOld;
                        data.modbus.handshake = handshakeOld;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        // Sette ny type
                        data.type = SensorType.SerialPort;

                        // Sette tilbake gamle connection data
                        data.serialPort.portName = portNameOld;
                        data.serialPort.baudRate = baudRateOld;
                        data.serialPort.dataBits = dataBitsOld;
                        data.serialPort.stopBits = stopBitsOld;
                        data.serialPort.parity = parityOld;
                        data.serialPort.handshake = handshakeOld;

                        data.serialPort.packetHeader = adamAddressOld;
                        break;
                }

                // Skrive til fil
                config.SetLightsOutputData(data);
            }
        }

        public string portName
        {
            get
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        return data.modbus?.portName;

                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort?.portName;

                    default:
                        return string.Empty;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        if (data.modbus != null)
                            data.modbus.portName = value;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.portName = value;
                        break;

                    default:
                        break;
                }
            }
        }

        public int baudRate
        {
            get
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        return data.modbus.baudRate;

                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort.baudRate;

                    default:
                        return 9600;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        if (data.modbus != null)
                            data.modbus.baudRate = value;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.baudRate = value;
                        break;

                    default:
                        break;
                }
            }
        }

        public int dataBits
        {
            get
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        return data.modbus.dataBits;

                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort.dataBits;

                    default:
                        return 8;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        if (data.modbus != null)
                            data.modbus.dataBits = value;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.dataBits = value;
                        break;

                    default:
                        break;
                }
            }
        }

        public StopBits stopBits
        {
            get
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        return data.modbus.stopBits;

                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort.stopBits;

                    default:
                        return StopBits.One;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        if (data.modbus != null)
                            data.modbus.stopBits = value;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.stopBits = value;
                        break;

                    default:
                        break;
                }
            }
        }

        public Parity parity
        {
            get
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        return data.modbus.parity;

                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort.parity;

                    default:
                        return Parity.None;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        if (data.modbus != null)
                            data.modbus.parity = value;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.parity = value;
                        break;

                    default:
                        break;
                }
            }
        }

        public Handshake handshake
        {
            get
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        return data.modbus.handshake;

                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort.handshake;

                    default:
                        return Handshake.None;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.MODBUS_RTU:
                        if (data.modbus != null)
                            data.modbus.handshake = value;
                        break;

                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.handshake = value;
                        break;

                    default:
                        break;
                }
            }
        }

        public string adamAddress
        {
            // Bruker packetHeader til å lagre ADAM addresse
            // Bør kanskje på sikt legges i egen dedikert variabel (som også lagres i config)
            get
            {
                switch (type)
                {
                    case OutputConnectionType.ADAM_4060:
                        return data.serialPort.packetHeader;

                    default:
                        return string.Empty;
                }
            }
            set
            {
                switch (type)
                {
                    case OutputConnectionType.ADAM_4060:
                        if (data.serialPort != null)
                            data.serialPort.packetHeader = value;
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
