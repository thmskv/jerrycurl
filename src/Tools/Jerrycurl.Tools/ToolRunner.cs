using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jerrycurl.Tools
{
    public static class ToolRunner
    {
        public static string Escape(string argument)
        {
            if (argument.Any(char.IsWhiteSpace))
                return $"\"{argument}\"";

            return argument;
        }

        public static string Escape(IEnumerable<string> args) => string.Join(" ", args.Select(Escape));

        public static string[] ToArgumentList(string arguments)
        {
            char[] c = arguments.ToCharArray();
            bool inSingle = false;
            bool inDouble = false;

            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == '"' && !inSingle)
                {
                    inDouble = !inDouble;
                    c[i] = '\n';
                }
                if (c[i] == '\'' && !inDouble)
                {
                    inSingle = !inSingle;
                    c[i] = '\n';
                }
                if (!inSingle && !inDouble && c[i] == ' ')
                    c[i] = '\n';
            }
            return new string(c).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static async Task RunAsync(ToolRunnerOptions options)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = options.ToolName,
                Arguments = Escape(options.Arguments),
                WorkingDirectory = options.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            StringBuilder stdOutBuilder = new StringBuilder();
            StringBuilder stdErrBuilder = new StringBuilder();

            void StdOut(DataReceivedEventArgs e)
            {
                stdOutBuilder.AppendLine(e.Data);
                options.StdOut?.Invoke(e.Data + Environment.NewLine);
            }

            void StdErr(DataReceivedEventArgs e)
            {
                stdErrBuilder.AppendLine(e.Data);
                options.StdErr?.Invoke(e.Data + Environment.NewLine);
            }

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                TaskCompletionSource<bool> outputCloseEvent = new TaskCompletionSource<bool>();

                if (startInfo.RedirectStandardOutput)
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                            outputCloseEvent.SetResult(true);
                        else
                            StdOut(e);
                    };
                }
                else
                    outputCloseEvent.SetResult(true);


                TaskCompletionSource<bool> errorCloseEvent = new TaskCompletionSource<bool>();

                if (startInfo.RedirectStandardError)
                {
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                            errorCloseEvent.SetResult(true);
                        else
                            StdErr(e);
                    };
                }
                else
                    errorCloseEvent.SetResult(true);

                try
                {
                    if (!process.Start())
                        throw new ToolException(-1, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString());
                }
                catch (Exception ex)
                {
                    throw new ToolException(-1, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString(), innerException: ex);
                }

                if (startInfo.RedirectStandardOutput)
                    process.BeginOutputReadLine();

                if (startInfo.RedirectStandardError)
                    process.BeginErrorReadLine();

                Task<bool> waitForExit = WaitForExitAsync(process, options.Timeout);
                Task processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                if (await Task.WhenAny(Task.Delay(options.Timeout), processTask) == processTask && await waitForExit)
                {
                    if (process.ExitCode != 0)
                        throw new ToolException(process.ExitCode, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString());
                }
                else
                {
                    try
                    {
                        process.Kill();
                    }
                    catch { }

                    throw new Exception("Timed out.");
                }
            }

            static Task<bool> WaitForExitAsync(Process process, int timeout) => Task.Run(() => process.WaitForExit(timeout));
        }
    }
}
