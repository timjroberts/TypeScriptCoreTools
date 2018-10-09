using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;

namespace Tasks.WebPack
{
    public class WebPackTask : Task
    {
        [Required]
        public string WorkingDirectory
		{ get; set; }

        [Required]
        public string Configuration
		{ get; set; }

        [Required]
        public string WebPackLibraryName
        { get; set; }

        [Required]
        public bool BundleAsLibrary
        { get; set; }

        [Required]
        public ITaskItem[] BundledPackages
        { get; set; }

        public override bool Execute()
        {
            using (var webpackProcess = new WebPackNodeProcess())
            {
                webpackProcess.WorkingDirectory = new DirectoryInfo(WorkingDirectory);

                webpackProcess.Start();

                webpackProcess.PackProject(WebPackLibraryName, BundleAsLibrary, Configuration, MapBundledPackages(BundledPackages));
            }

            return true;
        }

        private static JArray MapBundledPackages(ITaskItem[] bundledPackages)
        {
            return new JArray(
                bundledPackages
                .Where(bundledPackage =>
                    {
                        var hasEntryPoint = bundledPackage.GetMetadata("HasEntryPoint");

                        return string.Equals(hasEntryPoint, "true", StringComparison.OrdinalIgnoreCase);
                    }
                )
                .Select(bundledPackage =>
                    {
                        var obj = new JObject();

                        obj.Add("packageName", bundledPackage.ItemSpec);
                        obj.Add("resolvedDirectoryPath", bundledPackage.GetMetadata("ResolvedDirectoryPath"));

                        var isBundle = bundledPackage.GetMetadata("IsBundle");

                        obj.Add("isBundle", string.IsNullOrEmpty(isBundle) ? false : bool.Parse(isBundle));

                        return obj;
                    }
                )
            );
        }
    }
}