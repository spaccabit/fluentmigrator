#region License
// Copyright (c) 2007-2018, FluentMigrator Project
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

namespace FluentMigrator.Runner.Generators.MySql
{
    public class MySql5Generator : MySql4Generator
    {
        public MySql5Generator()
            : base(new MySqlColumn(new MySql5TypeMap(), new MySqlQuoter()), new MySqlQuoter(), new EmptyDescriptionGenerator())
        {
        }

        protected MySql5Generator(IColumn column, IQuoter quoter, IDescriptionGenerator descriptionGenerator)
            : base(column, quoter, descriptionGenerator)
        {
        }

    }
}
