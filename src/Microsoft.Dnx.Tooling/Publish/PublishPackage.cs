// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using NuGet;

namespace Microsoft.Dnx.Tooling.Publish
{
    public class PublishPackage
    {
        private readonly PackageDescription _package;

        public PublishPackage(PackageDescription package)
        {
            _package = package;
        }

        public LibraryIdentity Library { get { return _package.Identity; } }

        public string TargetPath { get; private set; }

        public async Task Emit(PublishRoot root)
        {
            root.Reports.Quiet.WriteLine("Using {0} dependency {1}", _package.Identity.Type, Library.Name);

            var srcPackagePathResolver = new DefaultPackagePathResolver(root.SourcePackagesPath);
            var targetPackagePathResolver = new DefaultPackagePathResolver(root.TargetPackagesPath);
            var srcPackageDir = srcPackagePathResolver.GetInstallPath(
                _package.Identity.Name,
                _package.Identity.Version);
            var targetPackageDir = targetPackagePathResolver.GetInstallPath(
                _package.Identity.Name,
                _package.Identity.Version);

            await Task.Run(() => root.Operations.Copy(srcPackageDir, targetPackageDir));
        }
    }
}
