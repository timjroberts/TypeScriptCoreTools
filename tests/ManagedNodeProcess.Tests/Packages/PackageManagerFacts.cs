using System;
using ManagedNodeProcess.Packages;
using TestUtils;
using TestUtils.Fixtures;
using Xunit;

namespace ManagedNodeProcess.Tests.Packages
{
    public class IndependentNodeProcess : NodeProcess
    { }

    [RequiresNpmPackage("left-pad")]
    public class LeftPadDependentNodeProcess : NodeProcess
    { }

    [RequiresNpmPackage("a67ceda85f")]
    public class UnknownDependentNodeProcess : NodeProcess
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
                p => string.Equals(p, "left-pad@latest", StringComparison.Ordinal)
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
