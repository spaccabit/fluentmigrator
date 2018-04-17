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

namespace FluentMigrator.Runner.Processors.DB2
{
    using FluentMigrator.Runner.Generators.DB2;

    public class Db2ProcessorFactory : MigrationProcessorFactory
    {
        #region Methods

        public override IMigrationProcessor Create(string connectionString, IAnnouncer announcer, IMigrationProcessorOptions options)
        {
            var factory = new Db2DbFactory();
            var connection = factory.CreateConnection(connectionString);
            return new Db2Processor(connection, new Db2Generator(new Db2Quoter()), announcer, options, factory);
        }

        #endregion Methods
    }
}
