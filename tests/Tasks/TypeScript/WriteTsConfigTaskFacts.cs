using System;
using System.IO;
using Tasks.TypeScript;
using TestUtils.Fixtures;
using Xunit;

namespace Tasks.Utils.Tests
{
    public class WriteTsConfigTaskFacts : IClassFixture<TempDirectoryFixture>
    {
        private readonly TempDirectoryFixture _tempDirectory;

        public WriteTsConfigTaskFacts(TempDirectoryFixture tempDirectory)
        {
            _tempDirectory = tempDirectory;
        }

        [Fact]
        public void Test1()
        {
            var tempDir = _tempDirectory.Create();
            var tsConfigJsonFilePath = Path.Combine(tempDir.FullName, "tsconfig.json");

            var task = new WriteTsConfigTask()
            {
                TsConfigJsonFilePath = tsConfigJsonFilePath
            };

            Assert.True(task.Execute());
        }
    }
}