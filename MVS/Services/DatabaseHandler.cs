using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Telerik.Windows.Data;
using MySqlConnector;
using System.Windows.Markup;
using static MVS.DialogImport;
using MVS.Models;

namespace MVS
{
    public class DatabaseHandler
    {
        // Database connection parametre
        private string connectionString;

        // MVS Data Set tabell
        private const string tableNameMVSDataSets = "mvs_data_sets";
        private const string columnMVSName = "name";
        private const string columnMVSComments = "comments";
        private const string columnMVSInputSetup = "input_setup";

        // MVS Data tabeller
        private const string tableNameMVSDataPrefix = "mvs_data_";
        private const string columnTimestamp = "timestamp";
        private const string columnRefPitch = "ref_pitch";
        private const string columnRefRoll = "ref_roll";
        private const string columnRefHeave = "ref_heave";
        private const string columnTestPitch = "test_pitch";
        private const string columnTestRoll = "test_roll";
        private const string columnTestHeave = "test_heave";

        // Error Messages
        private const string tableNameErrorMessages = "error_messages";
        private const string columnId = "sensor_data_id";
        private const string columnType = "type";
        private const string columnMessage = "message";

        // Får vi opprettet OK forbindelse?
        private bool isDatabaseConnectionOK;

        // Configuration
        private Config config;

        public DatabaseHandler(Config config = null, bool isDatabaseConnectionOK = false)
        {
            if (config == null)
                this.config = new Config();
            else
                this.config = config;

            // Generate encrypted user ID
            //this.config.Write(ConfigKey.DatabaseUserID, Encryption.EncryptString(Encryption.ToSecureString("root")));

            // Generate encrypted password
            //this.config.Write(ConfigKey.DatabasePassword, Encryption.EncryptString(Encryption.ToSecureString("test99")));

            string address = this.config.ReadWithDefault(ConfigKey.DatabaseAddress, Constants.DefaultDatabaseAddress);
            string port = this.config.ReadWithDefault(ConfigKey.DatabasePort, Constants.DefaultDatabasePort).ToString();
            string database = this.config.ReadWithDefault(ConfigKey.DatabaseName, Constants.DefaultDatabaseName);

            // Database login
            SecureString userid = Encryption.DecryptString(this.config.Read(ConfigKey.DatabaseUserID));
            SecureString password = Encryption.DecryptString(this.config.Read(ConfigKey.DatabasePassword));

            connectionString = string.Format(@"server={0};port={1};userid={2};password={3};database={4};sslmode=none",
                address,
                port,
                Encryption.ToInsecureString(userid),
                Encryption.ToInsecureString(password),
                database);

            this.isDatabaseConnectionOK = isDatabaseConnectionOK;
        }

        public void CreateDataTables(Project dataSet, MVSDataCollection mvsDataCollection)
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

        public void CreateSessionTables()
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
                        columnMVSComments,
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

        public bool TableExists(Project dataSet)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var cmd = new MySqlCommand();
                cmd.Connection = connection;

                connection.Open();

                cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
                cmd.Parameters.AddWithValue("@TableName", tableNameMVSDataPrefix + dataSet.Id);
        
                int count = Convert.ToInt32(cmd.ExecuteScalar());
        
                return count > 0;
            }
        }

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

        public void Insert(Project dataSet, MVSDataCollection mvsDataCollection)
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

        public int Insert(Project dataSet)
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
                        cmd.CommandText = string.Format("INSERT INTO {0}({1}, {2}, {3}) VALUES(@Name, @Comments, @InputSetup)",
                            tableNameMVSDataSets,
                            columnMVSName,
                            columnMVSComments,
                            columnMVSInputSetup);

                        // Insert parametre
                        cmd.Parameters.AddWithValue("@Name", dataSet.Name);
                        cmd.Parameters.AddWithValue("@Comments", dataSet.Comments);
                        cmd.Parameters.AddWithValue("@InputSetup", (int)dataSet.InputMRUs);

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

        public void Update(Project dataSet)
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
                        cmd.CommandText = string.Format("UPDATE {0} SET {1}=@Name, {2}=@Comments, {3}=@InputSetup WHERE id={4}",
                            tableNameMVSDataSets,
                            columnMVSName,
                            columnMVSComments,
                            columnMVSInputSetup,
                            dataSet.Id);

                        // Update parametre
                        cmd.Parameters.AddWithValue("@Name", dataSet.Name);
                        cmd.Parameters.AddWithValue("@Comments", dataSet.Comments);
                        cmd.Parameters.AddWithValue("@InputSetup", (int)dataSet.InputMRUs);

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

        public void DeleteDataSet(Project dataSet)
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

        public void DeleteData(Project dataSet)
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

        public List<Project> GetAllProjects()
        {
            List<Project> dataSets = new List<Project>();

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
                            dataSets.Add(new Project(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                (InputMRUType)reader.GetInt32(3)));
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

        // Henter start og end timestamp fra databasen
        public void LoadTimestamps(Project dataSet)
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
                        else
                            dataSet.StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

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
                        else
                            dataSet.EndTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

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

        public void LoadSessionData(Project dataSet, RadObservableCollection<ProjectData> dataList)
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

                        // SQL
                        cmd.CommandText = string.Format("SELECT {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7} FROM {8}",
                            "id",
                            columnTimestamp,
                            columnRefPitch,
                            columnRefRoll,
                            columnRefHeave,
                            columnTestPitch,
                            columnTestRoll,
                            columnTestHeave,
                            tableNameMVSDataPrefix + dataSet.Id);

                        // Hente data
                        MySqlDataReader dbReader = cmd.ExecuteReader();

                        // Lagre data
                        while (dbReader.Read())
                        {
                            ProjectData data = new ProjectData();

                            data.id = dbReader.GetInt32(0);
                            data.timestamp = dbReader.GetDateTime(1);

                            data.refPitch = Convert.ToDouble(dbReader.GetString(2));
                            data.refRoll = Convert.ToDouble(dbReader.GetString(3));
                            data.refHeave = Convert.ToDouble(dbReader.GetString(4));

                            data.testPitch = Convert.ToDouble(dbReader.GetString(5));
                            data.testRoll = Convert.ToDouble(dbReader.GetString(6));
                            data.testHeave = Convert.ToDouble(dbReader.GetString(7));

                            dataList.Add(data);
                        }

                        // Lukke leser
                        dbReader.Close();

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

        public ImportResult ImportHMSData(Project selectedSession, ReportProgressDelegate reportProgress)
        {
            ImportResult result = new ImportResult();

            try
            {
                if (isDatabaseConnectionOK)
                {
                    RadObservableCollection<ProjectData> dataList = new RadObservableCollection<ProjectData>();

                    string hmsDBaddress = config.ReadWithDefault(ConfigKey.HMSDatabaseAddress, Constants.DefaultHMSDatabaseAddress);
                    string hmsDBport = config.ReadWithDefault(ConfigKey.HMSDatabasePort, Constants.DefaultHMSDatabasePort).ToString();
                    string hmsDBdatabase = config.ReadWithDefault(ConfigKey.HMSDatabaseName, Constants.DefaultHMSDatabaseName);
                    string hmsDBdatabaseTable = config.ReadWithDefault(ConfigKey.HMSDatabaseTable, Constants.DefaultHMSDatabaseTable);

                    // Database login
                    SecureString hmsDBuserid = Encryption.DecryptString(config.Read(ConfigKey.HMSDatabaseUserID));
                    SecureString hmsDBpassword = Encryption.DecryptString(config.Read(ConfigKey.HMSDatabasePassword));

                    string hmsDBconnectionString = string.Format(@"server={0};port={1};userid={2};password={3};database={4};sslmode=none",
                        hmsDBaddress,
                        hmsDBport,
                        Encryption.ToInsecureString(hmsDBuserid),
                        Encryption.ToInsecureString(hmsDBpassword),
                        hmsDBdatabase);

                    // Leser først fra HMS database
                    using (var connection = new MySqlConnection(hmsDBconnectionString))
                    {
                        // SQL kommando
                        var cmd = new MySqlCommand();
                        cmd.Connection = connection;

                        // Åpne database connection
                        connection.Open();

                        // SQL
                        cmd.CommandText = string.Format("SELECT {0}, {1}, {2}, {3}, {4} FROM {5} WHERE {6} BETWEEN @StartTime AND @EndTime",
                            "id",
                            columnTimestamp,
                            "pitch",
                            "roll",
                            "heave",
                            hmsDBdatabaseTable,
                            columnTimestamp);

                        // Update parametre
                        cmd.Parameters.AddWithValue("@StartTime", selectedSession.StartTime);
                        cmd.Parameters.AddWithValue("@EndTime", selectedSession.EndTime);

                        // Hente data
                        MySqlDataReader dbReader = cmd.ExecuteReader();

                        // Lagre data
                        while (dbReader.Read())
                        {
                            ProjectData data = new ProjectData();

                            data.id = dbReader.GetInt32(0);
                            data.timestamp = dbReader.GetDateTime(1);

                            data.testPitch = Convert.ToDouble(dbReader.GetString(2));

                            // I data fra sensor er positive tall roll til høyre.
                            // Internt er positive tall roll til venstre. Venstre er høyest på grafen. Dette er standard i CAP.
                            data.testRoll = Convert.ToDouble(dbReader.GetString(3)) * -1;
                            
                            data.testHeave = Convert.ToDouble(dbReader.GetString(4));

                            dataList.Add(data);
                        }

                        // Lukke leser
                        dbReader.Close();

                        // Lukke database connection
                        connection.Close();
                    }

                    // Så lagrer vi data i MVS database
                    if (dataList.Count > 0)
                    {
                        using (var connection = new MySqlConnection(connectionString))
                        {
                            // SQL kommando
                            var cmd = new MySqlCommand();
                            cmd.Connection = connection;

                            // Åpne database connection
                            connection.Open();

                            cmd.CommandText = string.Format("UPDATE {0} SET {1}=@testPitch, {2}=@testRoll, {3}=@testHeave WHERE {4} = (SELECT {4} FROM {0} ORDER BY ABS(TIMEDIFF({4}, @timestamp)) LIMIT 1)",
                                tableNameMVSDataPrefix + selectedSession.Id,
                                columnTestPitch,
                                columnTestRoll,
                                columnTestHeave,
                                columnTimestamp);

                            double progressCount = 0;

                            foreach (var data in dataList)
                            {
                                cmd.Parameters.Clear();

                                // Update parametre
                                cmd.Parameters.AddWithValue("@timestamp", data.timestamp);
                                cmd.Parameters.AddWithValue("@testPitch", data.testPitch.ToString());
                                cmd.Parameters.AddWithValue("@testRoll", data.testRoll.ToString());
                                cmd.Parameters.AddWithValue("@testHeave", data.testHeave.ToString());

                                // Execute
                                cmd.ExecuteNonQuery();

                                // Progress oppdatering
                                reportProgress((int)((progressCount++ / dataList.Count) * 100));
                            }

                            // Lukke database connection
                            connection.Close();
                        }
                    }
                    else
                    {
                        result.code = ImportResultCode.NoDataFoundForSelectedTimeframe;
                    }
                }
                else
                {
                    result.code = ImportResultCode.ConnectionToMVSDatabaseFailed;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return result;
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
