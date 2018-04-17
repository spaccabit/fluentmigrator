#region License
// Copyright (c) 2007-2018, Sean Chambers and the FluentMigrator Project
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using McMaster.Extensions.CommandLineUtils;

namespace FluentMigrator.DotNet.Cli.Commands
{
    public class MigrationCommand : BaseCommand
    {
        [Option("-a|--assembly <ASSEMBLY_NAME>", Description = "The assemblies containing the migrations you want to execute.")]
        [Required]
        public IEnumerable<string> TargetAssemblies { get; }

        [Option("-n|--namespace <NAMESPACE>", Description = "The namespace contains the migrations you want to run. Default is all migrations found within the Target Assembly will be run.")]
        public string Namespace { get; }

        [Option("--nested", Description = "Whether migrations in nested namespaces should be included. Used in conjunction with the namespace option.")]
        public bool NestedNamespaces { get; }

        [Option("--start-version", Description = "The specific version to start migrating from. Only used when NoConnection is true. Default is 0.")]
        public long? StartVersion { get; }

        [Option("--working-directory <WORKING_DIRECTORY>", Description = "The directory to load SQL scripts specified by migrations from.")]
        public string WorkingDirectory { get; }

        [Option("-t|--tag", Description = "Filters the migrations to be run by tag.")]
        public IEnumerable<string> Tags { get; }
    }
}
