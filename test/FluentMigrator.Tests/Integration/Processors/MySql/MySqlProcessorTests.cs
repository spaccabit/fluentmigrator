using System.IO;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Generators.MySql;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.MySql;

using MySql.Data.MySqlClient;

using NUnit.Framework;
using NUnit.Should;

namespace FluentMigrator.Tests.Integration.Processors.MySql
{
    [TestFixture]
    [Category("Integration")]
    [Category("MySql")]
    public class MySqlProcessorTests
    {
        protected MySqlProcessor Processor;

        protected MySqlConnection Connection;

        [SetUp]
        public void SetUp()
        {
            if (!IntegrationTestOptions.MySql.IsEnabled)
                Assert.Ignore();
            Connection = new MySqlConnection(IntegrationTestOptions.MySql.ConnectionString);
            Processor = new MySqlProcessor(Connection, new MySql4Generator(),
                new TextWriterAnnouncer(TestContext.Out), new ProcessorOptions(), new MySqlDbFactory());
            Connection.Open();
        }

        [TearDown]
        public void TearDown()
        {
            Processor?.Dispose();
        }

        [Test]
        public void CallingProcessWithPerformDBOperationExpressionWhenInPreviewOnlyModeWillNotMakeDbChanges()
        {
            var output = new StringWriter();

            var connection = new MySqlConnection(IntegrationTestOptions.MySql.ConnectionString);

            var processor = SetupMySqlProcessorWithPreviewOnly(output, connection);

            bool tableExists;

            try
            {
                var expression =
                    new PerformDBOperationExpression
                    {
                        Operation = (con, trans) =>
                        {
                            var command = con.CreateCommand();
                            command.CommandText = "CREATE TABLE processtesttable (test int NULL) ";
                            command.Transaction = trans;

                            command.ExecuteNonQuery();
                        }
                    };

                processor.Process(expression);

                var com = connection.CreateCommand();
                com.CommandText = "";

                tableExists = processor.TableExists("", "processtesttable");
            }
            finally
            {
                processor.RollbackTransaction();
            }

            tableExists.ShouldBeFalse();
        }

        [Test]
        public void CallingExecuteWithPerformDBOperationExpressionWhenInPreviewOnlyModeWillNotMakeDbChanges()
        {
            var output = new StringWriter();

            var connection = new MySqlConnection(IntegrationTestOptions.MySql.ConnectionString);

            var processor = SetupMySqlProcessorWithPreviewOnly(output, connection);

            bool tableExists;

            try
            {
                processor.Execute("CREATE TABLE processtesttable (test int NULL) ");

                tableExists = processor.TableExists("", "processtesttable");
            }
            finally
            {
                processor.RollbackTransaction();
            }

            tableExists.ShouldBeFalse();
        }

        [Test]
        public void CallingDefaultValueExistsReturnsTrueWhenMatches()
        {
            try
            {
                Processor.Execute("CREATE TABLE dftesttable (test int NULL DEFAULT 1) ");
                Processor.DefaultValueExists(null, "dftesttable", "test", 1).ShouldBeTrue();
            }
            finally
            {
                Processor.Execute("DROP TABLE dftesttable");
            }
        }

        [Test]
        public void CallingReadTableDataQuotesTableName()
        {
            try
            {
                Processor.Execute("CREATE TABLE `infrastructure.version` (test int null) ");
                Processor.ReadTableData(null, "infrastructure.version");
            }
            finally
            {
                Processor.Execute("DROP TABLE `infrastructure.version`");
            }
        }

        private static MySqlProcessor SetupMySqlProcessorWithPreviewOnly(StringWriter output,
            MySqlConnection connection)
        {
            var processor = new MySqlProcessor(
                connection,
                new MySql4Generator(),
                new TextWriterAnnouncer(output),
                new ProcessorOptions { PreviewOnly = true },
                new MySqlDbFactory());
            return processor;
        }
    }
}
