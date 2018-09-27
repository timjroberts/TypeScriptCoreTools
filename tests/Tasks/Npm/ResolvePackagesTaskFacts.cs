using System;
using System.IO;
using TestUtils.Fixtures;
using TestUtils.Builders;
using Xunit;
using System.Diagnostics;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Tasks.Npm.Tests
{
    public class ResolvePackagesTaskFacts : IClassFixture<TempDirectoryFixture>
    {
        private readonly TempDirectoryFixture _tempDirectory;

        public ResolvePackagesTaskFacts(TempDirectoryFixture tempDirectory)
        {
            _tempDirectory = tempDirectory;
        }

        [Fact]
        public void MissingPackageJsonFileYieldsEmptyPackages()
        {
            AssertEmptyResolvedPackages(_tempDirectory);
        }

        [Fact]
        public void EmptyPackageJsonFileYieldsEmptyPackages()
        {
            AssertEmptyResolvedPackages(_tempDirectory, new PackageJsonFileBuilder());
        }

        [Fact]
        public void EmptyDependenciesObjectYieldsEmptyPackages()
        {
            AssertEmptyResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => { })
            );
        }

        [Fact]
        public void DependenciesAreResolvable()
        {
            AssertResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => 
                        {
                            deps.Include("left-pad", "1.1.3");
                            deps.Include("repeat-string", "1.5.2");
                        }
                    ),
                DefaultTaskConfiguration,
                (taskItem) =>
                { 
                    Assert.Equal("left-pad", taskItem.ItemSpec);
                    Assert.Equal("1.1.3", taskItem.GetMetadata("ResolvedVersion"));
                    Assert.EndsWith("left-pad", taskItem.GetMetadata("ResolvedDirectoryPath"));
                },
                (taskItem) =>
                { 
                    Assert.Equal("repeat-string", taskItem.ItemSpec);
                    Assert.Equal("1.5.2", taskItem.GetMetadata("ResolvedVersion"));
                    Assert.EndsWith("repeat-string", taskItem.GetMetadata("ResolvedDirectoryPath"));
                }
            );
        }

        [Fact]
        public void DependenciesCanBeExported()
        {
            AssertResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => 
                        {
                            deps.Include("left-pad", "1.1.3");
                            deps.Include("repeat-string", "1.5.2");
                        }
                    ).ExportDependencies("repeat-string"),
                DefaultTaskConfiguration,
                (taskItem) =>
                {
                    Assert.Equal("left-pad", taskItem.ItemSpec);
                    Assert.Equal("false", taskItem.GetMetadata("IsExported"));
                },
                (taskItem) =>
                {
                    Assert.Equal("repeat-string", taskItem.ItemSpec);
                    Assert.Equal("true", taskItem.GetMetadata("IsExported"));
                }
            );
        }

        [Fact]
        public void OnlyReceiveExportedDependenciesWhenExportedOnlyIsTrue()
        {
            AssertResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => 
                        {
                            deps.Include("left-pad", "1.1.3");
                            deps.Include("repeat-string", "1.5.2");
                        }
                    ).ExportDependencies("repeat-string"),
                (task) =>
                {
                    task.ExportedOnly = true;
                },
                (taskItem) =>
                {
                    Assert.Equal("repeat-string", taskItem.ItemSpec);
                    Assert.Equal("true", taskItem.GetMetadata("IsExported"));
                }
            );
        }

        [Fact]
        public void DependencyLocalTypesWithoutFolderHierarchyAreResolvable()
        {
            AssertResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => 
                        {
                            deps.Include("left-pad", "latest");
                        }
                    ),
                DefaultTaskConfiguration,
                (taskItem) =>
                {
                    Assert.Equal("left-pad", taskItem.ItemSpec);
                    Assert.StartsWith(taskItem.GetMetadata("ResolvedDirectoryPath"), taskItem.GetMetadata("ResolvedTypesRootDirectoryPath"));
                }
            );
        }

        [Fact]
        public void DependencyLocalTypesWithFolderHierarchyAreResolvable()
        {
            AssertResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => 
                        {
                            deps.Include("mobx", "5.5.0");
                        }
                    ),
                DefaultTaskConfiguration,
                (taskItem) =>
                {
                    Assert.Equal("mobx", taskItem.ItemSpec);
                    Assert.StartsWith(taskItem.GetMetadata("ResolvedDirectoryPath"), taskItem.GetMetadata("ResolvedTypesRootDirectoryPath"));
                }
            );
        }

        [Fact]
        public void DependencyTypePackagesAreResolvable()
        {
            AssertResolvedPackages(
                _tempDirectory,
                new PackageJsonFileBuilder()
                    .AddDependencies(deps => 
                        {
                            deps.Include("react-dom", "latest");
                            deps.Include("@types/react-dom", "latest");
                        }
                    ),
                DefaultTaskConfiguration,
                (taskItem) =>
                {
                    Assert.Equal("react-dom", taskItem.ItemSpec);
                    Assert.EndsWith("@types/react-dom", taskItem.GetMetadata("ResolvedTypesRootDirectoryPath"));
                }
            );
        }

        private static void AssertResolvedPackages(
            TempDirectoryFixture tempDirectory,
            IPackageJsonFileBuilder builder,
            Action<ResolvePackagesTask> configureTask,
            params Action<ITaskItem>[] taskItemInspectors
        )
        {
            var tempDir = tempDirectory.Create();
            var packageJsonFilePath = Path.Combine(tempDir.FullName, "package.json");

            builder.Build(new FileInfo(packageJsonFilePath));

            Process.Start(
                new ProcessStartInfo("npm")
                {
                    RedirectStandardOutput = true,
                    Arguments = "install --no-save",
                    WorkingDirectory = tempDir.FullName
                }
            ).WaitForExit();

            var task = new ResolvePackagesTask()
            {
                PackageJsonFilePath = packageJsonFilePath
            };

            configureTask(task);

            Assert.True(task.Execute());
            
            if (taskItemInspectors != null && taskItemInspectors.Length > 0)
            {
                Assert.Collection<ITaskItem>(task.ResolvedPackages, taskItemInspectors);
            }
        }

        private static void AssertEmptyResolvedPackages(TempDirectoryFixture tempDirectory, IPackageJsonFileBuilder builder = null)
        {
            var tempDir = tempDirectory.Create();
            var packageJsonFilePath = Path.Combine(tempDir.FullName, "package.json");

            if (builder != null)
            {
                builder.Build(new FileInfo(packageJsonFilePath));
            }

            var task = new ResolvePackagesTask()
            {
                PackageJsonFilePath = packageJsonFilePath
            };

            Assert.True(task.Execute());
            Assert.Empty(task.ResolvedPackages);
        }

        private static void DefaultTaskConfiguration(ResolvePackagesTask task)
        { }
    }
}