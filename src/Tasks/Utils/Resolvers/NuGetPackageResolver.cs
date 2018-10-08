using System.IO;
using System.Linq;
using System.Collections.Generic;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.ProjectModel;

namespace Tasks.Utils.Resolvers
{
    public class NuGetPackageResolver
    {
        private readonly FallbackPackagePathResolver _packagePathResolver;

        public NuGetPackageResolver(INuGetPathContext pathContext)
        {
            _packagePathResolver = new FallbackPackagePathResolver(pathContext);
        }

        public NuGetPackageResolver(string userPackageFolder, IEnumerable<string> fallbackPackageFolders)
        {
            _packagePathResolver = new FallbackPackagePathResolver(userPackageFolder, fallbackPackageFolders);
        }

        public string GetPackageDirectory(string packageId, string packageVersion)
        {
            return _packagePathResolver.GetPackageDirectory(packageId, packageVersion);
        }

        public static NuGetPackageResolver CreateResolver(LockFile lockFile, string projectPath)
        {
            NuGetPackageResolver packageResolver;

            string userPackageFolder = lockFile.PackageFolders.FirstOrDefault()?.Path;

            if (userPackageFolder != null)
            {
                var fallBackFolders = lockFile.PackageFolders.Skip(1).Select(f => f.Path);
                packageResolver = new NuGetPackageResolver(userPackageFolder, fallBackFolders);
            }
            else
            {
                NuGetPathContext nugetPathContext = NuGetPathContext.Create(Path.GetDirectoryName(projectPath));
                packageResolver = new NuGetPackageResolver(nugetPathContext);
            }

            return packageResolver;
        }
    }
}