﻿#region License
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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FluentMigrator
{
    public static class SqlScriptTokenReplacer
    {
        public static string ReplaceSqlScriptTokens(string sqlText, IDictionary<string, string> parameters)
        {
            // since all the Processors are using String.Format() in their Execute method
            //  we need to escape the brackets with double brackets or else it throws an incorrect format error on the String.Format call
            sqlText = sqlText.Replace("{", "{{").Replace("}", "}}");

            // Are parameters set?
            if (parameters != null && parameters.Count != 0)
            {
                // Replace $(word) elements with values stored
                // in the Parameters dictionary.
                sqlText = Regex.Replace(
                    sqlText,
                    @"\$\((?<token>\w+)\)",
                    m =>
                    {
                        var key = m.Groups["token"].Value;
                        if (parameters.TryGetValue(key, out var keyValue))
                        {
                            return keyValue;
                        }

                        // Return the whole match value when the key
                        // wasn't found in the Parameters dictionary.
                        // This might help finding unset variables.
                        return m.Value;
                    }
                );

                // Replace $$((word)) with $(word)
                sqlText = Regex.Replace(sqlText, @"\${2}\({2}(\w+)\){2}", @"$$($1)");
            }

            return sqlText;
        }
    }
}
