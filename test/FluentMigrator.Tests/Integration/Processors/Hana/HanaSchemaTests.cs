using System.Data.SqlClient;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Generators.Hana;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.Hana;
using NUnit.Framework;
using NUnit.Should;
using Sap.Data.Hana;

namespace FluentMigrator.Tests.Integration.Processors.Hana
{
    [TestFixture]
    [Category("Integration")]
    [Category("Hana")]
    public class HanaSchemaTests : BaseSchemaTests
    {
        public HanaConnection Connection { get; set; }
        public HanaProcessor Processor { get; set; }

        [SetUp]
        public void SetUp()
        {
            if (!IntegrationTestOptions.Hana.IsEnabled)
                Assert.Ignore();
            Connection = new HanaConnection(IntegrationTestOptions.Hana.ConnectionString);
            Processor = new HanaProcessor(Connection, new HanaGenerator(), new TextWriterAnnouncer(TestContext.Out), new ProcessorOptions(), new HanaDbFactory());
            Connection.Open();
            Processor.BeginTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            Processor?.CommitTransaction();
            Processor?.Dispose();
        }

        [Test]
        public override void CallingSchemaExistsReturnsFalseIfSchemaDoesNotExist()
        {
            Processor.SchemaExists("DoesNotExist").ShouldBeFalse();
        }

        [Test]
        public override void CallingSchemaExistsReturnsTrueIfSchemaExists()
        {
            Assert.Ignore("Schemas aren't supported by this SAP Hana runner");

            Processor.SchemaExists("SYSTEM").ShouldBeTrue();
        }
    }
}
