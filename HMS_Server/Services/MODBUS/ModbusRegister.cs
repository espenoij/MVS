namespace HMS_Server
{
    class ModbusRegister
    {
        public const int RegisterSize = 9999;

        public ModbusRegisterAddressBool[] coil = new ModbusRegisterAddressBool[RegisterSize];
        public ModbusRegisterAddressBool[] discrete = new ModbusRegisterAddressBool[RegisterSize];
        public ModbusRegisterAddressUshort[] input = new ModbusRegisterAddressUshort[RegisterSize];
        public ModbusRegisterAddressUshort[] holding = new ModbusRegisterAddressUshort[RegisterSize];

        public ModbusRegister()
        {
            for (int i = 0; i < RegisterSize; i++)
            {
                coil[i] = new ModbusRegisterAddressBool();
                discrete[i] = new ModbusRegisterAddressBool();
                input[i] = new ModbusRegisterAddressUshort();
                holding[i] = new ModbusRegisterAddressUshort();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < RegisterSize; i++)
            {
                coil[i].IsSet = false;
                discrete[i].IsSet = false;
                input[i].IsSet = false;
                holding[i].IsSet = false;
            }
        }
    }

    class ModbusRegisterAddressBool
    {
        public ModbusRegisterAddressBool()
        {
            data = false;
            IsSet = false;
        }

        public bool data { get; set; }
        public bool IsSet { get; set; }
    }

    class ModbusRegisterAddressUshort
    {
        public ModbusRegisterAddressUshort()
        {
            data = 0;
            IsSet = false;
        }

        public ushort data { get; set; }
        public bool IsSet { get; set; }
    }
}
