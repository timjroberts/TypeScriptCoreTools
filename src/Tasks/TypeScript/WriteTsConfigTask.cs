using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using Tasks.Utils;

namespace Tasks.TypeScript
{
    public class WriteTsConfigTask : Task
    {
        private static readonly JsonMergeSettings DefaultJsontMergeSettings = new JsonMergeSettings()
        {
            MergeArrayHandling = MergeArrayHandling.Union,
            MergeNullValueHandling = MergeNullValueHandling.Merge
        };

        [Required]
        public string TsConfigJsonFilePath
        { get; set; }

        [Required]
        public ITaskItem[] CompilerPaths
        { get; set; }

        public override bool Execute()
        {
            var tsConfigJsonFile = new FileInfo(TsConfigJsonFilePath);

            var currentTsConfig = tsConfigJsonFile.Exists
                ? JsonUtils.LoadJObject(tsConfigJsonFile)
                : new JObject();

            var compilerOptions = (JObject)currentTsConfig["compilerOptions"] ?? new JObject();

            compilerOptions.Merge(GetUpdatedCompilerOptions(), DefaultJsontMergeSettings);

            var excludes = (JArray)currentTsConfig["exclude"] ?? new JArray();

            excludes.Merge(new JArray("./bin", "./obj"), DefaultJsontMergeSettings);

            currentTsConfig["compilerOptions"] = compilerOptions;
            currentTsConfig["exclude"] = excludes;

            JsonUtils.SaveJObject(currentTsConfig, tsConfigJsonFile);

            return true;
        }

        private JObject GetUpdatedCompilerOptions()
        {
            var compilerOptions = new JObject();

            if (CompilerPaths == null || CompilerPaths.Length == 0) return compilerOptions;

            compilerOptions.Add("baseUrl", ".");
            compilerOptions.Add(
                "paths",
                CompilerPaths.Aggregate(
                    new JObject(),
                    (obj, taskItem) =>
                    {
                        var compilerPathDirectory = new DirectoryInfo(taskItem.ItemSpec);

                        obj.Add(compilerPathDirectory.Name, new JArray(compilerPathDirectory.FullName));

                        return obj;
                    }
                )
            );

            return compilerOptions;
        }
    }
}