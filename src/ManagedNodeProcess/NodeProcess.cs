using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ManagedNodeProcess.Packages;
using ManagedNodeProcess.Utils;

namespace ManagedNodeProcess
{
    public class NodeProcess : INodeProcess
    {
        private static readonly Regex WhiteSpaceRegex = new Regex("\\n\\s*", RegexOptions.Multiline);

        private static readonly Regex ChunkRegex = new Regex("<\\[(\\w*)\\((\\d*)\\)\\[(.*)\\]\\]>");

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly long _baseTime = DateTime.Now.Ticks;
        private readonly IDictionary<long, ChunkEvaluationAsyncEvent> _evaluationResults = new Dictionary<long, ChunkEvaluationAsyncEvent>();
        private readonly PackageManager _packageManager;
        private readonly IEnumerable<string> _scriptChunks;

        private Process _nodeProcess;
        private StringBuilder _currentOutChunkBuilder = null;
        private StringBuilder _currentErrorChunkBuilder = null;
        private bool _disposed = false;
        
        public NodeProcess()
            : this(Enumerable.Empty<string>())
        { }

        public NodeProcess(IEnumerable<string> scriptChunks)
        {
            ExecutionContext = NodeProcessExecutionContext.Current ?? CreateDefaultNodeProcessExecutionContext();
            _packageManager = PackageManager.Create(this, ExecutionContext.NodeModulesRootDirectory);
            _scriptChunks = scriptChunks;
        }

        ~NodeProcess()
        {
            Dispose(false);
        }

        public DirectoryInfo WorkingDirectory { get; set; }

        protected INodeProcessExecutionContext ExecutionContext { get; private set; }

        public void Start()
        {
            _nodeProcess = LaunchNodeProcess(BuildNodeProcessStartInfo());

            PipeNodeProcessOutputStreams();

            if (_packageManager.RequiredPackages.Count() > 0)
            {
                EvaluateScriptChunk(EmbeddedScriptResourceReader.ReadScript("/Scripts/Resolver.js"));
                EvaluateScriptChunk($"SetResolveRootPath('{ExecutionContext.NodeModulesRootDirectory}');");
            }

            foreach (var scriptChunk in _scriptChunks)
            {
                EvaluateScriptChunk(scriptChunk);
            }
        }

        public void EvaluateScriptChunk(string scriptChunk)
        {
            try
            {
                Task.Run(() => EvaluateScriptChunkAsync<string>(scriptChunk)).Wait();
            }
            catch (AggregateException aggErr)
            {
                foreach (var err in aggErr.Flatten().InnerExceptions)
                {
                    if (err is ApplicationException || err is PackageInstallException)
                    {
                        throw err;
                    }
                }
            }
        }

        public T EvaluateScriptChunk<T>(string scriptChunk)
        {
            try
            {
                return Task.Run(() => EvaluateScriptChunkAsync<T>(scriptChunk)).Result;
            }
            catch (AggregateException aggErr)
            {
                foreach (var err in aggErr.Flatten().InnerExceptions)
                {
                    if (err is ApplicationException || err is PackageInstallException)
                    {
                        throw err;
                    }
                }

                throw new SystemException("Encountered an unknown error while evaluating the script chunk.");
            }
        }

        public async Task EvaluateScriptChunkAsync(string scriptChunk)
        {
            await EvaluateScriptChunkAsync<string>(scriptChunk);
        }

        public async Task<T> EvaluateScriptChunkAsync<T>(string scriptChunk)
        {
            await _packageManager.Install();

            var chunkId = DateTime.Now.Ticks - _baseTime;
            var evaluationResult = new ChunkEvaluationAsyncEvent();

            _evaluationResults[chunkId] = evaluationResult;

            _nodeProcess.StandardInput.WriteLine($"<[JS({chunkId})[{scriptChunk}]]>");

            try
            {
                var result = await evaluationResult.WaitAsync();

                if (string.Equals(result, "undefined", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(result, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return default(T);
                }

                return JsonConvert.DeserializeObject<T>(result, JsonSerializerSettings);
            }
            finally
            {
                _evaluationResults.Remove(chunkId);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (_nodeProcess != null && !_nodeProcess.HasExited)
            {
                _nodeProcess.Kill();
            }

            _disposed = true;
        }

        protected virtual void OnReceiveChunkResult(string chunkType, long chunkId, string chunkContent, bool isError)
        {
            if (!string.Equals(chunkType, "JS", StringComparison.Ordinal)) return;

            var evaluationResult = _evaluationResults[chunkId];

            if (evaluationResult == null) return;

            if (isError)
            {
                evaluationResult.SetError(new ApplicationException(chunkContent));
                
                return;
            }

            evaluationResult.SetResult(chunkContent);
        }

        protected virtual void OnReceiveConsoleOut(string data)
        {
            Console.WriteLine(data);
        }

        protected virtual void OnReceiveConsoleError(string data)
        {
            var currentColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(data);
            Console.ForegroundColor = currentColor;
        }        

        private ProcessStartInfo BuildNodeProcessStartInfo()
        {
            var biosScript = WhiteSpaceRegex.Replace(
                EmbeddedScriptResourceReader.ReadScript("/Scripts/Bios.js"),
                string.Empty
            );
                    
            var startInfo = new ProcessStartInfo("node")
                {
                    WorkingDirectory = WorkingDirectory?.FullName,
                    Arguments = $"-e \"{biosScript}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

            return startInfo;
        }

        private static Process LaunchNodeProcess(ProcessStartInfo startInfo)
        {
            try
            {
                var process = Process.Start(startInfo);

                process.EnableRaisingEvents = true;

                return process;
            }
            catch (Exception err)
            {
                var message =  "Failed to start Node process. To resolve this:\n\n"
                            +  "[1] Ensure that Node.js is installed and can be found in one of the PATH directories.\n"
                            + $"    Current PATH enviroment variable is: {Environment.GetEnvironmentVariable("PATH")}\n"
                            +  "    Make sure the Node executable is in one of those directories, or update your PATH.\n\n"
                            +  "[2] See the InnerException for further details of the cause.";

                throw new InvalidOperationException(message, err);
            }
        }

        private void PipeNodeProcessOutputStreams()
        {
            _nodeProcess.OutputDataReceived += (sender, evt) =>
            {
                if (string.IsNullOrEmpty(evt.Data)) return;

                ProcessStreamData(evt.Data, ref _currentOutChunkBuilder, OnReceiveConsoleOut, false);
            };

            _nodeProcess.ErrorDataReceived += (sender, evt) =>
            {
                if (string.IsNullOrEmpty(evt.Data)) return;
                
                ProcessStreamData(evt.Data, ref _currentErrorChunkBuilder, OnReceiveConsoleError, true);
            };

            _nodeProcess.BeginOutputReadLine();
            _nodeProcess.BeginErrorReadLine();
        }

        private void ProcessStreamData(string data, ref StringBuilder chunkBuilder, Action<string> consoleAction, bool isError)
        {
            if (data.StartsWith("<["))
            {
                chunkBuilder = new StringBuilder();
            }

            if (chunkBuilder != null)
            {
                chunkBuilder.Append(data);

                if (data.EndsWith("]]>"))
                {
                    var outChunk = chunkBuilder.ToString();

                    chunkBuilder = null;

                    var matches = ChunkRegex.Match(outChunk);

                    if (!matches.Success) return; // Error?

                    var chunkType = matches.Groups[1].ToString();
                    var chunkId = long.Parse(matches.Groups[2].ToString());
                    var chunkContent = matches.Groups[3].ToString();

                    OnReceiveChunkResult(chunkType, chunkId, chunkContent, isError);
                }

                return;
            }

            consoleAction(data);
        }

        private NodeProcessExecutionContext CreateDefaultNodeProcessExecutionContext()
        {
            // https://johnkoerner.com/csharp/special-folder-values-on-windows-versus-mac/

            // The following will be set to '/Users/<user>/.local/share' on *nix and MacOS, and 'C:\Users\<user>\AppData\Local' on Windows
            var appDataRootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var rootPath = Path.Combine(appDataRootPath, "TypeScriptSDK");

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            var assemblyPath = Path.Combine(rootPath, this.GetType().Assembly.GetName().Name.ToLowerInvariant());

            if (!Directory.Exists(assemblyPath))
            {
                Directory.CreateDirectory(assemblyPath);
            }

            return new NodeProcessExecutionContext(new DirectoryInfo(rootPath), new DirectoryInfo(assemblyPath));
        }
    }
}
