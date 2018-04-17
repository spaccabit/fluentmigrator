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

using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Model;

namespace FluentMigrator.Builders.Create.Index
{
    public class CreateIndexExpressionBuilder : ExpressionBuilderBase<CreateIndexExpression>,
        ICreateIndexForTableSyntax,
        ICreateIndexOnColumnOrInSchemaSyntax,
        ICreateIndexColumnOptionsSyntax,
        ICreateIndexOptionsSyntax,
        ISupportAdditionalFeatures,
        ICreateIndexColumnUniqueOptionsSyntax,
        ICreateIndexMoreColumnOptionsSyntax
    {
        public CreateIndexExpressionBuilder(CreateIndexExpression expression)
            : base(expression)
        {
        }

        public IDictionary<string, object> AdditionalFeatures => Expression.Index.AdditionalFeatures;

        public IndexColumnDefinition CurrentColumn { get; set; }

        public ICreateIndexOnColumnOrInSchemaSyntax OnTable(string tableName)
        {
            Expression.Index.TableName = tableName;
            return this;
        }

        public ICreateIndexColumnOptionsSyntax OnColumn(string columnName)
        {
            CurrentColumn = new IndexColumnDefinition { Name = columnName };
            Expression.Index.Columns.Add(CurrentColumn);
            return this;
        }

        public ICreateIndexOptionsSyntax WithOptions()
        {
            return this;
        }

        public ICreateIndexOnColumnSyntax InSchema(string schemaName)
        {
            Expression.Index.SchemaName = schemaName;
            return this;
        }

        public ICreateIndexMoreColumnOptionsSyntax Ascending()
        {
            CurrentColumn.Direction = Direction.Ascending;
            return this;
        }

        public ICreateIndexMoreColumnOptionsSyntax Descending()
        {
            CurrentColumn.Direction = Direction.Descending;
            return this;
        }

        public ICreateIndexColumnUniqueOptionsSyntax Unique()
        {
            Expression.Index.IsUnique = true;
            return this;
        }

        ICreateIndexOnColumnSyntax ICreateIndexOptionsSyntax.Unique()
        {
            Expression.Index.IsUnique = true;
            return this;
        }

        public ICreateIndexOnColumnSyntax NonClustered()
        {
            Expression.Index.IsClustered = false;
            return this;
        }

        public ICreateIndexOnColumnSyntax Clustered()
        {
            Expression.Index.IsClustered = true;
            return this;
        }
    }
}
