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

using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Model;

namespace FluentMigrator.Runner.Models
{
    internal class FirebirdTableDefinition
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public ICollection<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
        public ICollection<ForeignKeyDefinition> ForeignKeys { get; set; } = new List<ForeignKeyDefinition>();
        public ICollection<IndexDefinition> Indexes { get; set; } = new List<IndexDefinition>();
    }
}
