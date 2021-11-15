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
        private const string tableNamePrefixSensorData = "sensor_data";
        private const string columnTimestamp = "timestamp";
        private const string columnData = "data";

        // Error Messages
        private const string tableNameErrorMessages = "error_messages";
        private const string columnId = "sensor_data_id";
        private const string columnType = "type";
        private const string columnMessage = "message";

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

            string address = config.Read(ConfigKey.DatabaseAddress);
            string port = config.Read(ConfigKey.DatabasePort);
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

        public void CreateTables(RadObservableCollectionEx<SensorData> sensorDataList)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Opprette tabeller for sensor dasta
                    //////////////////////////////////////////////////////////
                    lock (sensorDataList)
                    {
                        // For hver sensor verdi som skal lagres oppretter vi en ny database tabell, dersom den ikke allerede eksisterer
                        foreach (var sensorData in sensorDataList)
                        {
                            string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                            // Opprette nytt database table
                            cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3), {2} DOUBLE)", tableName, columnTimestamp, columnData);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Opprette error messages tabell
                    //////////////////////////////////////////////////////////
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3), {2} INTEGER, {3} TEXT, {4} TEXT)", tableNameErrorMessages, columnTimestamp, columnId, columnType, columnMessage);
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

        public void CreateTables(RadObservableCollectionEx<HMSData> hmsInputDataList)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Opprette HMS data tabeller
                    //////////////////////////////////////////////////////////
                    lock (hmsInputDataList)
                    {
                        // For hver HMS data verdi som skal lagres oppretter vi en ny database tabell, dersom den ikke allerede eksisterer
                        foreach (var hmsData in hmsInputDataList)
                        {
                            if (!string.IsNullOrEmpty(hmsData.dbTableName))
                            {
                                // Opprette nytt database table
                                cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TIMESTAMP(3), {2} DOUBLE)", hmsData.dbTableName, columnTimestamp, columnData);
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {

                            }
                        }
                    }

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

        public void RemoveUnusedTables(RadObservableCollectionEx<SensorData> sensorDataList)
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

        public void DeleteAllData(RadObservableCollectionEx<SensorData> sensorDataList)
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
                            string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                            // Slette alle data i database table
                            cmd.CommandText = string.Format(@"TRUNCATE {0}", tableName);
                            cmd.ExecuteNonQuery();
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

        public void Insert(HMSData hmsData)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    if (hmsData.timestamp != DateTime.MinValue &&
                        !double.IsNaN(hmsData.data) &&
                        !string.IsNullOrEmpty(hmsData.dbTableName))
                    {
                        using (var connection = new MySqlConnection(connectionString))
                        {
                            // SQL kommando
                            var cmd = new MySqlCommand();
                            cmd.Connection = connection;

                            // Insert kommando
                            cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}) VALUES(@1, @2)", hmsData.dbTableName, columnTimestamp, columnData);

                            // Insert parametre
                            cmd.Parameters.AddWithValue("@1", hmsData.timestamp);
                            cmd.Parameters.AddWithValue("@2", hmsData.data);

                            // Åpne database connection
                            connection.Open();

                            // Insert execute
                            cmd.ExecuteNonQuery();

                            // Lukke database connection
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DatabaseMaintenance(RadObservableCollectionEx<SensorData> sensorDataList)
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
                                string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

                                // Slette alle data i database tabell
                                cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                                    tableName,
                                    columnTimestamp,
                                    config.Read(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Error Messages Data
                        ////////////////////////////////
                        cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                            tableNameErrorMessages,
                            columnTimestamp,
                            config.Read(ConfigKey.ErrorMessageStorageTime, Constants.DatabaseMessagesStorageTimeDefault));
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

        public void DatabaseMaintenance(RadObservableCollectionEx<HMSData> hmsDataList)
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
                        lock (hmsDataList)
                        {
                            // Slå opp tabellene for hver HMS verdi og slette data
                            foreach (var hmsData in hmsDataList)
                            {
                                if (!string.IsNullOrEmpty(hmsData.dbTableName))
                                {
                                    // Slette alle data i database tabell
                                    cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < TIMESTAMP(UTC_TIMESTAMP() - INTERVAL {2} DAY)",
                                        hmsData.dbTableName,
                                        columnTimestamp,
                                        config.Read(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

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
