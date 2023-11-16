using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace MVS
{
    public class DatabaseHandler
    {
        // Database connection parametre
        private string connectionString;

        // MVS Data Set tabeller
        private const string tableNameMVSDataSets = "mvs_data_sets";
        private const string columnMVSName = "name";
        private const string columnMVSDescription = "description";
        private const string columnMVSInputSetup = "input_setup";

        // MVS Data Set tabeller
        private const string tableNameMVSDataPrefix = "mvs_data_";
        private const string columnTimestamp = "timestamp";

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

            string address = config.ReadWithDefault(ConfigKey.DatabaseAddress, Constants.DefaultDatabaseAddress);
            string port = config.ReadWithDefault(ConfigKey.DatabasePort, Constants.DefaultDatabasePort).ToString();
            string database = config.ReadWithDefault(ConfigKey.DatabaseName, Constants.DefaultDatabaseName);

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

        //public void CreateTables(RadObservableCollection<SensorData> sensorDataList)
        //{
        //    try
        //    {
        //        using (var connection = new MySqlConnection(connectionString))
        //        {
        //            var cmd = new MySqlCommand();
        //            cmd.Connection = connection;

        //            connection.Open();

        //            // Opprette tabeller for sensor data
        //            //////////////////////////////////////////////////////////
        //            lock (sensorDataList)
        //            {
        //                // For hver sensor verdi som skal lagres oppretter vi en ny database tabell, dersom den ikke allerede eksisterer
        //                foreach (var sensorData in sensorDataList.ToList())
        //                {
        //                    if (sensorData.saveToDatabase)
        //                    {
        //                        string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

        //                        // Opprette nytt database table
        //                        cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} DATETIME(3), {2} DOUBLE)", tableName, columnTimestamp, columnData);
        //                        cmd.ExecuteNonQuery();
        //                    }
        //                }
        //            }

        //            // Opprette error messages tabell
        //            //////////////////////////////////////////////////////////
        //            cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} DATETIME(3), {2} INTEGER, {3} TEXT, {4} TEXT)", tableNameErrorMessages, columnTimestamp, columnId, columnType, columnMessage);
        //            cmd.ExecuteNonQuery();

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

        public void CreateDataTables(RecordingSession dataSet, MVSDataCollection mvsDataCollection)
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
                    foreach (var hmsData in mvsDataCollection.GetDataList().ToList())
                    {
                        // Kun variabler som har column navn satt skal legges inn
                        if (!string.IsNullOrEmpty(hmsData.dbColumn))
                            columnNames += ", " + hmsData.dbColumn + " TEXT";
                    }

                    // Opprette nytt database table
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} DATETIME(3){2})",
                        tableNameMVSDataPrefix + dataSet.Id,
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

        public void CreateDataSetTables()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Opprette nytt database table
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} TEXT, {2} TEXT, {3} INTEGER)",
                        tableNameMVSDataSets,
                        columnMVSName,
                        columnMVSDescription,
                        columnMVSInputSetup);

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

        public void CreateErrorMessagesTables()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    // Opprette error messages tabell
                    //////////////////////////////////////////////////////////
                    cmd.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS {0}(id INTEGER PRIMARY KEY AUTO_INCREMENT, {1} DATETIME(3), {2} INTEGER, {3} TEXT, {4} TEXT)", tableNameErrorMessages, columnTimestamp, columnId, columnType, columnMessage);

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

        //public void RemoveUnusedTables(RadObservableCollection<SensorData> sensorDataList)
        //{
        //    try
        //    {
        //        using (var connection = new MySqlConnection(connectionString))
        //        {
        //            var cmd = new MySqlCommand();
        //            cmd.Connection = connection;

        //            connection.Open();

        //            // Vi leser nextID variabelen for å finne ut hvor mange tabeller vi må sjekke
        //            string result = config.Read(ConfigKey.nextID, ConfigKey.SensorSectionHeader, ConfigType.Data);

        //            // Dersom vi finner nextID variabelen og der er data
        //            if (result != string.Empty)
        //            {
        //                // Konverter til integer
        //                int totalIDs = Convert.ToInt32(result);

        //                lock (sensorDataList)
        //                {
        //                    for (int i = 0; i < totalIDs; i++)
        //                    {
        //                        if (sensorDataList.ToList().Where(x => x.id == i)?.Count() == 0)
        //                        {
        //                            string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, i);

        //                            // Slette database tabell
        //                            cmd.CommandText = string.Format("DROP TABLE IF EXISTS {0}", tableName);
        //                            cmd.ExecuteNonQuery();
        //                        }
        //                    }
        //                }
        //            }

        //            connection.Close();
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public void DeleteAllData(RadObservableCollection<SensorData> sensorDataList)
        //{
        //    // NB! Tabellen må eksistere ellers gies feilmelding

        //    try
        //    {
        //        using (var connection = new MySqlConnection(connectionString))
        //        {
        //            var cmd = new MySqlCommand();
        //            cmd.Connection = connection;

        //            connection.Open();

        //            lock (sensorDataList)
        //            {
        //                // Slå opp tabellene for hver sensor verdi og slette data
        //                foreach (var sensorData in sensorDataList.ToList())
        //                {
        //                    if (sensorData.saveToDatabase)
        //                    {
        //                        string tableName = string.Format("{0}_{1}", tableNamePrefixSensorData, sensorData.id);

        //                        // Slette alle data i database table
        //                        cmd.CommandText = string.Format(@"TRUNCATE {0}", tableName);
        //                        cmd.ExecuteNonQuery();
        //                    }
        //                }
        //            }

        //            // Slette også alle feilmeldinger fra databasen
        //            cmd.CommandText = string.Format(@"TRUNCATE {0}", tableNameErrorMessages);
        //            cmd.ExecuteNonQuery();

        //            connection.Close();
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public void DeleteAllDataSets()
        {
            // NB! Tabellen må eksistere ellers gies feilmelding

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    var cmd = new MySqlCommand();
                    cmd.Connection = connection;

                    connection.Open();

                    string tableName = string.Format("{0}", tableNameMVSDataSets);

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

        public void Insert(RecordingSession dataSet, MVSDataCollection mvsDataCollection)
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

                        // Først legge inn timestamp
                        columnNames += columnTimestamp;

                        foreach (var hmsData in mvsDataCollection.GetDataList().ToList())
                        {
                            // Kun variabler som har column navn satt skal legges inn
                            if (!string.IsNullOrEmpty(hmsData.dbColumn))
                                columnNames += ", " + hmsData.dbColumn;
                        }

                        // Value parametre
                        string valueNumbers = "";
                        int i = 1;

                        // Først legge inn timestamp
                        valueNumbers += string.Format("@{0}", i++);

                        foreach (var hmsData in mvsDataCollection.GetDataList().ToList())
                        {
                            if (hmsData != null)
                            {
                                // Kun variabler som har column navn satt skal legges inn
                                if (!string.IsNullOrEmpty(hmsData.dbColumn))
                                    valueNumbers += ", " + string.Format("@{0}", i++);
                            }
                        }

                        // Insert kommando
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}) VALUES({2})",
                            tableNameMVSDataPrefix + dataSet.Id,
                            columnNames,
                            valueNumbers);

                        // Insert value parametre
                        i = 1;

                        // Først legge inn timestamp
                        cmd.Parameters.AddWithValue(string.Format("@{0}", i++), DateTime.UtcNow);

                        foreach (var hmsData in mvsDataCollection.GetDataList().ToList())
                        {
                            if (hmsData != null)
                            {
                                // Kun variabler som har column navn satt skal legges inn
                                if (!string.IsNullOrEmpty(hmsData.dbColumn))
                                {
                                    string paramName = string.Format("@{0}", i++);

                                    cmd.Parameters.AddWithValue(paramName, hmsData.data.ToString());
                                }
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

        public int Insert(RecordingSession dataSet)
        {
            int id = -1;

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
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}, {3}) VALUES(@Name, @Description, @InputSetup)",
                            tableNameMVSDataSets,
                            columnMVSName,
                            columnMVSDescription,
                            columnMVSInputSetup);

                        // Insert parametre
                        cmd.Parameters.AddWithValue("@Name", dataSet.Name);
                        cmd.Parameters.AddWithValue("@Description", dataSet.Description);
                        cmd.Parameters.AddWithValue("@InputSetup", (int)dataSet.InputSetup);

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        cmd.ExecuteNonQuery();

                        // Lukke database connection
                        connection.Close();

                        // Retrieve id of last insert
                        id = (int)cmd.LastInsertedId;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return id;
        }

        public void Update(RecordingSession dataSet)
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
                        cmd.CommandText = string.Format("UPDATE {0} SET {1}=@Name, {2}=@Description, {3}=@InputSetup WHERE id={4}",
                            tableNameMVSDataSets,
                            columnMVSName,
                            columnMVSDescription,
                            columnMVSInputSetup,
                            dataSet.Id);

                        // Update parametre
                        cmd.Parameters.AddWithValue("@Name", dataSet.Name);
                        cmd.Parameters.AddWithValue("@Description", dataSet.Description);
                        cmd.Parameters.AddWithValue("@InputSetup", (int)dataSet.InputSetup);

                        // Åpne database connection
                        connection.Open();

                        // Execute
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

        public void DeleteDataSet(RecordingSession dataSet)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE id = {1}",
                            tableNameMVSDataSets,
                            dataSet.Id);

                        // Åpne database connection
                        connection.Open();

                        // Execute
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

        public void DeleteData(RecordingSession dataSet)
        {
            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        cmd.CommandText = string.Format(@"DROP TABLE IF EXISTS {0}",
                            tableNameMVSDataPrefix + dataSet.Id);

                        // Åpne database connection
                        connection.Open();

                        // Execute
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

        public List<RecordingSession> GetAllSessions()
        {
            List<RecordingSession> dataSets = new List<RecordingSession>();

            try
            {
                if (isDatabaseConnectionOK)
                {
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;
                        cmd.CommandText = string.Format("SELECT * FROM {0} ORDER BY id",
                            tableNameMVSDataSets);

                        // Åpne database connection
                        connection.Open();

                        // Insert execute
                        MySqlDataReader reader = cmd.ExecuteReader();

                        // Lese ut data
                        while (reader.Read())
                        {
                            dataSets.Add(new RecordingSession(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                (VerificationInputSetup)reader.GetInt32(3)));
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

            return dataSets;
        }

        public void LoadTimestamps(RecordingSession dataSet)
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

                        // Åpne database connection
                        connection.Open();

                        // 1: Hente start timestamp
                        cmd.CommandText = string.Format("SELECT * FROM {0} LIMIT 1",
                            tableNameMVSDataPrefix + dataSet.Id);

                        // Hente data
                        MySqlDataReader readerStart = cmd.ExecuteReader();

                        // Lagre start tid
                        if (readerStart.Read())
                            dataSet.StartTime = readerStart.GetDateTime(1);

                        // Lukke leser
                        readerStart.Close();

                        // 2: Hente end timestamp
                        cmd.CommandText = string.Format("SELECT * FROM {0} ORDER BY id DESC LIMIT 1",
                            tableNameMVSDataPrefix + dataSet.Id);

                        // Hente data
                        MySqlDataReader readerEnd = cmd.ExecuteReader();

                        // Lagre end tid
                        if (readerEnd.Read())
                            dataSet.EndTime = readerEnd.GetDateTime(1);

                        // Lukke leser
                        readerEnd.Close();

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

        // Method to get the first entry in the database
        public void DatabaseMaintenance()
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

        //public void DatabaseMaintenanceHMSData()
        //{
        //    // Slette data eldre enn angitt antall dager
        //    try
        //    {
        //        if (isDatabaseConnectionOK)
        //        {
        //            using (var connection = new MySqlConnection(connectionString))
        //            {
        //                var cmd = new MySqlCommand();
        //                cmd.Connection = connection;

        //                connection.Open();

        //                // HMS Data
        //                ////////////////////////////////

        //                // Slette alle gamle data i database tabell
        //                cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < DATETIME(UTC_DATETIME() - INTERVAL {2} DAY)",
        //                    tableNamePrefixHMSData,
        //                    columnTimestamp,
        //                    config.ReadWithDefault(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

        //                cmd.ExecuteNonQuery();

        //                connection.Close();
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public void DatabaseMaintenanceSensorStatus()
        //{
        //    // Slette data eldre enn angitt antall dager
        //    try
        //    {
        //        if (isDatabaseConnectionOK)
        //        {
        //            using (var connection = new MySqlConnection(connectionString))
        //            {
        //                var cmd = new MySqlCommand();
        //                cmd.Connection = connection;

        //                connection.Open();

        //                // Sensor Status
        //                ////////////////////////////////

        //                // Slette alle gamle data i database tabell
        //                cmd.CommandText = string.Format(@"DELETE FROM {0} WHERE {1} < DATETIME(UTC_DATETIME() - INTERVAL {2} DAY)",
        //                    tableNamePrefixSensorStatus,
        //                    columnTimestamp,
        //                    config.ReadWithDefault(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault));

        //                cmd.ExecuteNonQuery();

        //                connection.Close();
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

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
