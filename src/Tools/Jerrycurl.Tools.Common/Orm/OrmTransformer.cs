using Jerrycurl.CommandLine;
using Jerrycurl.Tools.Orm.Model;
using Jerrycurl.Tools.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Orm
{
    public class OrmTransformer
    {
        public async Task<DatabaseModel> TransformAsync(OrmModel ormFile, DatabaseModel databaseModel)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string jsFile = this.ResolveJavaScriptPath(ormFile);

            if (jsFile == null)
                return databaseModel;

            string hostFile = Path.Combine(tempPath, "transform.host.js");
            string hostContent = ResourceHelper.GetTransformHostString();
            string inputFile = Path.Combine(tempPath, "input.json");
            string outputFile = Path.Combine(tempPath, "output.json");
            string workingDir = Path.GetDirectoryName(jsFile);
            string nodePath = this.FindNodePath(ormFile);

            Directory.CreateDirectory(tempPath);

            File.WriteAllText(hostFile, hostContent);
            await this.SerializeAsync(databaseModel, inputFile);

            try
            {
                await ToolRunner.RunAsync(nodePath, new[] { hostFile, jsFile, inputFile, outputFile }, workingDir, capture: true);

                DatabaseModel newModel = await this.DeserializeAsync<DatabaseModel>(outputFile);

                return newModel;
            }
            catch (ToolException ex)
            {
                return null;
            }
            //catch (ToolException ex)
            //{
            //    string log = string.IsNullOrEmpty(ex.StdErr) ? ex.StdOut : ex.StdErr;

            //    throw new KeplerException("Error running Node.js.", fileName: transformItem.AbsolutePath, log: log, innerException: ex.InnerException ?? ex);
            //}
            finally
            {
                Directory.Delete(tempPath, true);
            }
        }

        private string ResolveJavaScriptPath(OrmModel ormFile)
        {
            if (ormFile.Transform != null && File.Exists(ormFile.Transform))
                return Path.GetFullPath(ormFile.Transform);
            else
            {
                var defaultPath = ormFile.Input + ".js";

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

        private string FindNodePath(OrmModel ormFile)
        {
            var nodePath = this.GetDefaultNodePaths().FirstOrDefault(File.Exists);

            return nodePath;
        }

        private IEnumerable<string> GetDefaultNodePaths()
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
