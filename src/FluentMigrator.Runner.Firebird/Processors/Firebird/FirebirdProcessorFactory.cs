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

using FluentMigrator.Runner.Generators.Firebird;

namespace FluentMigrator.Runner.Processors.Firebird
{
    public class FirebirdProcessorFactory : MigrationProcessorFactory
    {
        public FirebirdOptions FbOptions { get; set; }

        public FirebirdProcessorFactory() : this(FirebirdOptions.AutoCommitBehaviour()) { }
        public FirebirdProcessorFactory(FirebirdOptions fbOptions)
        {
            FbOptions = fbOptions ?? throw new ArgumentNullException(nameof(fbOptions));
        }

        public override IMigrationProcessor Create(string connectionString, IAnnouncer announcer, IMigrationProcessorOptions options)
        {
            var fbOpt = ((FirebirdOptions) FbOptions.Clone())
                .ApplyProviderSwitches(options.ProviderSwitches);
            var factory = new FirebirdDbFactory();
            var connection = factory.CreateConnection(connectionString);
            return new FirebirdProcessor(connection, new FirebirdGenerator(FbOptions), announcer, options, factory, fbOpt);
        }
    }
}
