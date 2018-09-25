using System.IO;

namespace ManagedNodeProcess
{
    public interface INodeProcessExecutionContext
    {
        DirectoryInfo RootDirectory { get; }

        DirectoryInfo NodeModulesRootDirectory { get; }
    }
}
