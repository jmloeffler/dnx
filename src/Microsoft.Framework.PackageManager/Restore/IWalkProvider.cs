﻿using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.DependencyManagement;
using Microsoft.Framework.PackageManager.Restore.RuntimeModel;

namespace Microsoft.Framework.PackageManager
{
    public interface IWalkProvider
    {
        bool IsHttp { get; }

        Task<WalkProviderMatch> FindLibrary(LibraryRange libraryRange, FrameworkName targetFramework, bool includeUnlisted);
        Task<IEnumerable<LibraryDependency>> GetDependencies(WalkProviderMatch match, FrameworkName targetFramework);
        Task<RuntimeFile> GetRuntimes(WalkProviderMatch match, FrameworkName targetFramework);
        Task CopyToAsync(WalkProviderMatch match, Stream stream);
    }
}
