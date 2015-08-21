﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Dnx.CommonTestUtils;
using Xunit;

namespace Microsoft.Dnx.Tooling
{
    public class DnuPackTests
    {
        public static IEnumerable<object[]> RuntimeComponents
        {
            get
            {
                return TestUtils.GetRuntimeComponentsCombinations();
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_P2PDifferentFrameworks(string flavor, string os, string architecture)
        {
            string stdOut;
            string stdError;
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);

            using (var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                var p1 = Path.Combine(testEnv.RootDir, "P1");
                var p2 = Path.Combine(testEnv.RootDir, "P2");

                Directory.CreateDirectory(p1);
                Directory.CreateDirectory(p2);

                File.WriteAllText($"{p1}/project.json",
                @"{
                    ""dependencies"": {
                        ""System.Runtime"":""4.0.20-*""
                    },
                    ""frameworks"": {
                        ""dotnet"": {}
                    }
                  }");

                File.WriteAllText($"{p1}/BaseClass.cs", @"
public class BaseClass {
    public virtual void Test() { }
}");

                File.WriteAllText($"{p2}/project.json",
                @"{
                    ""dependencies"": {
                        ""P1"":""""
                    },
                    ""frameworks"": {
                        ""dnxcore50"": {}
                    }
                  }");
                File.WriteAllText($"{p2}/TestClass.cs", @"
public class TestClass : BaseClass {
    public override void Test() { }
}");

                var environment = new Dictionary<string, string> { { "DNX_TRACE", "0" } };
                DnuTestUtils.ExecDnu(runtimeHomeDir, "restore", "", out stdOut, out stdError, environment: null, workingDir: testEnv.RootDir);
                var exitCode = DnuTestUtils.ExecDnu(runtimeHomeDir, "pack", "", out stdOut, out stdError, environment, p2);
                Assert.Equal(0, exitCode);
            }
        }
        
        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_AdditionalFiles(string flavor, string os, string architecture)
        {
            const string dirTree = @"{
    ""."": [""project.json""],
    ""theTools"": {
        ""."": [""install.ps1""],
        ""sub"": [""support.ps1""]
    }
}";

            const string projectJson = @"{
    ""version"": ""1.0.0"",
    ""frameworks"": { ""dnx451"": {} },
    ""packageFiles"": {
        ""tools"": ""theTools/**/*.ps1""
    }   
}";

            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            string[] entries;
            using(var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                DirTree.CreateFromJson(dirTree)
                    .WithFileContents("project.json", projectJson)
                    .WriteTo(testEnv.ProjectPath);

                DnuTestUtils.ExecDnu(runtimeHomeDir, "restore", "", workingDir: testEnv.RootDir);
                DnuTestUtils.ExecDnu(runtimeHomeDir, "pack", $"--out {testEnv.PublishOutputDirPath}", workingDir: testEnv.ProjectPath);

                // Open the produced package
                using (var archive = ZipFile.OpenRead(Path.Combine(testEnv.PublishOutputDirPath, "Debug", $"{testEnv.ProjectName}.1.0.0.nupkg")))
                {
                    entries = archive.Entries.Select(e => e.FullName).Where(IsNotOpcMetadata).ToArray();
                }
            }

            // Check it
            Assert.Equal(new[]
            {
                "ProjectName.nuspec",
                "lib/dnx451/ProjectName.dll",
                "lib/dnx451/ProjectName.xml",
                "tools/install.ps1",
                "tools/sub/support.ps1"
            }, entries);
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_NoArgs(string flavor, string os, string architecture)
        {
            string expectedDNX =
            @"Building {0} for DNX,Version=v4.5.1
  Using Project dependency {0} 1.0.0
    Source: {1}/project.json".Replace('/', Path.DirectorySeparatorChar);
            string expectedDNXCore =
            @"Building {0} for DNXCore,Version=v5.0
  Using Project dependency {0} 1.0.0
    Source: {1}/project.json".Replace('/', Path.DirectorySeparatorChar);
            string expectedNupkg =
            @"{0} -> {1}/bin/Debug/{0}.1.0.0.nupkg
{0} -> {1}/bin/Debug/{0}.1.0.0.symbols.nupkg".Replace('/', Path.DirectorySeparatorChar);
            string stdOut;
            string stdError;
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            int exitCode;

            using (var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                File.WriteAllText($"{testEnv.RootDir}/project.json",
                @"{
                    ""frameworks"": {
                        ""dnx451"": {
                        },
                        ""dnxcore50"": {
                            ""dependencies"": {
                                ""System.Runtime"":""4.0.20-*""
                            }
                        }
                    }
                  }");
                var environment = new Dictionary<string, string> { { "DNX_TRACE", "0" } };
                DnuTestUtils.ExecDnu(runtimeHomeDir, "restore", "", out stdOut, out stdError, environment: null, workingDir: testEnv.RootDir);
                exitCode = DnuTestUtils.ExecDnu(runtimeHomeDir, "pack", "", out stdOut, out stdError, environment, testEnv.RootDir);

                Assert.Empty(stdError);
                Assert.Contains(string.Format(expectedDNX, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.Contains(string.Format(expectedDNXCore, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.Contains(string.Format(expectedNupkg, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.Equal(0, exitCode);
                Assert.True(Directory.Exists($"{testEnv.RootDir}/bin"));
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_FrameworkSpecified(string flavor, string os, string architecture)
        {
            string expectedDNX =
            @"Building {0} for DNX,Version=v4.5.1
  Using Project dependency {0} 1.0.0
    Source: {1}/project.json".Replace('/', Path.DirectorySeparatorChar);
            string expectedNupkg =
            @"{0} -> {1}/bin/Debug/{0}.1.0.0.nupkg
{0} -> {1}/bin/Debug/{0}.1.0.0.symbols.nupkg".Replace('/', Path.DirectorySeparatorChar);
            string stdOut;
            string stdError;
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            int exitCode;

            using (var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                File.WriteAllText($"{testEnv.RootDir}/project.json",
                @"{
                    ""frameworks"": {
                        ""dnx451"": {
                        },
                        ""dnxcore50"": {
                            ""dependencies"": {
                                ""System.Runtime"":""4.0.20-*""
                            }
                        }
                    }
                  }");
                var environment = new Dictionary<string, string> { { "DNX_TRACE", "0" } };
                DnuTestUtils.ExecDnu(runtimeHomeDir, "restore", "", out stdOut, out stdError, environment: null, workingDir: testEnv.RootDir);
                exitCode = DnuTestUtils.ExecDnu(runtimeHomeDir, "pack", "--framework dnx451", out stdOut, out stdError, environment, testEnv.RootDir);

                Assert.Empty(stdError);
                Assert.Contains(string.Format(expectedDNX, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.DoesNotContain("DNXCore", stdOut);
                Assert.Contains(string.Format(expectedNupkg, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.Equal(0, exitCode);
                Assert.True(Directory.Exists($"{testEnv.RootDir}/bin"));
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_OutPathSpecified(string flavor, string os, string architecture)
        {
            string expectedNupkg =
            @"{0} -> {1}/CustomOutput/Debug/{0}.1.0.0.nupkg".Replace('/', Path.DirectorySeparatorChar);
            string expectedSymbol =
            @"{0} -> {1}/CustomOutput/Debug/{0}.1.0.0.symbols.nupkg".Replace('/', Path.DirectorySeparatorChar);
            string stdOut;
            string stdError;
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            int exitCode;

            using (var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                File.WriteAllText($"{testEnv.RootDir}/project.json",
                @"{
                    ""frameworks"": {
                        ""dnx451"": {
                        },
                        ""dnxcore50"": {
                            ""dependencies"": {
                                ""System.Runtime"":""4.0.20-*""
                            }
                        }
                    }
                  }");
                DnuTestUtils.ExecDnu(runtimeHomeDir, "restore", "", out stdOut, out stdError, environment: null, workingDir: testEnv.RootDir);
                exitCode = DnuTestUtils.ExecDnu(runtimeHomeDir, "pack", "--out CustomOutput", out stdOut, out stdError, environment: null, workingDir: testEnv.RootDir);

                Assert.Empty(stdError);
                Assert.Contains(string.Format(expectedNupkg, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.Contains(string.Format(expectedSymbol, Path.GetFileName(testEnv.RootDir), testEnv.RootDir), stdOut);
                Assert.Equal(0, exitCode);
                Assert.True(Directory.Exists($"{testEnv.RootDir}/CustomOutput"));
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_DoesNotExecutePostBuildScriptWhenBuildFails(string flavor, string os, string architecture)
        {
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            var projectJson = @"{
  ""scripts"": {
    ""postbuild"": ""echo POST_BUILD_SCRIPT_OUTPUT"",
    ""postpack"": ""echo POST_PACK_SCRIPT_OUTPUT""
  },
}";
            var sourceFileContents = @"Invalid source code that makes build fail";

            using (var tempDir = new DisposableDir())
            {
                var projectJsonPath = Path.Combine(tempDir, Runtime.Project.ProjectFileName);
                var sourceFilePath = Path.Combine(tempDir, "Program.cs");
                File.WriteAllText(projectJsonPath, projectJson);
                File.WriteAllText(sourceFilePath, sourceFileContents);

                string stdOut, stdErr;
                var exitCode = DnuTestUtils.ExecDnu(
                    runtimeHomeDir,
                    "pack",
                    projectJsonPath,
                    out stdOut,
                    out stdErr);

                Assert.NotEqual(0, exitCode);
                Assert.NotEmpty(stdErr);
                Assert.DoesNotContain("POST_BUILD_SCRIPT_OUTPUT", stdOut);
                Assert.DoesNotContain("POST_PACK_SCRIPT_OUTPUT", stdOut);
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuPack_ShowUnresolvedDependencyWhenBuildFails(string flavor, string os, string architecture)
        {
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            var projectJson = @"{
  ""frameworks"": {
    ""dnx451"": {
      ""dependencies"": {
      ""NonexistentPackage"": ""1.0.0""
      }
    }
  }
}";

            using (var tempDir = new DisposableDir())
            {
                var projectPath = Path.Combine(tempDir, "Project");
                var emptyLocalFeed = Path.Combine(tempDir, "EmptyLocalFeed");
                Directory.CreateDirectory(projectPath);
                Directory.CreateDirectory(emptyLocalFeed);
                var projectJsonPath = Path.Combine(projectPath, Runtime.Project.ProjectFileName);
                File.WriteAllText(projectJsonPath, projectJson);

                string stdOut, stdErr;
                var exitCode = DnuTestUtils.ExecDnu(
                    runtimeHomeDir,
                    "restore",
                    $"{projectJsonPath} -s {emptyLocalFeed}",
                    out stdOut,
                    out stdErr);
                Assert.NotEqual(0, exitCode);

                exitCode = DnuTestUtils.ExecDnu(
                    runtimeHomeDir,
                    "pack",
                    projectJsonPath,
                    out stdOut,
                    out stdErr);

                Assert.NotEqual(0, exitCode);
                Assert.NotEmpty(stdErr);
                var unresolvedDependencyErrorCount = stdErr
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Contains("The dependency NonexistentPackages >= 1.0.0 could not be resolved"))
                    .Count();
                Assert.Equal(1, unresolvedDependencyErrorCount);
            }
        }

        private static readonly HashSet<string> OpcMetadataPaths = new HashSet<string>()
        {
            "_rels/.rels",
            "[Content_Types].xml"
        };

        private bool IsNotOpcMetadata(string arg)
        {
            return !OpcMetadataPaths.Contains(arg);
        }
    }
}