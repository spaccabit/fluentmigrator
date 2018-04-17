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
using System.Diagnostics;
using System.IO;
using System.Reflection;

using FluentMigrator.Runner.Infrastructure.Hosts;

namespace FluentMigrator.Runner.Infrastructure
{
    public static class RuntimeHost
    {
        private static readonly string[] _noNames = new string[0];

#if NETFRAMEWORK
        private static readonly IHostAbstraction _currentHost = new NetFrameworkHost();
#else
        private static readonly IHostAbstraction _currentHost = new NetCoreHost();
#endif

        public static IHostAbstraction Current => _currentHost;

        public static IEnumerable<AssemblyName> FindAssemblies()
        {
            foreach (var fullGacDirectory in GetFullGacDirectories())
            {
                foreach (var directory in Directory.EnumerateDirectories(fullGacDirectory))
                {
                    var asmBaseName = Path.GetFileName(directory);
                    foreach (var asmName in GetAssemblyNames(fullGacDirectory, asmBaseName))
                    {
                        yield return asmName;
                    }
                }
            }
        }

        public static IEnumerable<AssemblyName> FindAssemblies(string name)
        {
            foreach (var assemblyName in FindAssemblies())
            {
                if (string.Equals(assemblyName.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return assemblyName;
            }
        }

        private static IEnumerable<AssemblyName> GetAssemblyNames(string fullGacDirectory, string assemblyName)
        {
            foreach (var fullPath in Directory.EnumerateDirectories(Path.Combine(fullGacDirectory, assemblyName)))
            {
                var versionInfo = Path.GetFileName(fullPath);
                // Console.WriteLine(versionInfo);

                Version asmVersion;
                string culture;
                string pkToken;

                Debug.Assert(versionInfo != null, nameof(versionInfo) + " != null");
                var parts = versionInfo.Split('_');
                if (parts.Length < 3)
                    continue;

                try
                {
                    if (!parts[0].StartsWith("v"))
                    {
                        asmVersion = Version.Parse(parts[0]);
                        culture = parts[1];
                        pkToken = parts[2];
                    }
                    else
                    {
                        asmVersion = Version.Parse(parts[1]);
                        culture = parts[2];
                        pkToken = parts[3];
                    }
                }
                catch
                {
                    // Ignore errors
                    continue;
                }

                if (string.IsNullOrEmpty(culture))
                {
                    culture = "neutral";
                }

                var asmName = $"{assemblyName}, Version={asmVersion}, Culture={culture}, PublicKeyToken={pkToken}";
                yield return new AssemblyName(asmName);
            }
        }

        private static IEnumerable<string> GetFullGacDirectories()
        {
            var winDir = Environment.GetEnvironmentVariable("WINDIR");
            if (!string.IsNullOrEmpty(winDir))
            {
                return GetFullGacDirectoriesOnWindows(winDir);
            }

            var asmPath = typeof(Int32).Assembly.Location;
            var isMono = asmPath.Contains("/mono/");
            if (!isMono)
                return _noNames;

            var frameworkDir = Path.GetDirectoryName(asmPath);
            var gacDir = Path.Combine(Path.GetDirectoryName(frameworkDir), "gac");
            if (!Directory.Exists(gacDir))
                return _noNames;

            return new[] { gacDir };
        }

        private static IEnumerable<string> GetFullGacDirectoriesOnWindows(string winDir)
        {
            var netAssemblyPaths = new []
            {
                Path.Combine(winDir, "Microsoft.NET", "assembly"),
                Path.Combine(winDir, "assembly"),
            };

            var gacDirs = GetGacDirectories();
            foreach (var netAssemblyPath in netAssemblyPaths)
            {
                foreach (var gacDir in gacDirs)
                {
                    var fullGacDir = Path.Combine(netAssemblyPath, gacDir);
                    if (!Directory.Exists(fullGacDir))
                        continue;
                    yield return fullGacDir;
                }
            }
        }

        private static string[] GetGacDirectories()
        {
            if (Environment.Is64BitProcess)
                return new[] { "GAC_MSIL", "GAC_64", "NativeImages_v2.0.50727_64", "GAC" };
            return new[] { "GAC_MSIL", "GAC_32", "NativeImages_v2.0.50727_32", "GAC" };
        }
    }
}
