using System.IO;
using Xunit.Sdk;

namespace TestUtils
{
    public class DirectoryAssert
    {
        public static void SubDirectoriesExist(DirectoryInfo rootDir, params string[] segments)
        {
            SubDirectoriesExist(rootDir.FullName, segments);
        }

        public static void SubDirectoriesExist(string rootDir, string[] segments)
        {
            if (!Directory.Exists(rootDir)) throw new XunitException($"Expected root directory '{rootDir}' to exist.");

            var currentSegment = rootDir;

            foreach (var segment in segments)
            {
                currentSegment = Path.Combine(currentSegment, segment);

                if (!Directory.Exists(currentSegment)) throw new XunitException($"Expected directory '{currentSegment}' to exist.");
            }
        }
    }
}
