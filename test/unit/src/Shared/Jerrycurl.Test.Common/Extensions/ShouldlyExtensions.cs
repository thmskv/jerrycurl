using System.Text.Json;
using Shouldly;

namespace Jerrycurl.Test.Extensions
{
    public static class ShouldlyExtensions
    {
        public static void ShouldBeSameAsJson<T>(this T source, T expected)
        {
            var sourceJson = JsonSerializer.Serialize(source);
            var expectedJson = JsonSerializer.Serialize(expected);

            sourceJson.ShouldBe(expectedJson);
        }
    }
}
