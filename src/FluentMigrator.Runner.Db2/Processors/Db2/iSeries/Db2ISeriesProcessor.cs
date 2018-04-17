#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
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

using System.Collections.Generic;
using System.Data;
using System.Linq;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.DB2;
using FluentMigrator.Runner.Helpers;

namespace FluentMigrator.Runner.Processors.DB2.iSeries
{
    public class Db2ISeriesProcessor : GenericProcessorBase
    {
        public Db2ISeriesProcessor(IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory)
            : base(connection, factory, generator, announcer, options)
        {
            this.Quoter = new Db2Quoter();
        }

        public override string DatabaseType => "DB2 iSeries";

        public override IList<string> DatabaseTypeAliases { get; } = new List<string> { "IBM DB2 iSeries", "DB2" };

        public IQuoter Quoter
        {
            get;
            set;
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "TABLE_SCHEMA = '" + this.FormatToSafeName(schemaName) + "' AND ";

            var doesExist = this.Exists("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE {0} TABLE_NAME = '{1}' AND COLUMN_NAME='{2}'", schema, this.FormatToSafeName(tableName), this.FormatToSafeName(columnName));
            return doesExist;
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "TABLE_SCHEMA = '" + this.FormatToSafeName(schemaName) + "' AND ";

            return this.Exists("SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE {0} TABLE_NAME = '{1}' AND CONSTRAINT_NAME='{2}'", schema, this.FormatToSafeName(tableName), this.FormatToSafeName(constraintName));
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "TABLE_SCHEMA = '" + this.FormatToSafeName(schemaName) + "' AND ";
            var defaultValueAsString = string.Format("%{0}%", FormatHelper.FormatSqlEscape(defaultValue.ToString()));

            return this.Exists("SELECT COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE {0} TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}' AND COLUMN_DEFAULT LIKE '{3}'", schema, this.FormatToSafeName(tableName), columnName.ToUpper(), defaultValueAsString);
        }

        public override void Execute(string template, params object[] args)
        {
            this.Process(string.Format(template, args));
        }

        public override bool Exists(string template, params object[] args)
        {
            this.EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(string.Format(template, args), Connection, Transaction, Options))
            using (var reader = command.ExecuteReader())
            {
                return reader.Read();
            }
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "INDEX_SCHEMA = '" + this.FormatToSafeName(schemaName) + "' AND ";

            var doesExist = this.Exists(
                "SELECT NAME FROM INFORMATION_SCHEMA.SYSINDEXES WHERE {0}TABLE_NAME = '{1}' AND NAME = '{2}'",
                schema,
                this.FormatToSafeName(tableName),
                this.FormatToSafeName(indexName));

            return doesExist;
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Announcer.Say("Performing DB Operation");

            if (Options.PreviewOnly)
            {
                return;
            }

            this.EnsureConnectionIsOpen();

            if (expression.Operation != null)
            {
                expression.Operation(this.Connection, this.Transaction);
            }
        }

        public override DataSet Read(string template, params object[] args)
        {
            this.EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(string.Format(template, args), Connection, Transaction, Options))
            using (var reader = command.ExecuteReader())
            {
                return reader.ReadDataSet();
            }
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return this.Read("SELECT * FROM {0}", Quoter.QuoteTableName(tableName, schemaName));
        }

        public override bool SchemaExists(string schemaName)
        {
            return this.Exists("SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{0}'", this.FormatToSafeName(schemaName));
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "TABLE_SCHEMA = '" + this.FormatToSafeName(schemaName) + "' AND ";

            return this.Exists("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE {0}TABLE_NAME = '{1}'", schema, this.FormatToSafeName(tableName));
        }

        protected override void Process(string sql)
        {
            Announcer.Sql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
            {
                return;
            }

            this.EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(sql, Connection, Transaction, Options))
            {
                command.ExecuteNonQuery();
            }
        }

        private string FormatToSafeName(string sqlName)
        {
            var rawName = this.Quoter.UnQuote(sqlName);

            return rawName.Contains('\'') ? FormatHelper.FormatSqlEscape(rawName) : rawName.ToUpper();
        }
    }
}
