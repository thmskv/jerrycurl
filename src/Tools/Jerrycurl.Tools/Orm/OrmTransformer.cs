using Jerrycurl.Tools.Orm.Model;
using Jerrycurl.Tools.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Orm
{
    internal class OrmTransformer
    {
        public async Task<SchemaModel> TransformAsync(OrmToolOptions options, SchemaModel schema, ToolConsole console)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string jsFile = this.ResolveJavaScriptPath(options);

            if (jsFile == null)
                return schema;

            console.WriteLine($"Transforming with '{Path.GetFileName(jsFile)}'...");

            string hostFile = Path.Combine(tempPath, "transform.host.js");
            string hostContent = ResourceHelper.GetTransformHostString();
            string inputFile = Path.Combine(tempPath, "input.json");
            string outputFile = Path.Combine(tempPath, "output.json");
            string workingDir = Path.GetDirectoryName(jsFile);
            string nodePath = this.FindNodePath();

            Directory.CreateDirectory(tempPath);

            File.WriteAllText(hostFile, hostContent);
            await this.SerializeAsync(schema, inputFile);

            ToolRunnerOptions nodeOptions = new ToolRunnerOptions()
            {
                ToolName = nodePath,
                Arguments = new[] { hostFile, jsFile, inputFile, outputFile },
                WorkingDirectory = workingDir,
                //StdErr = s => console.Write(s),
                StdOut = s => console.Write(s),
            };

            try
            {
                await ToolRunner.RunAsync(nodeOptions);

                SchemaModel newModel = await this.DeserializeAsync<SchemaModel>(outputFile);

                return newModel;
            }
            catch (ToolException ex)
            {
                throw new OrmToolException("Transformation failed.", ex.StdErr, ex);
            }
            catch (Exception ex)
            {
                throw new OrmToolException("Transformation failed.", ex);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        private string ResolveJavaScriptPath(OrmToolOptions options)
        {
            if (options.Transform != null && File.Exists(options.Transform))
                return Path.GetFullPath(options.Transform);
            else
            {
                var defaultPath = options.Input + ".js";

                if (File.Exists(defaultPath))
                    return Path.GetFullPath(defaultPath);
            }

            return null;
        }

        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        private async Task<T> DeserializeAsync<T>(string filePath)
        {
            using var stream = File.OpenRead(filePath);

            return await JsonSerializer.DeserializeAsync<T>(stream, this.GetJsonOptions());
        }

        private async Task SerializeAsync<T>(T value, string filePath)
        {
            using var stream = File.OpenWrite(filePath);

            await JsonSerializer.SerializeAsync(stream, value, this.GetJsonOptions());
        }

        private string FindNodePath()
        {
            return GetDefaultPaths().FirstOrDefault(File.Exists);

            IEnumerable<string> GetDefaultPaths()
            {
                yield return Environment.ExpandEnvironmentVariables("%ProgramW6432%\\nodejs\\node.exe");
                yield return "C:\\Program Files\\nodejs\\node.exe";
                yield return "/usr/bin/node";
                yield return "/usr/bin/nodejs";
                yield return "/usr/local/bin/node";
                yield return Environment.ExpandEnvironmentVariables("%PROGRAMFILES%\\nodejs\\node.exe");

                foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
                {
                    string[] fullPaths = new[] { Path.Combine(path, "node.exe"), Path.Combine(path, "node") };

                    foreach (var fullPath in fullPaths)
                        yield return fullPath;
                }
            }
        }
    }
}
