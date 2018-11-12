using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using Tasks.Utils.Resolvers;

namespace Tasks.Build
{
    public class ResolvePackageReferencePathsTask : Task
    {
        private static readonly IList<string> ESTargets = new List<string>() { "ES5" };

        private static readonly IList<string> ExcludedPackageIds = new List<string>() { "NETStandard.Library", "Microsoft.NET.Test.Sdk" };

        public ResolvePackageReferencePathsTask()
        {
            ResolveTypePaths = false;
        }

        [Required]
        public string ProjectDirectory
        { get; set; }

        [Required]
        public ITaskItem[] PackageReferences
        { get; set; }

        public bool ResolveTypePaths
        { get; set; }

        [Output]
        public ITaskItem[] ResolvedPaths
        { get; private set; }

        public override bool Execute()
        {
            var assetsFile = new FileInfo(Path.Combine(ProjectDirectory, "obj/project.assets.json"));

            if (PackageReferences == null || PackageReferences.Length == 0 || !assetsFile.Exists)
            {
                ResolvedPaths = Enumerable.Empty<TaskItem>().ToArray();

                return true;
            }

			var lockFileCache = new LockFileCache(BuildEngine4);
			var packageResolver = NuGetPackageResolver.CreateResolver(lockFileCache.GetLockFile(assetsFile.FullName), ProjectDirectory);

            ResolvedPaths = ResolveTypePaths
                ? GetResolvedTypePathItems(packageResolver)
                : GetResolvedBundlePath(packageResolver);

            return true;
        }

        private ITaskItem[] GetResolvedTypePathItems(NuGetPackageResolver packageResolver)
        {
            return PackageReferences.Where((taskItem) => !ExcludedPackageIds.Contains(taskItem.ItemSpec))
                .Select(p => new DirectoryInfo(Path.Combine(packageResolver.GetPackageDirectory(p.ItemSpec, p.GetMetadata("Version")), "lib")))
                .Where(dir => Directory.Exists(Path.Combine(dir.FullName, "typings")))
                .SelectMany(dir => Directory.GetDirectories(Path.Combine(dir.FullName, "typings")))
                //.SelectMany(libInfo => ESTargets.Select(esTarget => new { PackageName = libInfo.PackageName, ESTarget = esTarget, ManifestFilePath = Path.Combine(libInfo.Directory.FullName, $"js/index.manifest.json") }))
                //.Where(manifestInfo => File.Exists(manifestInfo.ManifestFilePath))
                //.Where(dir => File.Exists(Path.Combine(dir.FullName, "js/index.manifest.json")))
                .Select(dir => new TaskItem(dir))
                // .Select(manifestInfo => new TaskItem(
                //         manifestInfo.ManifestFilePath,
                //         new Dictionary<string, string>()
                //         {
                //             { "NpmPackageName", manifestInfo.PackageName.Replace('.', '-').ToLowerInvariant() },
                //             { "ManifestESTarget", manifestInfo.ESTarget },
                            
                //         }
                //     )
                // )
                .ToArray();
    
        }

        private ITaskItem[] GetResolvedBundlePath(NuGetPackageResolver packageResolver)
        {
            return PackageReferences.Where((taskItem) => !ExcludedPackageIds.Contains(taskItem.ItemSpec))
                .Select(p => new DirectoryInfo(Path.Combine(packageResolver.GetPackageDirectory(p.ItemSpec, p.GetMetadata("Version")), "lib")))
                //.SelectMany(libInfo => ESTargets.Select(esTarget => new { PackageName = libInfo.PackageName, ESTarget = esTarget, ManifestFilePath = Path.Combine(libInfo.Directory.FullName, $"js/index.manifest.json") }))
                //.Where(manifestInfo => File.Exists(manifestInfo.ManifestFilePath))
                .Where(dir => File.Exists(Path.Combine(dir.FullName, "js/index.manifest.json")))
                .Select(dir => new TaskItem(dir.FullName))
                // .Select(manifestInfo => new TaskItem(
                //         manifestInfo.ManifestFilePath,
                //         new Dictionary<string, string>()
                //         {
                //             { "NpmPackageName", manifestInfo.PackageName.Replace('.', '-').ToLowerInvariant() },
                //             { "ManifestESTarget", manifestInfo.ESTarget },
                            
                //         }
                //     )
                // )
                .ToArray();
        }
    }
}