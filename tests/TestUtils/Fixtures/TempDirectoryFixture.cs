using System;
using System.Collections.Generic;
using System.IO;

namespace TestUtils.Fixtures
{
    public class TempDirectoryFixture : IDisposable
    {
        private readonly IList<DirectoryInfo> _disposablePaths = new List<DirectoryInfo>();
          
        public DirectoryInfo Create(bool autoDisposeDirectory = true)
        {
            var disposablePath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"{DateTime.UtcNow.Ticks:x}"));

            if (autoDisposeDirectory)
            {
                _disposablePaths.Add(disposablePath);
            }

            return disposablePath;
        }

        public void Dispose()
        {
            foreach (var disposablePath in _disposablePaths)
            {
                try
                {
                    disposablePath.Delete(true);
                }
                catch
                { }
            }
        }
    }
}
