using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Tasks.TypeScript
{
    public class CompileTypeScriptTask : Task
    {
        private static readonly IList<string> ESTargets = new List<string>() { "ES5" };

        [Required]
        public string WorkingDirectory
		{ get; set; }

        [Required]
        public string PackageName
		{ get; set; }

        [Required]
        public string Configuration
		{ get; set; }
        
        public override bool Execute()
        {
            var sourceMapOption = string.Equals(Configuration, "debug", StringComparison.InvariantCultureIgnoreCase)
                ? "--sourceMap "
                : string.Empty;

            foreach (var esTarget in ESTargets)
            {
                var compileErrors = new List<string>();

                var tscProcess = Process.Start(
                    new ProcessStartInfo("tsc", $"-d -t {esTarget.ToLower()} -m commonjs {sourceMapOption}--outDir obj/{Configuration}/js --declarationDir obj/typings/{PackageName}")
                    { 
                        WorkingDirectory = WorkingDirectory,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                );

                tscProcess.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data == null) return;

                    // TODO: Use Regex to parse and check for error:
                    // file.ts(ln,cl): error TS12345: Error message.
                    compileErrors.Add(args.Data);
                };
                tscProcess.BeginOutputReadLine();
                tscProcess.WaitForExit();
                tscProcess.CancelOutputRead();

                if (tscProcess.ExitCode != 0)
                {
                    if (compileErrors.Count > 0)
                    {
                        foreach (var compileError in compileErrors)
                        {
                            Log.LogError(compileError);
                        }

                        return false;
                    }
                }
            }

            return true;
        }
    }
}