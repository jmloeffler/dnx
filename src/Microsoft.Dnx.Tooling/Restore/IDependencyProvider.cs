// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Microsoft.Dnx.Runtime
{
    public interface IDependencyProvider
    {
        LibraryDescription GetDescription(LibraryRange libraryRange, FrameworkName targetFramework);

        IEnumerable<string> GetAttemptedPaths(FrameworkName targetFramework);
    }
}
