using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tasks.Utils;

namespace Tasks.Npm
{
    /// <summary>
    /// Resolves the paths for the npm packages listed in a given 'package.json' file.
    /// </summary>
    /// <remarks>
    /// The <see cref="ResolvePackagesTask" /> task will also return metadata about each package
    /// including the package version, and the directory path that represents the TypeScript
    /// type roots for the associated package.
    /// </remarks>
    public class ResolvePackagesTask : Task
    {
        /// <summary>
        /// The prefix that is applied to TypeScript type packages.
        /// </summary>
        private const string TypesPackageScope = "@types/";

        /// <summary>
        /// Initializes a new <see cref="ResolvePackagesTask" /> object with its default properties.
        /// </summary>
        public ResolvePackagesTask()
        {
            ExportedOnly = false;
        }

        /// <summary>
        /// Gets or sets the file path to the 'package.json' file.
        /// </summary>
        /// <value>A string representing a 'package.json' file path.</value>
        [Required]
        public string PackageJsonFilePath
        { get; set; }

        /// <summary>
        /// Gets or sets a flag that determines if only exported packages should be returned.
        /// </summary>
        /// <value><c>true</c> if only exported packages are required; <c>false</c> if all packages
        /// are required.</value>
        public bool ExportedOnly
        { get; set; }

        /// <summary>
        /// Gets the resolved package paths.
        /// </summary>
        /// <value>An array of <see cref="ITaskItem" /> objects that describe the resolved packages if
        /// the task executed sucessfully; <c>null</c> otherwise.</value>
        [Output]
        public ITaskItem[] ResolvedPackages
        { get; private set; }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <remarks>
        /// If the task was sucessfully executed then the resolved package data is made available via
        /// the <see cref="ResolvedPackages" /> property.
        /// </remarks>
        /// <returns><c>true</c> if the task was executed sucessfully; <c>false</c> otherwise.</returns>
        public override bool Execute()
        {
            var packageJsonFile = new FileInfo(PackageJsonFilePath);

            if (!packageJsonFile.Exists)
            {
                ResolvedPackages = Enumerable.Empty<TaskItem>().ToArray();

                return true;
            }

            var packageObj = JsonUtils.LoadJObject(packageJsonFile);

            var dependencies = (JObject)packageObj["dependencies"];

            if (dependencies == null)
            {
                ResolvedPackages = Enumerable.Empty<TaskItem>().ToArray();

                return true;
            }

            var exportedDependencies = ((JArray)packageObj["exportDependencies"] ?? new JArray()).Values();

            using (var process = new NpmNodeProcess())
            {
                process.WorkingDirectory = packageJsonFile.Directory;

                process.Start();

                ResolvedPackages = dependencies.Properties()
                    .Where(property => !property.Name.StartsWith(TypesPackageScope)) // remove type packages
                    .Select(property =>
                    {
                        var resolvedPackage = process.ResolvePackage(property.Name);

                        var taskItem = new TaskItem(property.Name);

                        var hasResolvedEntryPoint = (bool)resolvedPackage["hasResolvedEntryPoint"];

                        taskItem.SetMetadata("ResolvedDirectoryPath", (string)resolvedPackage["resolvedDirectoryPath"]);
                        taskItem.SetMetadata("ResolvedTypesRootDirectoryPath", (string)resolvedPackage["typesRootDirectoryPath"]);
                        taskItem.SetMetadata("ResolvedVersion", (string)resolvedPackage["resolvedVersion"]);
                        taskItem.SetMetadata("IsExported", exportedDependencies.Contains(property.Name) ? "true" : "false");
                        taskItem.SetMetadata("HasEntryPoint", hasResolvedEntryPoint ? "true" : "false");

                        if (!hasResolvedEntryPoint)
                        {
                            Log.LogWarning($"Package '{property.Name}' does not have an entry point.");
                        }

                        return taskItem;
                    })
                    .Where(taskItem => !ExportedOnly || ExportedOnly && string.Equals(taskItem.GetMetadata("IsExported"), "true", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            return true;
        }
    }
}