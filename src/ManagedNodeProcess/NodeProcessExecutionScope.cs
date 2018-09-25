using System;
using System.IO;

namespace ManagedNodeProcess
{
    public sealed class NodeProcessExecutionScope : IDisposable
    {
        public NodeProcessExecutionScope(DirectoryInfo rootPath, DirectoryInfo nodeModulesRootPath)
        {
            NodeProcessExecutionContext.Current = new NodeProcessExecutionContext(rootPath, nodeModulesRootPath);
        }

        public void Dispose()
        {
            NodeProcessExecutionContext.Current = null;
        }
    }
}
