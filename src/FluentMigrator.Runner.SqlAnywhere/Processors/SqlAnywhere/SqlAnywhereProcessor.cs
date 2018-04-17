#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
// Copyright (c) 2010, Nathan Brown
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.BatchParser.Sources;
using FluentMigrator.Runner.BatchParser.SpecialTokenSearchers;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.SqlAnywhere;

namespace FluentMigrator.Runner.Processors.SqlAnywhere
{
    public sealed class SqlAnywhereProcessor : GenericProcessorBase
    {
        //select 1 from sys.syscolumn as c inner join sys.systable as t on t.table_id = c.table_id where t.table_name = '{0}' and c.column_name = '{1}'
        private const string SCHEMA_EXISTS = "SELECT 1 WHERE EXISTS (SELECT * FROM sys.sysuserperm WHERE user_name = '{0}') ";
        private const string TABLE_EXISTS = "SELECT 1 WHERE EXISTS (SELECT t.* FROM sys.systable AS t INNER JOIN sys.sysuserperm AS up ON up.user_id = t.creator WHERE up.user_name = '{0}' AND t.table_name = '{1}')";
        private const string COLUMN_EXISTS = "SELECT 1 WHERE EXISTS (SELECT c.* FROM sys.syscolumn AS c INNER JOIN sys.systable AS t ON t.table_id = c.table_id INNER JOIN sys.sysuserperm AS up ON up.user_id = t.creator WHERE up.user_name = '{0}' AND t.table_name = '{1}' AND c.column_name = '{2}')";
        private const string CONSTRAINT_EXISTS = "SELECT 1 WHERE EXISTS (SELECT c.* FROM sys.sysconstraint AS c INNER JOIN sys.systable AS t ON t.object_id = c.table_object_id INNER JOIN sys.sysuserperm AS up ON up.user_id = t.creator WHERE up.user_name = '{0}' AND t.table_name = '{1}' AND c.constraint_name = '{2}')";
        private const string INDEX_EXISTS = "SELECT 1 WHERE EXISTS (SELECT i.* FROM sys.sysindex AS i INNER JOIN sys.systable AS t ON t.table_id = i.table_id INNER JOIN sys.sysuserperm AS up ON up.user_id = t.creator WHERE i.index_name = '{0}' AND up.user_name = '{1}' AND t.table_name = '{2}')";
        private const string SEQUENCES_EXISTS = "SELECT 1 WHERE EXISTS (SELECT s.* FROM sys.syssequence AS s INNER JOIN sys.sysuserperm AS up ON up.user_id = s.owner WHERE up.user_name = '{0}' AND s.sequence_name = '{1}' )";
        private const string DEFAULTVALUE_EXISTS = "SELECT 1 WHERE EXISTS (SELECT c.* FROM sys.syscolumn AS c INNER JOIN sys.systable AS t ON t.table_id = c.table_id INNER JOIN sys.sysuserperm AS up ON up.user_id = t.creator WHERE up.user_name = '{0}' AND t.table_name = '{1}' AND c.column_name = '{2}' AND c.default LIKE '{3}')";

        public override string DatabaseType { get; }

        public override IList<string> DatabaseTypeAliases { get; } = new List<string> { "SqlAnywhere" };

        public SqlAnywhereProcessor(string databaseType, IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory)
            : base(connection, factory, generator, announcer, options)
        {
            DatabaseType = databaseType;
        }

        private static string SafeSchemaName(string schemaName)
        {
            return string.IsNullOrEmpty(schemaName) ? "dbo" : FormatHelper.FormatSqlEscape(schemaName);
        }

        public override bool SchemaExists(string schemaName)
        {
            return Exists(SCHEMA_EXISTS, SafeSchemaName(schemaName));
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            try
            {
                return Exists(TABLE_EXISTS, SafeSchemaName(schemaName),
                    FormatHelper.FormatSqlEscape(tableName));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            return Exists(COLUMN_EXISTS, SafeSchemaName(schemaName),
                FormatHelper.FormatSqlEscape(tableName), FormatHelper.FormatSqlEscape(columnName));
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return Exists(CONSTRAINT_EXISTS, SafeSchemaName(schemaName),
                FormatHelper.FormatSqlEscape(tableName), FormatHelper.FormatSqlEscape(constraintName));
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return Exists(INDEX_EXISTS,
                FormatHelper.FormatSqlEscape(indexName), SafeSchemaName(schemaName), FormatHelper.FormatSqlEscape(tableName));
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return Exists(SEQUENCES_EXISTS, SafeSchemaName(schemaName),
                FormatHelper.FormatSqlEscape(sequenceName));
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            string defaultValueAsString = $"%{FormatHelper.FormatSqlEscape(defaultValue.ToString())}%";
            return Exists(DEFAULTVALUE_EXISTS, SafeSchemaName(schemaName),
                FormatHelper.FormatSqlEscape(tableName),
                FormatHelper.FormatSqlEscape(columnName), defaultValueAsString);
        }

        public override void Process(CreateSchemaExpression expression)
        {
            var password = expression.GetAdditionalFeature(SqlAnywhereExtensions.SchemaPassword, string.Empty);
            if (string.IsNullOrEmpty(password))
                throw new Exception("Create schema requires connection for the schema user. No password specified in CreateSchemaExpression.");

            if (!Exists("SELECT count(*) FROM \"dbo\".\"syslogins\" WHERE \"name\"='{0}'", FormatHelper.FormatSqlEscape(expression.SchemaName)))
            {
                // Try to automatically generate the user
                Announcer.Say("Creating user {0}.", expression.SchemaName);
                Execute("CREATE USER \"{0}\" IDENTIFIED BY \"{1}\"", expression.SchemaName, password);
            }

            var sql = Generator.Generate(expression);
            string connectionString = ReplaceUserIdAndPasswordInConnectionString(expression.SchemaName, password);
            Announcer.Say("Creating connection for user {0} to create schema.", expression.SchemaName);
            var connection = Factory.CreateConnection(connectionString);
            EnsureConnectionIsOpen(connection);
            Announcer.Say("Beginning out of scope transaction to create schema.");
            var transaction = connection.BeginTransaction();

            try
            {
                ExecuteNonQuery(connection, transaction, sql);
                transaction.Commit();
                Announcer.Say("Out of scope transaction to create schema committed.");
            }
            catch
            {
                transaction.Rollback();
                Announcer.Say("Out of scope transaction to create schema rolled back.");
                throw;
            }
            finally
            {
                transaction?.Dispose();
                connection.Dispose();
            }
        }

        private string ReplaceUserIdAndPasswordInConnectionString(string userId, string password)
        {
            var csb = new DbConnectionStringBuilder { ConnectionString = ConnectionString };
            var uidKey = new[] { "uid", "userid" }.FirstOrDefault(x => csb.ContainsKey(x)) ?? "uid";
            var pwdKey = new[] { "pwd", "password" }.FirstOrDefault(x => csb.ContainsKey(x)) ?? "pwd";
            csb[uidKey] = userId;
            csb[pwdKey] = password;

            return csb.ConnectionString;
        }

        public override void Execute(string template, params object[] args)
        {
            Process(String.Format(template, args));
        }

        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(String.Format(template, args), Connection, Transaction, Options))
            {
                var result = command.ExecuteScalar();
                return DBNull.Value != result && Convert.ToInt32(result) != 0;
            }
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("SELECT * FROM [{0}].[{1}]", SafeSchemaName(schemaName), tableName);
        }

        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(String.Format(template, args), Connection, Transaction, Options))
            using (var reader = command.ExecuteReader())
            {
                return reader.ReadDataSet();
            }
        }

        protected override void Process(string sql)
        {
            Announcer.Sql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
                return;

            EnsureConnectionIsOpen();

            if (ContainsGo(sql))
            {
                ExecuteBatchNonQuery(sql);

            }
            else
            {
                ExecuteNonQuery(Connection, Transaction, sql);
            }
        }

        private static bool ContainsGo(string sql)
        {
            var containsGo = false;
            var parser = new SqlAnywhereBatchParser();
            parser.SpecialToken += (sender, args) => containsGo = true;
            using (var source = new TextReaderSource(new StringReader(sql), true))
            {
                parser.Process(source);
            }

            return containsGo;
        }

        private void EnsureConnectionIsOpen(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private void ExecuteNonQuery(IDbConnection connection, IDbTransaction transaction, string sql)
        {
            using (var command = Factory.CreateCommand(sql, connection, transaction, Options))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    using (var message = new StringWriter())
                    {
                        message.WriteLine("An error occured executing the following sql:");
                        message.WriteLine(sql);
                        message.WriteLine("The error was {0}", ex.Message);

                        throw new Exception(message.ToString(), ex);
                    }
                }
            }
        }

        private void ExecuteBatchNonQuery(string sql)
        {
            var sqlBatch = string.Empty;

            try
            {
                var parser = new SqlAnywhereBatchParser();
                parser.SqlText += (sender, args) => { sqlBatch = args.SqlText.Trim(); };
                parser.SpecialToken += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(sqlBatch))
                        return;

                    if (args.Opaque is GoSearcher.GoSearcherParameters goParams)
                    {
                        using (var command = Factory.CreateCommand(string.Empty, Connection, Transaction, Options))
                        {
                            command.CommandText = sqlBatch;

                            for (var i = 0; i != goParams.Count; ++i)
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    sqlBatch = null;
                };

                using (var source = new TextReaderSource(new StringReader(sql), true))
                {
                    parser.Process(source, stripComments: true);
                }

                if (!string.IsNullOrEmpty(sqlBatch))
                {
                    using (var command = Factory.CreateCommand(string.Empty, Connection, Transaction, Options))
                    {
                        command.CommandText = sqlBatch;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                using (var message = new StringWriter())
                {
                    message.WriteLine("An error occured executing the following sql:");
                    message.WriteLine(string.IsNullOrEmpty(sqlBatch) ? sql : sqlBatch);
                    message.WriteLine("The error was {0}", ex.Message);

                    throw new Exception(message.ToString(), ex);
                }
            }
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Announcer.Say("Performing DB Operation");

            if (Options.PreviewOnly)
                return;

            EnsureConnectionIsOpen();

            expression.Operation?.Invoke(Connection, Transaction);
        }
    }
}

