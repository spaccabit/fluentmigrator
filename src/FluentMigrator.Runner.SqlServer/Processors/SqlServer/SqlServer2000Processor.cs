using FluentMigrator.Runner.Helpers;

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
using System.IO;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.BatchParser.Sources;
using FluentMigrator.Runner.BatchParser.SpecialTokenSearchers;

namespace FluentMigrator.Runner.Processors.SqlServer
{
    public sealed class SqlServer2000Processor : GenericProcessorBase
    {
        public SqlServer2000Processor(IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory)
            : base(connection, factory, generator, announcer, options)
        {
        }

        public override string DatabaseType => "SqlServer2000";

        public override IList<string> DatabaseTypeAliases { get; } = new List<string>() { "SqlServer" };

        public override void BeginTransaction()
        {
            base.BeginTransaction();
            Announcer.Sql("BEGIN TRANSACTION");
        }

        public override void CommitTransaction()
        {
            base.CommitTransaction();
            Announcer.Sql("COMMIT TRANSACTION");
        }

        public override void RollbackTransaction()
        {
            base.RollbackTransaction();
            Announcer.Sql("ROLLBACK TRANSACTION");
        }

        public override bool SchemaExists(string schemaName)
        {
            return true;
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            try
            {
                return Exists("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'", FormatHelper.FormatSqlEscape(tableName));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            return Exists("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}'",
                FormatHelper.FormatSqlEscape(tableName),
                FormatHelper.FormatSqlEscape(columnName));
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return Exists("SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_CATALOG = DB_NAME() AND TABLE_NAME = '{0}' AND CONSTRAINT_NAME = '{1}'",
                FormatHelper.FormatSqlEscape(tableName), FormatHelper.FormatSqlEscape(constraintName));
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return Exists("SELECT NULL FROM sysindexes WHERE name = '{0}'", FormatHelper.FormatSqlEscape(indexName));
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            return false;
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("SELECT * FROM [{0}]", tableName);
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

        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(String.Format(template, args), Connection, Transaction, Options))
            using (var reader = command.ExecuteReader())
            {
                return reader.Read();
            }
        }

        public override void Execute(string template, params object[] args)
        {
            Process(String.Format(template, args));
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
                ExecuteNonQuery(sql);
            }
        }

        private static bool ContainsGo(string sql)
        {
            var containsGo = false;
            var parser = new SqlServerBatchParser();
            parser.SpecialToken += (sender, args) => containsGo = true;
            using (var source = new TextReaderSource(new StringReader(sql), true))
            {
                parser.Process(source);
            }

            return containsGo;
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var command = Factory.CreateCommand(sql, Connection, Transaction, Options))
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
            sql += "\nGO"; // make sure last batch is executed.
            var sqlBatch = string.Empty;

            try
            {
                var parser = new SqlServerBatchParser();
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
            EnsureConnectionIsOpen();

            expression.Operation?.Invoke(Connection, Transaction);
        }
    }
}
