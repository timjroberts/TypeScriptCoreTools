using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ManagedNodeProcess.Packages
{
    public class PackageManager
    {
        private static class FunctionUtils
        {
            public static Func<T, TResult> FallThroughUntil<T, TResult>(Func<T, TResult> func, TResult initialValue, TResult untilValue)
            {
                var results = new Dictionary<string, TResult>();

                return (T param) =>
                {
                    var key = param.ToString();

                    if (!results.ContainsKey(key) || !Object.Equals(results[key], untilValue))
                    {
                        results[key] = func(param);
                    }

                    return (TResult)results[key];
                };
            }
        }

        private static readonly Regex UnknownPackageErrorRegEx = new Regex("npm ERR! 404.*:\\s*(.*)");

        private static readonly Func<DirectoryInfo, bool> IsInstalled = FunctionUtils.FallThroughUntil<DirectoryInfo, bool>((DirectoryInfo rootDirectory) =>
        {
            var nodeModulesPath = Path.Combine(rootDirectory.FullName, "node_modules");

            return Directory.Exists(nodeModulesPath);
        }, false, true);

        private readonly Type _type;
        private readonly DirectoryInfo _rootDirectory;

        private PackageManager(Type type, DirectoryInfo rootDirectory)
        {
            _type = type;
            _rootDirectory = rootDirectory;
        }

        public IEnumerable<RequiresNpmPackageAttribute> RequiredPackages
        {
            get
            {
                return (RequiresNpmPackageAttribute[])_type.GetCustomAttributes(typeof(RequiresNpmPackageAttribute), true);
            }
        }

        public async Task Install()
        {
            var requiredPackages = RequiredPackages.ToArray();

            if (requiredPackages.Length == 0 || IsInstalled(_rootDirectory)) return;

            await InstallNpmPackages(requiredPackages.Where(p => p.PackageWriter is null));
            await WritePackages(requiredPackages.Where(p => !(p.PackageWriter is null)));
        }

        public static PackageManager Create<T>(DirectoryInfo rootPath) where T : NodeProcess
        {
            return new PackageManager(typeof(T), rootPath);
        }

        public static PackageManager Create(object nodeProcess, DirectoryInfo rootPath)
        {
            if (!typeof(NodeProcess).IsInstanceOfType(nodeProcess))
            {
                throw new ArgumentException($"Supplied parameter '{nameof(nodeProcess)}' is not a NodeProcess object.");
            }
            
            return new PackageManager(nodeProcess.GetType(), rootPath);
        }

        private Task InstallNpmPackages(IEnumerable<RequiresNpmPackageAttribute> npmPackages)
        {
            if (npmPackages.Count() == 0) return Task.CompletedTask;

            return Task.Run(() =>
            {
                var unknownPackages = new List<string>();

                var npmProcess = Process.Start(new ProcessStartInfo("npm", $"install --no-save {string.Join(" ", npmPackages)}")
                {
                    WorkingDirectory = _rootDirectory.FullName,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                npmProcess.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data == null) return;

                        var matches = UnknownPackageErrorRegEx.Match(args.Data);

                        if (!matches.Success) return;

                        unknownPackages.Add(matches.Groups[1].ToString());
                    };
                npmProcess.BeginErrorReadLine();
                npmProcess.StandardOutput.ReadToEnd();
                npmProcess.WaitForExit();
                npmProcess.CancelErrorRead();

                if (npmProcess.ExitCode != 0)
                {
                    throw new PackageInstallException($"Unable to install required packages for '{_type.FullName}'.", unknownPackages);
                }
            });
        }

        private async Task WritePackages(IEnumerable<RequiresNpmPackageAttribute> packages)
        {
            if (packages.Count() == 0) return;

            foreach (var package in packages)
            {
                var packageWriter = (IPackageWriter)Activator.CreateInstance(package.PackageWriter);

                var nodeModulesDirectoryPath = Path.Combine(_rootDirectory.FullName, "node_modules");
                var packageDirectoryPath = Path.Combine(nodeModulesDirectoryPath, package.PackageName);

                if (!Directory.Exists(nodeModulesDirectoryPath)) Directory.CreateDirectory(nodeModulesDirectoryPath);
                if (!Directory.Exists(packageDirectoryPath)) Directory.CreateDirectory(packageDirectoryPath);
                
                await packageWriter.WritePackage(package.PackageWriter.Assembly, new DirectoryInfo(packageDirectoryPath));
            }
        }
    }
}
