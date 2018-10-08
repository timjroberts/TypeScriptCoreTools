using System;
using System.IO;
using Microsoft.Build.Framework;
using NuGet.Common;
using NuGet.ProjectModel;

namespace Tasks.Utils.Resolvers
{
    public class LockFileCache
    {
        private IBuildEngine4 _buildEngine;
        
        public LockFileCache(IBuildEngine4 buildEngine)
        {
            _buildEngine = buildEngine;
        }

        public LockFile GetLockFile(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                throw new Exception($"Assets file path '{path}' is not rooted. Only full paths are supported.");
            }

            string lockFileKey = GetTaskObjectKey(path);

            LockFile result;
			
            object existingLockFileTaskObject = _buildEngine.GetRegisteredTaskObject(lockFileKey, RegisteredTaskObjectLifetime.Build);
            if (existingLockFileTaskObject == null)
            {
                result = LoadLockFile(path);

                _buildEngine.RegisterTaskObject(lockFileKey, result, RegisteredTaskObjectLifetime.Build, true);
            }
            else
            {
                result = (LockFile)existingLockFileTaskObject;
            }

            return result;
        }

        private static string GetTaskObjectKey(string lockFilePath)
        {
            return $"{nameof(LockFileCache)}:{lockFilePath}";
        }

        private LockFile LoadLockFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"Assets file '{path}' not found. Run a NuGet package restore to generate this file.");
            }

            return LockFileUtilities.GetLockFile(path, NullLogger.Instance);
        }
    }
}