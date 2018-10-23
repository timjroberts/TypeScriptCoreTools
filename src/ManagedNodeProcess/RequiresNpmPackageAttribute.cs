using System;

namespace ManagedNodeProcess
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequiresNpmPackageAttribute : Attribute
    {
        private static readonly string DefaultVersion = "latest";

        public RequiresNpmPackageAttribute(string packageName)
        {
            PackageName = packageName;
            Version = DefaultVersion;
            PackageWriter = null;
        }

        public string PackageName { get; protected set; }

        public string Version { get; set; }

        public Type PackageWriter { get; set; }

        public override string ToString()
        {
            return $"{PackageName}@{Version}";
        }
    }
}
