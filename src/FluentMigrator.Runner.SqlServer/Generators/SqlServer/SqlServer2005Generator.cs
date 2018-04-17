#region License
//
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

using System.Text;

using FluentMigrator.Infrastructure;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.SqlServer;

namespace FluentMigrator.Runner.Generators.SqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentMigrator.Expressions;
    using FluentMigrator.Model;

    public class SqlServer2005Generator : SqlServer2000Generator
    {
        private static readonly HashSet<string> _supportedAdditionalFeatures = new HashSet<string>
        {
            SqlServerExtensions.IncludesList,
            SqlServerExtensions.OnlineIndex,
            SqlServerExtensions.RowGuidColumn,
        };

        private static readonly IQuoter _quoter = new SqlServer2005Quoter();

        public SqlServer2005Generator()
            : base(new SqlServer2005Column(new SqlServer2005TypeMap(), _quoter), _quoter, new SqlServer2005DescriptionGenerator())
        {
        }

        protected SqlServer2005Generator(IColumn column, IQuoter quoter, IDescriptionGenerator descriptionGenerator)
            : base(column, quoter, descriptionGenerator)
        {
        }

        public override string AddColumn { get { return "ALTER TABLE {0} ADD {1}"; } }

        public override string CreateIndex { get { return "CREATE {0}{1}INDEX {2} ON {3} ({4}{5}{6}){7}"; } }
        public override string DropIndex { get { return "DROP INDEX {0} ON {1}{2}"; } }

        public override string IdentityInsert { get { return "SET IDENTITY_INSERT {0} {1}"; } }

        public override string CreateForeignKeyConstraint { get { return "ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3} ({4}){5}{6}"; } }

        public virtual string GetIncludeString(CreateIndexExpression column)
        {
            var includes = column.GetAdditionalFeature<IList<IndexIncludeDefinition>>(SqlServerExtensions.IncludesList);
            return includes?.Count > 0 ? ") INCLUDE (" : string.Empty;
        }

        public virtual string GetWithOptions(ISupportAdditionalFeatures expression)
        {
            var items = new List<string>();
            var isOnline = expression.GetAdditionalFeature(SqlServerExtensions.OnlineIndex, (bool?)null);
            if (isOnline.HasValue)
            {
                items.Add($"ONLINE={(isOnline.Value ? "ON" : "OFF")}");
            }

            return string.Join(", ", items);
        }

        public override bool IsAdditionalFeatureSupported(string feature)
        {
            return _supportedAdditionalFeatures.Contains(feature)
             || base.IsAdditionalFeatureSupported(feature);
        }

        public override string Generate(CreateTableExpression expression)
        {
            var descriptionStatements = DescriptionGenerator.GenerateDescriptionStatements(expression);
            var createTableStatement = base.Generate(expression);
            var descriptionStatementsArray = descriptionStatements as string[] ?? descriptionStatements.ToArray();

            if (!descriptionStatementsArray.Any())
                return createTableStatement;

            return ComposeStatements(createTableStatement, descriptionStatementsArray);
        }

        public override string Generate(AlterTableExpression expression)
        {
            var descriptionStatement = DescriptionGenerator.GenerateDescriptionStatement(expression);

            if (string.IsNullOrEmpty(descriptionStatement))
                return base.Generate(expression);

            return descriptionStatement;
        }

        public override string Generate(CreateColumnExpression expression)
        {
            var alterTableStatement = base.Generate(expression);
            var descriptionStatement = DescriptionGenerator.GenerateDescriptionStatement(expression);

            if (string.IsNullOrEmpty(descriptionStatement))
                return alterTableStatement;

            return ComposeStatements(alterTableStatement, new[] { descriptionStatement });
        }

        public override string Generate(AlterColumnExpression expression)
        {
            var alterTableStatement = base.Generate(expression);
            var descriptionStatement = DescriptionGenerator.GenerateDescriptionStatement(expression);

            if (string.IsNullOrEmpty(descriptionStatement))
                return alterTableStatement;

            return ComposeStatements(alterTableStatement, new[] { descriptionStatement });
        }

        public override string Generate(CreateForeignKeyExpression expression)
        {
            if (expression.ForeignKey.PrimaryColumns.Count != expression.ForeignKey.ForeignColumns.Count)
            {
                throw new ArgumentException("Number of primary columns and secondary columns must be equal");
            }

            List<string> primaryColumns = new List<string>();
            List<string> foreignColumns = new List<string>();
            foreach (var column in expression.ForeignKey.PrimaryColumns)
            {
                primaryColumns.Add(Quoter.QuoteColumnName(column));
            }

            foreach (var column in expression.ForeignKey.ForeignColumns)
            {
                foreignColumns.Add(Quoter.QuoteColumnName(column));
            }
            return string.Format(
                CreateForeignKeyConstraint,
                Quoter.QuoteTableName(expression.ForeignKey.ForeignTable, expression.ForeignKey.ForeignTableSchema),
                Quoter.QuoteColumnName(expression.ForeignKey.Name),
                String.Join(", ", foreignColumns.ToArray()),
                Quoter.QuoteTableName(expression.ForeignKey.PrimaryTable, expression.ForeignKey.PrimaryTableSchema),
                String.Join(", ", primaryColumns.ToArray()),
                Column.FormatCascade("DELETE", expression.ForeignKey.OnDelete),
                Column.FormatCascade("UPDATE", expression.ForeignKey.OnUpdate)
                );
        }

        public override string Generate(CreateIndexExpression expression)
        {
            string[] indexColumns = new string[expression.Index.Columns.Count];
            IndexColumnDefinition columnDef;


            for (int i = 0; i < expression.Index.Columns.Count; i++)
            {
                columnDef = expression.Index.Columns.ElementAt(i);
                if (columnDef.Direction == Direction.Ascending)
                {
                    indexColumns[i] = Quoter.QuoteColumnName(columnDef.Name) + " ASC";
                }
                else
                {
                    indexColumns[i] = Quoter.QuoteColumnName(columnDef.Name) + " DESC";
                }
            }

            var includes = expression.Index.GetAdditionalFeature<IList<IndexIncludeDefinition>>(SqlServerExtensions.IncludesList);
            string[] indexIncludes = new string[includes?.Count ?? 0];

            if (includes != null)
            {
                for (int i = 0; i != includes.Count; i++)
                {
                    var includeDef = includes[i];
                    indexIncludes[i] = Quoter.QuoteColumnName(includeDef.Name);
                }
            }

            var withParts = GetWithOptions(expression);
            var withPart = !string.IsNullOrEmpty(withParts)
                ? $" WITH ({withParts})"
                : string.Empty;

            var result = string.Format(
                CreateIndex,
                GetUniqueString(expression),
                GetClusterTypeString(expression),
                Quoter.QuoteIndexName(expression.Index.Name),
                Quoter.QuoteTableName(expression.Index.TableName, expression.Index.SchemaName),
                String.Join(", ", indexColumns),
                GetIncludeString(expression),
                String.Join(", ", indexIncludes),
                withPart);

            return result;
        }

        public override string Generate(DeleteIndexExpression expression)
        {
            var withParts = GetWithOptions(expression);
            var withPart = !string.IsNullOrEmpty(withParts)
                ? $" WITH ({withParts})"
                : string.Empty;

            return string.Format(
                DropIndex,
                Quoter.QuoteIndexName(expression.Index.Name),
                Quoter.QuoteTableName(expression.Index.TableName, expression.Index.SchemaName),
                withPart);
        }

        public override string Generate(CreateConstraintExpression expression)
        {
            var withParts = GetWithOptions(expression);
            var withPart = !string.IsNullOrEmpty(withParts)
                ? $" WITH ({withParts})"
                : string.Empty;

            return $"{base.Generate(expression)}{withPart}";
        }

        public override string Generate(DeleteDefaultConstraintExpression expression)
        {
            string sql =
                "DECLARE @default sysname, @sql nvarchar(max);" + Environment.NewLine + Environment.NewLine +
                "-- get name of default constraint" + Environment.NewLine +
                "SELECT @default = name" + Environment.NewLine +
                "FROM sys.default_constraints" + Environment.NewLine +
                "WHERE parent_object_id = object_id('{0}')" + Environment.NewLine +
                "AND type = 'D'" + Environment.NewLine +
                "AND parent_column_id = (" + Environment.NewLine +
                "SELECT column_id" + Environment.NewLine +
                "FROM sys.columns" + Environment.NewLine +
                "WHERE object_id = object_id('{0}')" + Environment.NewLine +
                "AND name = '{1}'" + Environment.NewLine +
                ");" + Environment.NewLine + Environment.NewLine +
                "-- create alter table command to drop constraint as string and run it" + Environment.NewLine +
                "SET @sql = N'ALTER TABLE {0} DROP CONSTRAINT ' + QUOTENAME(@default);" + Environment.NewLine +
                "EXEC sp_executesql @sql;";
            return string.Format(sql, Quoter.QuoteTableName(expression.TableName, expression.SchemaName), expression.ColumnName);
        }

        public override string Generate(DeleteConstraintExpression expression)
        {
            var withParts = GetWithOptions(expression);
            var withPart = !string.IsNullOrEmpty(withParts)
                ? $" WITH ({withParts})"
                : string.Empty;

            return $"{base.Generate(expression)}{withPart}";
        }

        public override string Generate(CreateSchemaExpression expression)
        {
            return String.Format(CreateSchema, Quoter.QuoteSchemaName(expression.SchemaName));
        }

        public override string Generate(DeleteSchemaExpression expression)
        {
            return String.Format(DropSchema, Quoter.QuoteSchemaName(expression.SchemaName));
        }

        public override string Generate(AlterSchemaExpression expression)
        {
            return string.Format(AlterSchema, Quoter.QuoteSchemaName(expression.DestinationSchemaName), Quoter.QuoteTableName(expression.TableName, expression.SourceSchemaName));
        }

        private string ComposeStatements(string ddlStatement, IEnumerable<string> otherStatements)
        {
            var otherStatementsArray = otherStatements.ToArray();

            var statementsBuilder = new StringBuilder();
            statementsBuilder.AppendLine(ddlStatement);
            statementsBuilder.AppendLine("GO");
            statementsBuilder.AppendLine(string.Join(";", otherStatementsArray));

            return statementsBuilder.ToString();
        }
    }
}
