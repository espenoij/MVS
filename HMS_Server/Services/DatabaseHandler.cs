using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class DatabaseHandler
    {
        // Database connection parametre
        private string connectionString;

        // Database tabeller/kolonner
        // NB! Dersom HMS database tabellen endres er tanken at vi bare oppdaterer
        // tabell navnet med nytt versjonsnummer slik at den gamle tabellen blir liggende
        // selv om ny versjon av programmvaren med endre tabell installeres. Slik bevares
        // lagrede data (myndighetskrav). Disse kan slettes manuellt når de er gått ut på data (6mnd).
        private const string tableNamePrefixHMSData = "hms_data_v1";
        private const string tableNamePrefixSensorData = "sensor_data";
        private const string tableNamePrefixSensorStatus = "sensor_status";
        private const string columnTimestamp = "timestamp";
        private const string columnData = "data";
        private const string columnID = "id";
        private const string columnName = "name";
        private const string columnActive = "active";
        private const string columnStatus = "status";

        // Error Messages
        private const string tableNameErrorMessages = "error_messages";
        private const string columnId = "sensor_data_id";
        private const string columnType = "type";
        private const string columnMessage = "message";

        // Verification Data
        private const string veriDataTableName = "veri_data";
        private const string veriDataColumnTimeID = "time_id";
        private const string veriDataColumnRollR = "roll_r";
        private const string veriDataColumnRollL = "roll_l";
        private const string veriDataColumnPitchU = "pitch_u";
        private const string veriDataColumnPitchD = "pitch_d";
        private const string veriDataColumnInclinationMax = "incl_max";
        private const string veriDataColumnSHR = "shr";
        private const string veriDataColumnMSI = "msi";
        private const string veriDataColumnWSI = "wsi";
        private const string veriDataColumnVesselDir = "vessel_dir";

        private const string veriDataColumnHelideckHeading = "helideck_hdg";
        private const string veriDataColumnHelicopterHeading = "helicopter_hdg";
        private const string veriDataColumnDeltaVesselHeading = "delta_vessel_hdg";
        private const string veriDataColumnDeltaWindHeading = "delta_wind_hdg";
        private const string veriDataColumnRWD = "rwd";
        private const string veriDataColumnHelideckStatus = "hll_status";

        private const string veriDataColumnWindSpd2m = "wind_spd_2m";
        private const string veriDataColumnWindDir2m = "wind_dir_2m";
        private const string veriDataColumnWindGust2m = "wind_gust_2m";
        private const string veriDataColumnWindSpd10m = "wind_spd_10m";
        private const string veriDataColumnWindDir10m = "wind_dir_10m";
        private const string veriDataColumnWindGust10m = "wind_gust_10m";
        private const string veriDataColumnSensorMRU = "sensor_mru";
        private const string veriDataColumnSensorGyro = "sensor_gyro";
        private const string veriDataColumnSensorWind = "sensor_wind";

        // Får vi opprettet OK forbindelse?
        private bool isDatabaseConnectionOK;

        // Configuration
        private Config config;

        public DatabaseHandler(bool isDatabaseConnectionOK = false)
        {
            config = new Config();

            // Generate encrypted user ID
            //config.Write(ConfigKey.DatabaseUserID, Encryption.EncryptString(Encryption.ToSecureString("root")));

            // Generate encrypted password
            //config.Write(ConfigKey.DatabasePassword, Encryption.EncryptString(Encryption.ToSecureString("test99")));

            string address = config.ReadWithDefault(ConfigKey.DatabaseAddress, Constants.DefaultDatabaseAddress);
            string port = config.ReadWithDefault(ConfigKey.DatabasePort, Constants.DefaultDatabasePort).ToString();
            string database = config.Read(ConfigKey.DatabaseName);

            // Database login
            SecureString userid = Encryption.DecryptString(config.Read(ConfigKey.DatabaseUserID));
            SecureString password = Encryption.DecryptString(config.Read(ConfigKey.DatabasePassword));

            connectionString = string.Format(@"server={0};port={1};userid={2};password={3};database={4};sslmode=none",
                address,
                port,
                Encryption.ToInsecureString(userid),
                Encryption.ToInsecureString(password),
                database);

            this.isDatabaseConnectionOK = isDatabaseConnectionOK;
        }

        public void CreateTables(RadObservableCollection<SensorData> sensorDataList)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Opprette tabeller for sensor data
                    //////////////////////////////////////////////////////////
                    lock (sensorDataList)
                    {
                        // For hver sensor verdi som skal lagres oppretter vi en ny database tabell, dersom den ikke allerede eksisterer
                        foreach (var sensorData in sensorDataList)
                        {
                            if (sensorData.saveToDatabase)
                            {
                                string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                                // Opprette nytt database table
                                cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3), {2} DOUBLE)", tableName, columnTimestamp, columnData);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Opprette error messages tabell
                    //////////////////////////////////////////////////////////
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3), {2} INTEGER, {3} TEXT, {4} TEXT)", tableNameErrorMessages, columnTimestamp, columnId, columnType, columnMessage);
                    cmd.ExecuteNonQuery();

                    // Opprette verificatio data tabell
                    //////////////////////////////////////////////////////////
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TEXT, {2} TEXT, {3} TEXT, {4} TEXT, {5} TEXT, {6} TEXT, {7} TEXT, {8} TEXT, {9} TEXT, {10} TEXT, {11} TEXT, {12} TEXT, {13} TEXT, {14} TEXT, {15} TEXT, {16} TEXT, {17} TEXT, {18} TEXT, {19} TEXT, {20} TEXT, {21} TEXT, {22} TEXT, {23} TEXT, {24} TEXT, {25} TEXT)", 
                        // Tabell    
                        veriDataTableName,

                        // Kolonner
                        veriDataColumnTimeID,
                        veriDataColumnRollR,
                        veriDataColumnRollL,
                        veriDataColumnPitchU,
                        veriDataColumnPitchD,
                        veriDataColumnInclinationMax,
                        veriDataColumnSHR,
                        veriDataColumnMSI,
                        veriDataColumnWSI,
                        veriDataColumnVesselDir,
                        veriDataColumnHelideckHeading,
                        veriDataColumnHelicopterHeading,
                        veriDataColumnDeltaVesselHeading,
                        veriDataColumnDeltaWindHeading,
                        veriDataColumnRWD,
                        veriDataColumnHelideckStatus,
                        veriDataColumnWindSpd2m,
                        veriDataColumnWindDir2m,
                        veriDataColumnWindGust2m,
                        veriDataColumnWindSpd10m,
                        veriDataColumnWindDir10m,
                        veriDataColumnWindGust10m,
                        veriDataColumnSensorMRU,
                        veriDataColumnSensorGyro,
                        veriDataColumnSensorWind);
                    cmd.ExecuteNonQuery();

                    connection.Close();

                    isDatabaseConnectionOK = true;
                }
            }
            catch (Exception)
            {
                isDatabaseConnectionOK = false;

                // Sendes videre oppover fordi vi ikke kan lagre feilmeldinger herfra
                throw;
            }
        }

        //public void CreateTables(RadObservableCollectionEx<HMSData> hmsInputDataList)
        //{
        //    try
        //    {
        //        using (var connection = new MySqlConnection(connectionString))
        //        {
        //            var cmd = new MySqlCommand();
        //            cmd.Connection = connection;

        //            connection.Open();

        //            // Opprette HMS data tabeller
        //            //////////////////////////////////////////////////////////
        //            lock (hmsInputDataList)
        //            {
        //                // For hver HMS data verdi som skal lagres oppretter vi en ny database tabell, dersom den ikke allerede eksisterer
        //                foreach (var hmsData in hmsInputDataList)
        //                {
        //                    if (!string.IsNullOrEmpty(hmsData.dbColumn))
        //                    {
        //                        // Opprette nytt database table
        //                        cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3), {2} DOUBLE)", hmsData.dbColumn, columnTimestamp, columnData);
        //                        cmd.ExecuteNonQuery();
        //                    }
        //                    else
        //                    {

        //                    }
        //                }
        //            }

        //            connection.Close();

        //            isDatabaseConnectionOK = true;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        isDatabaseConnectionOK = false;

        //        // Sendes videre oppover fordi vi ikke kan lagre feilmeldinger herfra
        //        throw;
        //    }
        //}

        public void CreateTables(HMSDataCollection hmsDataCollection)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Liste med database column navn
                    string columnNames = "";
                    foreach (var hmsData in hmsDataCollection.GetDataList())
                    {
                        // Kun variabler som har column navn satt skal legges inn
                        if (!string.IsNullOrEmpty(hmsData.dbColumn))
                            columnNames += ", " + hmsData.dbColumn + " TEXT";
                    }

                    // Opprette nytt database table
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3){2})",
                        tableNamePrefixHMSData, 
                        columnTimestamp,
                        columnNames);
                    cmd.ExecuteNonQuery();

                    connection.Close();

                    isDatabaseConnectionOK = true;
                }
            }
            catch (Exception)
            {
                isDatabaseConnectionOK = false;

                // Sendes videre oppover fordi vi ikke kan lagre feilmeldinger herfra
                throw;
            }
        }

        public void CreateTables(RadObservableCollection<SensorGroup> sensorGroupList)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Liste med database column navn
                    string columnNames = "";
                    foreach (var sensorGroup in sensorGroupList)
                    {
                        // Kun variabler som har column navn satt skal legges inn
                        if (!string.IsNullOrEmpty(sensorGroup.dbColumn))
                            columnNames += ", " + sensorGroup.dbColumn + " INT";
                    }

                    // Opprette ny database table
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3){2})",
                        tableNamePrefixSensorStatus,
                        columnTimestamp,
                        columnNames);
                    cmd.ExecuteNonQuery();

                    connection.Close();

                    isDatabaseConnectionOK = true;
                }
            }
            catch (Exception)
            {
                isDatabaseConnectionOK = false;

                // Sendes videre oppover fordi vi ikke kan lagre feilmeldinger herfra
                throw;
            }
        }
        public void RemoveUnusedTables(RadObservableCollection<SensorData> sensorDataList)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Vi leser nextID variabelen for å finne ut hvor mange tabeller vi må sjekke
                    string result = config.Read(ConfigKey.nextID, ConfigKey.SensorSectionHeader, ConfigType.Data);

                    // Dersom vi finner nextID variabelen og der er data
                    if (result != string.Empty)
                    {
                        // Konverter til integer
                        int totalIDs = Convert.ToInt32(result);

                        lock (sensorDataList)
                        {
                            for (int i = 0; i < totalIDs; i++)
                            {
                                if (sensorDataList.Where(x => x.id == i)?.Count() == 0)
                                {
                                    string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, i);

                                    // Slette database tabell
                                    cmd.CommandText = string.Format("DROP TABLE IF EXISTS {0}", tableName);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteAllData(RadObservableCollection<SensorData> sensorDataList)
        {
            // NB! Tabellen må eksistere ellers gies feilmelding

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    lock (sensorDataList)
                    {
                        // Slå opp tabellene for hver sensor verdi og slette data
                        foreach (var sensorData in sensorDataList)
                        {
                            if (sensorData.saveToDatabase)
                            {
                                string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                                // Slette alle data i database table
                                cmd.CommandText = string.Format(@"TRUNCATE {0}", tableName);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Slette også alle feilmeldinger fra databasen
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableNameErrorMessages);
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteAllDataHMS()
        {
            // NB! Tabellen må eksistere ellers gies feilmelding

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Slå opp tabellene for hver sensor verdi og slette data
                    string tableName = string.Format("{0}", tableNamePrefixHMSData);

                    // Slette alle data i database table
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableName);
                    cmd.ExecuteNonQuery();

                    // Slette også alle feilmeldinger fra databasen
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableNameErrorMessages);
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteAllDataSensorStatus()
        {
            // NB! Tabellen må eksistere ellers gies feilmelding

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Slå opp tabellene for hver sensor verdi og slette data
                    string tableName = string.Format("{0}", tableNamePrefixSensorStatus);

                    // Slette alle data i database table
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableName);
                    cmd.ExecuteNonQuery();

                    // Slette også alle feilmeldinger fra databasen
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableNameErrorMessages);
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteAllDataVerification()
        {
            // NB! Tabellen må eksistere ellers gies feilmelding

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Slå opp tabellene for hver sensor verdi og slette data
                    string tableName = string.Format("{0}", veriDataTableName);

                    // Slette alle data i database table
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableName);
                    cmd.ExecuteNonQuery();

                    // Slette også alle feilmeldinger fra databasen
                    cmd.CommandText = string.Format(@"TRUNCATE {0}", tableNameErrorMessages);
                    cmd.ExecuteNonQuery();

                    connection.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Insert(SensorData sensorData)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // Table navn
                        string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Insert kommando
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}) VALUES(@1, @2)", tableName, columnTimestamp, columnData);

                        // Insert parametre
                        cmd.Parameters.AddWithValue("@1", sensorData.timestamp);
                        cmd.Parameters.AddWithValue("@2", sensorData.data);

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        cmd.ExecuteNonQuery();

                        // Lukke database connection
                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //public void Insert(HMSData hmsData)
        //{
        //    try
        //    {
        //        if (isDatabaseConnectionOK)
        //        {
        //            if (hmsData.timestamp != DateTime.MinValue &&
        //                !double.IsNaN(hmsData.data) &&
        //                !string.IsNullOrEmpty(hmsData.dbColumnName))
        //            {
        //                using (var connection = new MySqlConnection(connectionString))
        //                {
        //                    // SQL kommando
        //                    var cmd = new MySqlCommand();
        //                    cmd.Connection = connection;

        //                    // Insert kommando
        //                    cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}) VALUES(@1, @2)", hmsData.dbColumnName, columnTimestamp, columnData);

        //                    // Insert parametre
        //                    cmd.Parameters.AddWithValue("@1", hmsData.timestamp);
        //                    cmd.Parameters.AddWithValue("@2", hmsData.data);

        //                    // Åpne database connection
        //                    connection.Open();

        //                    // Insert execute
        //                    cmd.ExecuteNonQuery();

        //                    // Lukke database connection
        //                    connection.Close();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public void Insert(RadObservableCollection<SensorGroup> sensorGroupList)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Liste med database column navn
                        string columnNames = "";
                        bool first = true;
                        foreach (var sensorGroup in sensorGroupList)
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(sensorGroup.dbColumn))
                            {
                                if (first)
                                    columnNames += sensorGroup.dbColumn;
                                else
                                    columnNames += ", " + sensorGroup.dbColumn;

                                first = false;
                            }
                        }

                        // Value parametre
                        string valueNumbers = "";
                        first = true;
                        int i = 1;
                        foreach (var sensorGroup in sensorGroupList)
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(sensorGroup.dbColumn))
                            {
                                if (first)
                                    valueNumbers += string.Format("@{0}", i++);
                                else
                                    valueNumbers += ", " + string.Format("@{0}", i++);

                                first = false;
                            }
                        }

                        // Insert kommando
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}) VALUES({2})",
                            tableNamePrefixSensorStatus,
                            columnNames,
                            valueNumbers);

                        // Insert value parametre
                        i = 1;
                        foreach (var sensorGroup in sensorGroupList)
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(sensorGroup.dbColumn))
                            {
                                string paramName = string.Format("@{0}", i++);

                                if (sensorGroup.status == DataStatus.OK)
                                    cmd.Parameters.AddWithValue(paramName, 1);
                                else
                                    cmd.Parameters.AddWithValue(paramName, 0);
                            }
                        }

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        cmd.ExecuteNonQuery();

                        // Lukke database connection
                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public void Insert(HMSDataCollection hmsDataCollection)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Liste med database column navn
                        string columnNames = "";
                        bool first = true;
                        foreach (var hmsData in hmsDataCollection.GetDataList())
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(hmsData.dbColumn))
                            {
                                if (first)
                                    columnNames += hmsData.dbColumn;
                                else
                                    columnNames += ", " + hmsData.dbColumn;

                                first = false;
                            }
                        }

                        // Value parametre
                        string valueNumbers = "";
                        first = true;
                        int i = 1;
                        foreach (var hmsData in hmsDataCollection.GetDataList())
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(hmsData.dbColumn))
                            {
                                if (first)
                                    valueNumbers += string.Format("@{0}", i++);
                                else
                                    valueNumbers += ", " + string.Format("@{0}", i++);

                                first = false;
                            }
                        }

                        // Insert kommando
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}) VALUES({2})",
                            tableNamePrefixHMSData,
                            columnNames,
                            valueNumbers);

                        // Insert value parametre
                        i = 1;
                        foreach (var hmsData in hmsDataCollection.GetDataList())
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(hmsData.dbColumn))
                            {
                                string paramName = string.Format("@{0}", i++);

                                cmd.Parameters.AddWithValue(paramName, hmsData.data.ToString());
                            }
                        }

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        cmd.ExecuteNonQuery();

                        // Lukke database connection
                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Insert(RadObservableCollectionEx<VerificationData> verificationData)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Insert kommando
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, {22}, {23}, {24}, {25}) VALUES(@1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16, @17, @18, @19, @20, @21, @22, @23, @24, @25)",
                            // Tabell
                            veriDataTableName, 

                            // Kolonner
                            veriDataColumnTimeID,
                            veriDataColumnRollR,
                            veriDataColumnRollL,
                            veriDataColumnPitchU,
                            veriDataColumnPitchD,
                            veriDataColumnInclinationMax,
                            veriDataColumnSHR,
                            veriDataColumnMSI,
                            veriDataColumnWSI,
                            veriDataColumnVesselDir,
                            veriDataColumnHelideckHeading,
                            veriDataColumnHelicopterHeading,
                            veriDataColumnDeltaVesselHeading,
                            veriDataColumnDeltaWindHeading,
                            veriDataColumnRWD,
                            veriDataColumnHelideckStatus,
                            veriDataColumnWindSpd2m,
                            veriDataColumnWindDir2m,
                            veriDataColumnWindGust2m,
                            veriDataColumnWindSpd10m,
                            veriDataColumnWindDir10m,
                            veriDataColumnWindGust10m,
                            veriDataColumnSensorMRU,
                            veriDataColumnSensorGyro,
                            veriDataColumnSensorWind);

                        // Insert parametre
                        int i = 1;
                        foreach (var item in verificationData)
                        {
                            string paramName = string.Format("@{0}", i++);

                            cmd.Parameters.AddWithValue(paramName, item.testData);
                        }

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        cmd.ExecuteNonQuery();

                        // Lukke database connection
                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DatabaseMaintenance(RadObservableCollection<SensorData> sensorDataList)
        {
            // Slette data eldre enn angitt antall dager
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        connection.Open();

                        // Sensor Data
                        ////////////////////////////////
                        lock (sensorDataList)
                        {
                            // Slå opp tabellene for hver sensor verdi og slette data
                            foreach (var sensorData in sensorDataList)
                            {
                                if (sensorData.saveToDatabase)
                                {
                                    string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                                    // Slette alle data i database tabell
                                    cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                                        tableName,
                                        columnTimestamp,
                                        config.ReadWithDefault(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Error Messages Data
                        ////////////////////////////////
                        cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                            tableNameErrorMessages,
                            columnTimestamp,
                            config.ReadWithDefault(ConfigKey.ErrorMessageStorageTime, Constants.DatabaseMessagesStorageTimeDefault));
                        cmd.ExecuteNonQuery();

                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DatabaseMaintenanceHMSData()
        {
            // Slette data eldre enn angitt antall dager
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        connection.Open();

                        // HMS Data
                        ////////////////////////////////

                        // Slette alle gamle data i database tabell
                        cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                            tableNamePrefixHMSData,
                            columnTimestamp,
                            config.ReadWithDefault(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

                        cmd.ExecuteNonQuery();

                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DatabaseMaintenanceSensorStatus()
        {
            // Slette data eldre enn angitt antall dager
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        connection.Open();

                        // Sensor Status
                        ////////////////////////////////
                           
                        // Slette alle gamle data i database tabell
                        cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                            tableNamePrefixSensorStatus,
                            columnTimestamp,
                            config.ReadWithDefault(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

                        cmd.ExecuteNonQuery();

                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void InsertErrorMessage(ErrorMessage errorMessage)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Insert kommando
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}, {3}, {4}) VALUES(@1, @2, @3, @4)", tableNameErrorMessages, columnTimestamp, columnId, columnType, columnMessage);

                        // Insert parametre
                        cmd.Parameters.AddWithValue("@1", errorMessage.timestamp);
                        cmd.Parameters.AddWithValue("@2", errorMessage.id);
                        cmd.Parameters.AddWithValue("@3", errorMessage.type.ToString());
                        cmd.Parameters.AddWithValue("@4", errorMessage.message);

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        cmd.ExecuteNonQuery();

                        // Lukke database connection
                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                // Gjør ingenting her. Dersom feil meldingene ikke fungerer, så hjelper det ikke å prøve å lagre feilen med en ny feil feilmelding...
                // ...da er vi fortapt.

                //throw;
            }
        }

        public List<ErrorMessage> GetLastErrorMessages(ErrorMessageType type, int number)
        {
            List<ErrorMessage> errorMessages = new List<ErrorMessage>();

            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Insert kommando
                        switch (type)
                        {
                            case ErrorMessageType.All:
                                cmd.CommandText = string.Format("SELECT * FROM {0} ORDER BY id DESC LIMIT {1}", tableNameErrorMessages, number);
                                break;

                            default:
                                cmd.CommandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}' ORDER BY id DESC LIMIT {3}", tableNameErrorMessages, columnType, type.ToString(), number);
                                break;
                        }

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        MySqlDataReader reader = cmd.ExecuteReader();

                        // Lese ut data
                        while (reader.Read())
                        {
                            errorMessages.Add(new ErrorMessage(
                                reader.GetDateTime(1),
                                (ErrorMessageType)Enum.Parse(typeof(ErrorMessageType), reader.GetString(3)),
                                ErrorMessageCategory.None,
                                TextHelper.RemoveLinefeed(reader.GetString(4)),
                                reader.GetInt32(2),
                                reader.GetInt32(0)));
                        }

                        // Lukke database connection
                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return errorMessages;
        }

        public void DeleteErrorMessageData()
        {
            // NB! Tabellen må eksistere ellers gies feilmelding
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        connection.Open();

                        // Slette alle data i database table
                        cmd.CommandText = string.Format(@"TRUNCATE {0}", tableNameErrorMessages);
                        cmd.ExecuteNonQuery();

                        connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
