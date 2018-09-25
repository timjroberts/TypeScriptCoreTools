using System.IO;
using System.Threading;

namespace ManagedNodeProcess
{
    public sealed class NodeProcessExecutionContext : INodeProcessExecutionContext
    {
        private static readonly AsyncLocal<NodeProcessExecutionContext> _instance = new AsyncLocal<NodeProcessExecutionContext>();

        internal NodeProcessExecutionContext(DirectoryInfo rootPath, DirectoryInfo nodeModulesRootPath)
        {
            RootDirectory = rootPath;
            NodeModulesRootDirectory = nodeModulesRootPath;
        }

        public DirectoryInfo RootDirectory { get; private set; }
        public DirectoryInfo NodeModulesRootDirectory { get; private set; }

        public static NodeProcessExecutionContext Current
        {
            get { return _instance.Value; }
            internal set { _instance.Value = value; }
        }
    }
}
