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

using System;

using FluentMigrator.Builders.Create.Constraint;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.SqlAnywhere
{
    public static partial class SqlAnywhereExtensions
    {
        public const string ConstraintType = "SqlAnywhereConstraintType";
        public const string SchemaPassword = "SqlAnywhereSchemaPassword";
        public const string WithNullsDistinct = "SqlAnywhereNullsDistinct";

        private static void SetConstraintType(ICreateConstraintOptionsSyntax expression, SqlAnywhereConstraintType type)
        {
            if (!(expression is ISupportAdditionalFeatures additionalFeatures))
                throw new InvalidOperationException(UnsupportedMethodMessage(type, nameof(ISupportAdditionalFeatures)));

            additionalFeatures.AdditionalFeatures[ConstraintType] = type;
        }

        public static void Clustered(this ICreateConstraintOptionsSyntax expression)
        {
            SetConstraintType(expression, SqlAnywhereConstraintType.Clustered);
        }

        public static void NonClustered(this ICreateConstraintOptionsSyntax expression)
        {
            SetConstraintType(expression, SqlAnywhereConstraintType.NonClustered);
        }

        private static string UnsupportedMethodMessage(object methodName, string interfaceName)
        {
            var msg = string.Format(ErrorMessages.MethodXMustBeCalledOnObjectImplementingY, methodName, interfaceName);
            return msg;
        }
    }
}
