﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Dnx.Runtime.Helpers;
using NuGet;
using Xunit;

namespace Microsoft.Dnx.Runtime.Tests
{
    public class GacDependencyResolverFacts
    {
        // NOTE(anurse): Disabling tests for frameworks < .NET 4.0 because the CI doesn't have them installed :(
        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData("mscorlib", "4.0.0.0", "dnx451", @"%Windir%\Microsoft.NET\assembly\GAC_32\mscorlib\v4.0_4.0.0.0__b77a5c561934e089\mscorlib.dll", true)]
        //[InlineData("mscorlib", "2.0.0.0", "net20", @"%Windir%\assembly\GAC_32\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll", true)]
        [InlineData("mscorlib", "4.0.0.0", "net20", "", false)]
        [InlineData("mscorlib", "2.0.0.0", "dnx451", "", false)]
        [InlineData("mscorlib", "1.0.0.0", "dnx451", "", false)]
        [InlineData("mscorlib", "4.0.0.0", "dnxcore50", "", false)]
        public void GetDescriptionReturnsCorrectResults(string name, string version, string framework, string path, bool found)
        {
            var libraryRange = new LibraryRange(name, frameworkReference: true)
            {
                VersionRange = VersionUtility.ParseVersionRange(version)
            };

            var frameworkName = FrameworkNameHelper.ParseFrameworkName(framework);
            var resolver = new GacDependencyResolver();
            var library = resolver.GetDescription(libraryRange, frameworkName);

            if (found)
            {
                Assert.NotNull(library);
                Assert.Equal(SemanticVersion.Parse(version), library.Identity.Version);
                Assert.Equal(Environment.ExpandEnvironmentVariables(path), library.Path);
            }
            else
            {
                Assert.Null(library);
            }
        }
    }
}
