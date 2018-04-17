#region License
// Copyright (c) 2018, Fluent Migrator Project
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

using System;

using FluentMigrator.Runner.Generators.Generic;

namespace FluentMigrator.Runner.Generators.DB2
{
    public class Db2Quoter : GenericQuoter
    {
        public readonly char[] SpecialChars = "\"%'()*+|,{}-./:;<=>?^[]".ToCharArray();

        public override string FormatDateTime(DateTime value)
        {
            return ValueQuote + value.ToString("yyyy-MM-dd-HH.mm.ss") + ValueQuote;
        }

        protected override bool ShouldQuote(string name)
        {
            // Quotes are only included if the name contains a special character, in order to preserve case insensitivity where possible.
            return name.IndexOfAny(SpecialChars) != -1;
        }

        public override string QuoteIndexName(string indexName, string schemaName)
        {
            return CreateSchemaPrefixedQuotedIdentifier(
                QuoteSchemaName(schemaName),
                IsQuoted(indexName) ? indexName : Quote(indexName));
        }

        public override string FormatSystemMethods(SystemMethods value)
        {
            switch (value)
            {
                case SystemMethods.CurrentUTCDateTime:
                    return "(CURRENT_TIMESTAMP - CURRENT_TIMEZONE)";
                case SystemMethods.CurrentDateTime:
                    return "CURRENT_TIMESTAMP";
                case SystemMethods.CurrentUser:
                    return "USER";
            }

            return base.FormatSystemMethods(value);
        }
    }
}
