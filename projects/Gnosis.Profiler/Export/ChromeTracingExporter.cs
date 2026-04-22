using System.Text;
using Gnosis.Profiler.Sampling;

namespace Gnosis.Profiler.Export;

public static class ChromeTracingExporter
{
    #region 公开方法

    public static string Export()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");

        var samples = ProfilerSampler.GetAllSamples();
        var first = true;

        foreach (var sample in samples)
        {
            if (!first)
            {
                sb.AppendLine(",");
            }

            first = false;

            sb.AppendLine("  {");
            sb.AppendLine($"    \"name\": \"{sample.Name}\",");
            sb.AppendLine("    \"cat\": \"cpu\",");
            sb.AppendLine("    \"ph\": \"X\",");
            sb.AppendLine($"    \"dur\": {sample.AverageMs:F3},");
            sb.AppendLine($"    \"ts\": 0,");
            sb.AppendLine("    \"pid\": 1,");
            sb.AppendLine("    \"tid\": 1");
            sb.Append("  }");
        }

        sb.AppendLine();
        sb.Append("]");

        return sb.ToString();
    }

    #endregion
}
