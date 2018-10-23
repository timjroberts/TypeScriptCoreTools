using System;
using System.Collections.Generic;
using ManagedNodeProcess.Packages;
using ManagedNodeProcess.Utils;
using TestUtils;
using TestUtils.Fixtures;
using Xunit;

namespace ManagedNodeProcess.Tests.Packages
{
    public class FooPackageWriter : EmbeddedScriptResourcePackageWriter
    {
        public FooPackageWriter()
            : base(new [] { "ManagedNodeProcess.Tests/Scripts/Foo.js" })
        { }
    }

    public class BarPackageWriter : EmbeddedScriptResourcePackageWriter
    {
        public BarPackageWriter()
            : base(new [] { "ManagedNodeProcess.Tests/Scripts/Folder/Bar.js" })
        { }
    }

    public class IndependentNodeProcess : NodeProcess
    { }

    [RequiresNpmPackage("left-pad")]
    public class LeftPadDependentNodeProcess : NodeProcess
    { }

    [RequiresNpmPackage("a67ceda85f")]
    public class UnknownDependentNodeProcess : NodeProcess
    { }

    [RequiresNpmPackage("foo-package", PackageWriter=typeof(FooPackageWriter))]
    public class FooNodeProcess : NodeProcess
    { }

    [RequiresNpmPackage("bar-package", PackageWriter=typeof(BarPackageWriter))]
    public class BarNodeProcess : NodeProcess
    { }

    public class PackageManagerFacts : IClassFixture<TempDirectoryFixture>
    {
        private readonly TempDirectoryFixture _tempDirectory;

        public PackageManagerFacts(TempDirectoryFixture tempDirectory)
        {
            _tempDirectory = tempDirectory;
        }

        [Fact]
        public void CreateThrowsIfObjectIsNotANodeProcess()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                PackageManager.Create(this, _tempDirectory.Create());
            });

            Assert.Equal("Supplied parameter 'nodeProcess' is not a NodeProcess object.", exception.Message);
        }

        [Fact]
        public void RequiredPackagesIsEmptyWithNoPackageDependencies()
        {
            var packageMgr = PackageManager.Create<IndependentNodeProcess>(_tempDirectory.Create());

            Assert.Empty(packageMgr.RequiredPackages);
        }

        [Fact]
        public void RequiredPackagesReflectsDefinedDependencies()
        {
            var packageMgr = PackageManager.Create<LeftPadDependentNodeProcess>(_tempDirectory.Create());

            Assert.Collection(
                packageMgr.RequiredPackages,
                p => string.Equals(p.ToString(), "left-pad@latest", StringComparison.Ordinal)
            );
        }

        [Fact]
        public async void RequiredPackagesAreInstalledIntoGivenRootPath()
        {
            var tempDir = _tempDirectory.Create();
            var packageMgr = PackageManager.Create<LeftPadDependentNodeProcess>(tempDir);

            await packageMgr.Install();

            DirectoryAssert.SubDirectoriesExist(tempDir, "node_modules", "left-pad");
        }

        [Fact]
        public async void PackageWriterCanBeUsedToCreateStubPackage()
        {
            var tempDir = _tempDirectory.Create();
            var packageMgr = PackageManager.Create<FooNodeProcess>(tempDir);

            await packageMgr.Install();

            DirectoryAssert.SubDirectoriesExist(tempDir, "node_modules", "foo-package");
        }

        [Fact]
        public async void PackageWriterUsingSubFolderCanBeUsedToCreateStubPackage()
        {
            var tempDir = _tempDirectory.Create();
            var packageMgr = PackageManager.Create<BarNodeProcess>(tempDir);

            await packageMgr.Install();

            DirectoryAssert.SubDirectoriesExist(tempDir, "node_modules", "bar-package");
        }

        [Fact]
        public async void ThrowsOnInstallForUnknownPackage()
        {
            var tempDir = _tempDirectory.Create();
            var packageMgr = PackageManager.Create<UnknownDependentNodeProcess>(tempDir);

            var exception = await Assert.ThrowsAsync<PackageInstallException>(() =>
            {
                return packageMgr.Install();
            });

            Assert.Equal("Unable to install required packages for 'ManagedNodeProcess.Tests.Packages.UnknownDependentNodeProcess'.", exception.Message);
            
            Assert.Collection(
                exception.UninstallablePackages,
                p => string.Equals(p, "a67ceda85f@latest", StringComparison.Ordinal)
            );
        }
    }
}
