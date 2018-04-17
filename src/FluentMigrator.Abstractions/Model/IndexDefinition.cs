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

using System;
using System.Collections.Generic;
using System.Linq;

using FluentMigrator.Infrastructure;
using FluentMigrator.Infrastructure.Extensions;

namespace FluentMigrator.Model
{
    public class IndexDefinition : ICloneable, ICanBeValidated, ISupportAdditionalFeatures
    {
        private readonly IDictionary<string, object> _additionalFeatures = new Dictionary<string, object>();

        public virtual string Name { get; set; }
        public virtual string SchemaName { get; set; }
        public virtual string TableName { get; set; }
        public virtual bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
        public virtual ICollection<IndexColumnDefinition> Columns { get; set; }

        public IndexDefinition()
        {
            Columns = new List<IndexColumnDefinition>();
        }

        public virtual IDictionary<string, object> AdditionalFeatures => _additionalFeatures;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (String.IsNullOrEmpty(Name))
                errors.Add(ErrorMessages.IndexNameCannotBeNullOrEmpty);

            if (String.IsNullOrEmpty(TableName))
                errors.Add(ErrorMessages.TableNameCannotBeNullOrEmpty);

            if (Columns.Count == 0)
                errors.Add(ErrorMessages.IndexMustHaveOneOrMoreColumns);

            foreach (IndexColumnDefinition column in Columns)
                column.CollectValidationErrors(errors);

            foreach (var additionalFeature in _additionalFeatures.Select(x => x.Value).OfType<ICanBeValidated>())
                additionalFeature.CollectValidationErrors(errors);
        }

        public object Clone()
        {
            var result = new IndexDefinition
            {
                Name = Name,
                SchemaName = SchemaName,
                TableName = TableName,
                IsUnique = IsUnique,
                IsClustered = IsClustered,
                Columns = Columns.CloneAll().ToList(),
            };

            _additionalFeatures.CloneTo(result._additionalFeatures);

            return result;
        }
    }
}
