﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Dnx.Runtime;

namespace Microsoft.Dnx.Testing
{
    public class DnxSdk
    {
        public string Version { get; set; }

        public string Flavor { get; set; }

        public string Architecture { get; set; }

        public string OperationSystem { get; set; }

        public string Location { get; set; }

        public string BinDir => Path.Combine(Location, "bin");

        public string FullName { get; set; }

        public FrameworkName TargetFramework { get; set; }

        public Dnu Dnu => new Dnu(Location);

        public Dnx Dnx => new Dnx(Location);

        public static string GetRuntimeHome()
        {
            var dnxHomePath = Environment.GetEnvironmentVariable(EnvironmentNames.Home);
            var homePath = string.IsNullOrEmpty(dnxHomePath) ? null : Environment.ExpandEnvironmentVariables(dnxHomePath);

            if (string.IsNullOrEmpty(homePath))
            {
                var basePath = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(basePath))
                {
                    basePath = Environment.GetEnvironmentVariable("USERPROFILE");
                }

                homePath = System.IO.Path.Combine(basePath, ".dnx");
            }

            return homePath;
        }

        public static DnxSdk GetRuntime(string version)
        {
            return GetRuntime(version, "clr", "win", "x86");
        }

        public static DnxSdk GetRuntime(string version, string flavor, string os, string arch)
        {
            return GetRuntime(GetRuntimeHome(), version, flavor, os, arch);
        }

        public static DnxSdk GetRuntime(string basePath, string version, string flavor, string os, string arch)
        {
            var fullName = GetRuntimeName(flavor, os, arch) + $".{version}";
            return new DnxSdk
            {
                FullName = fullName,
                TargetFramework = TestUtils.GetFrameworkForRuntimeFlavor(flavor),
                Location = System.IO.Path.Combine(basePath, "runtimes", fullName),
                Architecture = arch,
                Flavor = flavor,
                OperationSystem = os,
                Version = version
            };
        }

        public static string GetRuntimeName(string flavor, string os, string architecture)
        {
            // Mono ignores os and architecture
            if (string.Equals(flavor, "mono", StringComparison.OrdinalIgnoreCase))
            {
                return "dnx-mono";
            }

            return $"dnx-{flavor}-{os}-{architecture}";
        }

        public override string ToString()
        {
            return Location;
        }
    }
}
