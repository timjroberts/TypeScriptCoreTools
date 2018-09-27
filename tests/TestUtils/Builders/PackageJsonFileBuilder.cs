
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestUtils.Builders
{
    public interface IDependencyObjectBuilder
    {
        void Include(string packageName, string packageVersion);
    }

    public interface IPackageJsonFileBuilder
    {
        IPackageJsonFileBuilder MergeWithExisting();

        IPackageJsonFileBuilder AddDependencies(Action<IDependencyObjectBuilder> deps);

        IPackageJsonFileBuilder ExportDependencies(params string[] packageNames);

        JObject Build();

        JObject Build(FileInfo packageJsonFile);
    }

    public class PackageJsonFileBuilder : IPackageJsonFileBuilder
    {
        private enum BuildMode : byte
        {
            Replace,
            Merge
        }

        private class DependencyObjectBuilder : IDependencyObjectBuilder
        {
            private JObject _dependencyObj;

            private Action<IDependencyObjectBuilder> _dependencyObjBuilderCallback;

            public void Include(string packageName, string packageVersion)
            {
                if (_dependencyObj == null) return;

                _dependencyObj.Add(packageName, packageVersion);
            }

            public DependencyObjectBuilder UsingCallback(Action<IDependencyObjectBuilder> callback)
            {
                _dependencyObjBuilderCallback = callback;

                return this;
            }

            public JObject Build()
            {
                _dependencyObj = new JObject();

                _dependencyObjBuilderCallback(this);

                return _dependencyObj;
            }
        }

        private BuildMode _buildMode = BuildMode.Replace;
        private Action<IDependencyObjectBuilder> _dependenciesObjBuilderCallback = null;
        private string[] _exportedDependencies = null;

        public IPackageJsonFileBuilder MergeWithExisting()
        {
            _buildMode = BuildMode.Merge;

            return this;
        }

        public IPackageJsonFileBuilder AddDependencies(Action<IDependencyObjectBuilder> deps)
        {
            _dependenciesObjBuilderCallback = deps;

            return this;
        }

        public IPackageJsonFileBuilder ExportDependencies(params string[] packageNames)
        {
            _exportedDependencies = packageNames;

            return this;
        }

        public JObject Build()
        {
            return BuildPackageJson();
        }

        public JObject Build(FileInfo packageJsonFile)
        {
            return _buildMode == BuildMode.Replace
                ? ReplacePackageJsonFile(BuildPackageJson(), packageJsonFile)
                : MergePackageJsonFile(BuildPackageJson(), packageJsonFile);
        }

        private JObject ReplacePackageJsonFile(JObject obj, FileInfo packageJsonFile)
        {
            SavePackageJson(obj, packageJsonFile);

            return obj;
        }

        private JObject MergePackageJsonFile(JObject obj, FileInfo packageJsonFile)
        {
            var currentObj = packageJsonFile.Exists ? LoadPackageJson(packageJsonFile) : new JObject();

            currentObj.Merge(
                obj,
                new JsonMergeSettings()
                {
                    MergeArrayHandling = MergeArrayHandling.Union,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                }
            );

            SavePackageJson(currentObj, packageJsonFile);
            
            return currentObj;
        }

        private JObject BuildPackageJson()
        {
            var obj = new JObject();

            if (_dependenciesObjBuilderCallback != null)
            {
                obj.Add("dependencies", BuildDependencies());
            }

            if (_exportedDependencies != null)
            {
                obj.Add("exportDependencies", new JArray(_exportedDependencies));
            }

            return obj;
        }

        private JObject LoadPackageJson(FileInfo packageJsonFile)
        {
            using (var reader = File.OpenText(packageJsonFile.FullName))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return (JObject)JToken.ReadFrom(jsonReader);
            }
        }

        private void SavePackageJson(JObject obj, FileInfo packageJsonFile)
        {
            using (var writer = File.CreateText(packageJsonFile.FullName))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                obj.WriteTo(jsonWriter);
            }
        }

        private JObject BuildDependencies()
        {
            return new DependencyObjectBuilder()
                .UsingCallback(_dependenciesObjBuilderCallback)
                .Build();
        }
    }
}