using System;

namespace HMS_Server
{
    class ModbusCalculations
    {
        // MODBUS Helper
        ModbusHelper modbusHelper = new ModbusHelper();

        public void GetSelectedData(SensorData sensorData, ushort[] registers, ModbusData modbusData)
        {
            if (registers.Length > modbusHelper.AddressToOffset(sensorData.modbus.dataAddress))
                modbusData.data = Convert.ToInt32(registers[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)]);
            else
                modbusData.data = 0;
        }

        public void GetSelectedData(SensorData sensorData, bool[] registers, ModbusData modbusData)
        {
            if (registers.Length > modbusHelper.AddressToOffset(sensorData.modbus.dataAddress))
                modbusData.data = Convert.ToInt32(registers[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)]);
            else
                modbusData.data = 0;
        }

        public void ApplyCalculationsToSelectedData(SensorData sensorData, DateTime timestamp, ModbusData modbusData, ErrorHandler errorHandler, ErrorMessageCategory errorMessageCat, AdminSettingsVM adminSettingsVM)
        {
            modbusData.calculatedData = modbusData.data;

            // Data Calculations 1
            ////////////////////////////
            // Skal vi utføre kalkulasjoner?
            if (sensorData.dataCalculations[0].type != CalculationType.None)
            {
                // Utføre valgt prosessering
                modbusData.calculatedData = sensorData.dataCalculations[0].DoCalculations(modbusData.calculatedData.ToString(), timestamp, errorHandler, errorMessageCat, adminSettingsVM);
            }

            // Data Calculations 2
            ////////////////////////////
            // Skal vi utføre kalkulasjoner?
            if (sensorData.dataCalculations[1].type != CalculationType.None)
            {
                // Utføre valgt prosessering
                modbusData.calculatedData = sensorData.dataCalculations[1].DoCalculations(modbusData.calculatedData.ToString(), timestamp, errorHandler, errorMessageCat, adminSettingsVM);
            }

            // Data Calculations 3
            ////////////////////////////
            // Skal vi utføre kalkulasjoner?
            if (sensorData.dataCalculations[2].type != CalculationType.None)
            {
                // Utføre valgt prosessering
                modbusData.calculatedData = sensorData.dataCalculations[2].DoCalculations(modbusData.calculatedData.ToString(), timestamp, errorHandler, errorMessageCat, adminSettingsVM);
            }

            // Data Calculations 4
            ////////////////////////////
            // Skal vi utføre kalkulasjoner?
            if (sensorData.dataCalculations[3].type != CalculationType.None)
            {
                // Utføre valgt prosessering
                modbusData.calculatedData = sensorData.dataCalculations[3].DoCalculations(modbusData.calculatedData.ToString(), timestamp, errorHandler, errorMessageCat, adminSettingsVM);
            }
        }
    }
}
