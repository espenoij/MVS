using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    class ModbusHelper
    {
        public bool ValidAddressSpace(int address)
        {
            if ((address >= Constants.ModbusCoilMin && address <= Constants.ModbusCoilMax) ||
                (address >= Constants.ModbusDiscreteInputMin && address <= Constants.ModbusDiscreteInputMax) ||
                (address >= Constants.ModbusInputRegisterMin && address <= Constants.ModbusInputRegisterMax) ||
                (address >= Constants.ModbusHoldingRegisterMin && address <= Constants.ModbusHoldingRegisterMax))
                return true;
            else
                return false;
        }

        public ushort AddressToOffset(int address)
        {
            if (address >= Constants.ModbusCoilMin && address <= Constants.ModbusCoilMax)
                return (ushort)(address - Constants.ModbusCoilMin);
            else
            if (address >= Constants.ModbusDiscreteInputMin && address <= Constants.ModbusDiscreteInputMax)
                return (ushort)(address - Constants.ModbusDiscreteInputMin);
            else
            if (address >= Constants.ModbusInputRegisterMin && address <= Constants.ModbusInputRegisterMax)
                return (ushort)(address - Constants.ModbusInputRegisterMin);
            else
            if (address >= Constants.ModbusHoldingRegisterMin && address <= Constants.ModbusHoldingRegisterMax)
                return (ushort)(address - Constants.ModbusHoldingRegisterMin);
            else
                return 0;
        }

        public ModbusObjectType AddressToObjectType(int address)
        {
            if (address >= Constants.ModbusCoilMin && address <= Constants.ModbusCoilMax)
                return ModbusObjectType.Coil;
            else
            if (address >= Constants.ModbusDiscreteInputMin && address <= Constants.ModbusDiscreteInputMax)
                return ModbusObjectType.DiscreteInput;
            else
            if (address >= Constants.ModbusInputRegisterMin && address <= Constants.ModbusInputRegisterMax)
                return ModbusObjectType.InputRegister;
            else
            if (address >= Constants.ModbusHoldingRegisterMin && address <= Constants.ModbusHoldingRegisterMax)
                return ModbusObjectType.HoldingRegister;
            else
                return ModbusObjectType.None;
        }
    }
}
