#region License
//
// Copyright (c) 2018, Fluent Migrator Project
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
using FluentMigrator.Runner.Generators.Redshift;
using FluentMigrator.Runner.Helpers;

namespace FluentMigrator.Runner.Processors.Redshift
{
    public class RedshiftProcessor : GenericProcessorBase
    {
        readonly RedshiftQuoter quoter = new RedshiftQuoter();

        public override string DatabaseType => "Redshift";

        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        public RedshiftProcessor(IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory)
            : base(connection, factory, generator, announcer, options)
        {
        }

        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        public override bool SchemaExists(string schemaName)
        {
            return Exists("select * from information_schema.schemata where schema_name ilike '{0}'", FormatToSafeSchemaName(schemaName));
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            return Exists("select * from information_schema.tables where table_schema ilike '{0}' and table_name ilike '{1}'", FormatToSafeSchemaName(schemaName), FormatToSafeName(tableName));
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            return Exists("select * from information_schema.columns where table_schema ilike '{0}' and table_name ilike '{1}' and column_name ilike '{2}'", FormatToSafeSchemaName(schemaName), FormatToSafeName(tableName), FormatToSafeName(columnName));
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
            => false;

        public override bool IndexExists(string schemaName, string tableName, string indexName)
            => false;

        public override bool SequenceExists(string schemaName, string sequenceName)
            => false;

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("SELECT * FROM {0}", quoter.QuoteTableName(tableName, schemaName));
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            string defaultValueAsString = string.Format("%{0}%", FormatHelper.FormatSqlEscape(defaultValue.ToString()));
            return Exists("select * from information_schema.columns where table_schema ilike '{0}' and table_name ilike '{1}' and column_name ilike '{2}' and column_default like '{3}'", FormatToSafeSchemaName(schemaName), FormatToSafeName(tableName), FormatToSafeName(columnName), defaultValueAsString);
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

        protected override void Process(string sql)
        {
            Announcer.Sql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
                return;

            EnsureConnectionIsOpen();

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
                        message.WriteLine("An error occurred executing the following sql:");
                        message.WriteLine(sql);
                        message.WriteLine("The error was {0}", ex.Message);

                        throw new Exception(message.ToString(), ex);
                    }
                }
            }
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Announcer.Say("Performing DB Operation");

            if (Options.PreviewOnly)
                return;

            EnsureConnectionIsOpen();

            if (expression.Operation != null)
                expression.Operation(Connection, Transaction);
        }

        private string FormatToSafeSchemaName(string schemaName)
        {
            return FormatHelper.FormatSqlEscape(quoter.UnQuoteSchemaName(schemaName));
        }

        private string FormatToSafeName(string sqlName)
        {
            return FormatHelper.FormatSqlEscape(quoter.UnQuote(sqlName));
        }
    }
}
