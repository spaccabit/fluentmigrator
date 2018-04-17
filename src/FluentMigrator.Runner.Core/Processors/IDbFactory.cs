namespace FluentMigrator.Runner.Processors
{
    using System.Data;

    public interface IDbFactory
    {
        IDbConnection CreateConnection(string connectionString);
        IDbCommand CreateCommand(string commandText, IDbConnection connection, IDbTransaction transaction, IMigrationProcessorOptions options);
    }
}
