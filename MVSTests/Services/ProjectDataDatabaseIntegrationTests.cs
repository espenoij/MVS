using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Models;
using MVSTests.TestInfrastructure;
using MySqlConnector;

namespace MVSTests.Services
{
    [TestClass]
    public class ProjectDataDatabaseIntegrationTests
    {
        [TestMethod]
        public void LoadSessionData_ReturnsExpectedRowsAndClearsOnMissingTable()
        {
            if (Environment.GetEnvironmentVariable("MVS_TEST_LOCAL_MARIADB") != "1")
                Assert.Inconclusive("Set MVS_TEST_LOCAL_MARIADB=1 with the isolated local MariaDB fixture running.");

            var schema = "mvs_migration_test_" + Guid.NewGuid().ToString("N");
            const string server = "Server=127.0.0.1;Port=45173;User ID=root;Password=;SslMode=None;";
            using (var connection = new MySqlConnection(server))
            {
                connection.Open();
                Execute(connection, "CREATE DATABASE `" + schema + "`");
                try
                {
                    Execute(connection, "CREATE TABLE `" + schema + "`.mvs_data_54321 (" +
                        "id INT NOT NULL PRIMARY KEY, timestamp DATETIME NOT NULL, " +
                        "ref_pitch VARCHAR(40), ref_roll VARCHAR(40), ref_heave VARCHAR(40), " +
                        "test_pitch VARCHAR(40), test_roll VARCHAR(40), test_heave VARCHAR(40))");
                    Execute(connection, "INSERT INTO `" + schema + "`.mvs_data_54321 VALUES " +
                        "(1,'2026-01-01 00:00:00','1','2','3','4','5','6')," +
                        "(2,'2026-01-01 00:00:01','7','8','9','10','11','12')");

                    StaTestHelper.Run(() =>
                    {
                        var handler = new DatabaseHandler(null, isDatabaseConnectionOK: true);
                        typeof(DatabaseHandler).GetField("connectionString", BindingFlags.Instance | BindingFlags.NonPublic)
                            .SetValue(handler, server + "Database=" + schema + ";");
                        var errors = new ErrorHandler(handler);
                        var database = new MVSDatabase(handler, errors);
                        var current = new ProjectVM().projectsDataList;
                        current.Add(new ProjectData { id = -1 });
                        var project = new Project { Id = 54321 };

                        database.LoadSessionData(project, current);

                        Assert.AreEqual(2, current.Count);
                        CollectionAssert.AreEqual(
                            new[]
                            {
                                "1|2026-01-01 00:00:00|1|2|3|4|5|6",
                                "2|2026-01-01 00:00:01|7|8|9|10|11|12"
                            },
                            current.Select(SampleValues).ToArray());
                        Assert.IsFalse(errors.IsDatabaseError());

                        project.Id = 54322;
                        database.LoadSessionData(project, current);
                        Assert.AreEqual(0, current.Count);
                        Assert.AreEqual(
                            ErrorHandler.DatabaseErrorType.LoadSessionData,
                            errors.GetDatabaseErrorType());
                        Assert.IsTrue(errors.IsDatabaseError());
                        Assert.IsFalse(string.IsNullOrEmpty(errors.GetErrorMessageList().Last().message));
                    });
                }
                finally
                {
                    Execute(connection, "DROP DATABASE `" + schema + "`");
                }
            }
        }

        private static void Execute(MySqlConnection connection, string sql)
        {
            using (var command = new MySqlCommand(sql, connection))
                command.ExecuteNonQuery();
        }

        private static string SampleValues(ProjectData sample)
        {
            return string.Join("|", sample.id, sample.timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                sample.refPitch, sample.refRoll, sample.refHeave,
                sample.testPitch, sample.testRoll, sample.testHeave);
        }
    }
}
