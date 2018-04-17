#region License
// Copyright (c) 2018, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Generators.SQLite;
using FluentMigrator.Runner.Processors.SQLite;

using NUnit.Framework;

namespace FluentMigrator.Tests.Unit.Processors.SQLite
{
    [Category("SQLite")]
    public class BatchParserTests : ProcessorBatchParserTestsBase
    {
        protected override IMigrationProcessor CreateProcessor()
        {
            return new SQLiteProcessor(
                MockedConnection.Object,
                new SQLiteGenerator(),
                new NullAnnouncer(),
                ProcessorOptions,
                MockedDbFactory.Object);
        }
    }
}
