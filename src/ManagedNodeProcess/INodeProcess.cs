using System;
using System.IO;
using System.Threading.Tasks;

namespace ManagedNodeProcess
{
    public interface INodeProcess : IDisposable
    {
        DirectoryInfo WorkingDirectory { get; set; }

        void Start();

        void EvaluateScriptChunk(string scriptChunk);

        T EvaluateScriptChunk<T>(string scriptChunk);

        Task EvaluateScriptChunkAsync(string scriptChunk);
        
        Task<T> EvaluateScriptChunkAsync<T>(string scriptChunk);
    }
}
