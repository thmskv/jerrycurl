using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jerrycurl.Tools.Resources
{
    public static class ResourceHelper
    {
        public static Stream GetResourceStream(string fileName) => typeof(ResourceHelper).Assembly.GetManifestResourceStream(typeof(ResourceHelper), fileName);
        public static string GetResourceString(string fileName, Dictionary<string, string> replacements = null)
        {
            using var stream = GetResourceStream(fileName);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string content = reader.ReadToEnd();

            if (replacements != null)
            {
                foreach (var kvp in replacements)
                    content = content.Replace($"${kvp.Key}", kvp.Value);
            }

            return content;
        }

        public static string GetTransformHostString() => GetResourceString("transform_host.js");
        public static string GetTransformTypeScriptTemplateString() => GetResourceString("transform_template_d.ts");
        public static string GetTransformJavaScriptTemplateString(string typesFileName)
        {
            if (typesFileName == null)
                return GetResourceString("transform_template_notypes.js");
            else
                return GetResourceString("transform_template.js", new() { ["tsname"] = typesFileName });
        }

        public static string GetRazorSkeletonString() => GetResourceString("razor_skeleton.txt");
    }
}
