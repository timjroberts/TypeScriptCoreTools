using System;
using System.Threading.Tasks;

namespace ManagedNodeProcess.Utils
{
    internal class ChunkEvaluationAsyncEvent
    {
        private volatile TaskCompletionSource<string> _tcs = new TaskCompletionSource<string>();

        public Task<string> WaitAsync()
        {
            return _tcs.Task;
        }

        public void SetResult(string result)
        {
            _tcs.TrySetResult(result);
        }

        public void SetError(Exception error)
        {
            _tcs.TrySetException(error);
        }
    }
}
