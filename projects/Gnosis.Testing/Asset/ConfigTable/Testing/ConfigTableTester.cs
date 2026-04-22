using System.Text;
using Gnosis.Asset.Format.ConfigTables;

namespace Gnosis.Testing.Asset.ConfigTable;

/// <summary>
/// 配置表测试工具类，支持基于文件的测试模式
/// </summary>
public class ConfigTableTester
{
    #region 字段

    private readonly TimeSpan _timeout;
    private readonly Action<string>? _logger;

    #endregion

    #region 构造函数

    public ConfigTableTester(TimeSpan? timeout = null, Action<string>? logger = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _logger = logger;
    }

    #endregion

    #region 公共方法 - 文件驱动测试

    /// <summary>
    /// 运行基于文件的配置表解析测试
    /// </summary>
    /// <param name="csvPath">.csv 文件路径</param>
    /// <param name="autoGenerateExpected">如果 expected 文件不存在，是否自动生成</param>
    /// <returns>测试结果</returns>
    public ConfigTableFileTestResult RunParseTest(string csvPath, bool autoGenerateExpected = true)
    {
        var expectedPath = GetExpectedPath(csvPath);

        Log($"加载测试文件: {csvPath}");

        ConfigTableTestResult result;

        try
        {
            result = ParseCsv(csvPath);
        }
        catch (TimeoutException ex)
        {
            Log($"超时: {ex.Message}");
            return new ConfigTableFileTestResult(
                null,
                null,
                false,
                $"配置表解析超时（超过 {_timeout.TotalSeconds} 秒）",
                true
            );
        }
        catch (Exception ex)
        {
            Log($"异常: {ex.Message}");
            return new ConfigTableFileTestResult(
                null,
                null,
                false,
                $"配置表解析异常: {ex.Message}",
                false
            );
        }

        if (!File.Exists(expectedPath))
        {
            if (autoGenerateExpected)
            {
                Log($"生成 expected 文件: {expectedPath}");
                SaveExpectedFile(expectedPath, result.TableData);

                return new ConfigTableFileTestResult(
                    result.TableData,
                    result.TableData,
                    true,
                    $"已自动生成 expected 文件: {expectedPath}",
                    false
                );
            }

            return new ConfigTableFileTestResult(
                result.TableData,
                null,
                false,
                $"expected 文件不存在: {expectedPath}",
                false
            );
        }

        Log($"加载 expected 文件: {expectedPath}");
        var expected = LoadExpectedFile(expectedPath);

        var diff = CompareResults(result.TableData, expected);

        if (diff.IsMatch)
        {
            Log("测试通过");
        }
        else
        {
            Log($"测试失败: {diff.Message}");
        }

        return new ConfigTableFileTestResult(
            result.TableData,
            expected,
            diff.IsMatch,
            diff.Message,
            false
        );
    }

    /// <summary>
    /// 批量运行目录下所有 .csv 文件的测试
    /// </summary>
    public IReadOnlyList<ConfigTableFileTestResult> RunDirectoryTests(
        string directory,
        bool autoGenerateExpected = true,
        string searchPattern = "*.csv")
    {
        var results = new List<ConfigTableFileTestResult>();

        if (!Directory.Exists(directory))
        {
            Log($"目录不存在: {directory}");
            return results;
        }

        var csvFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);

        Log($"找到 {csvFiles.Length} 个测试文件");

        foreach (var csvPath in csvFiles)
        {
            Log($"\n--- 测试: {Path.GetFileName(csvPath)} ---");
            var result = RunParseTest(csvPath, autoGenerateExpected);
            results.Add(result);
        }

        var passed = results.Count(r => r.IsMatch);
        var failed = results.Count - passed;
        Log($"\n=== 测试汇总: {passed} 通过, {failed} 失败 ===");

        return results;
    }

    #endregion

    #region 公共方法 - 导出测试

    /// <summary>
    /// 测试配置表导出为 .script 和 .gon 文件
    /// </summary>
    public ConfigTableExportTestResult RunExportTest(ConfigTableData tableData, string outputDir, bool autoGenerateExpected = true)
    {
        Directory.CreateDirectory(outputDir);

        var exporter = new ConfigTableExporter();
        var scriptPath = Path.Combine(outputDir, $"{tableData.TableName}Table.script");
        var gonPath = Path.Combine(outputDir, $"{tableData.TableName}Table.gon");
        var binPath = Path.Combine(outputDir, $"{tableData.TableName}Table.bin");

        try
        {
            exporter.ExportAsync(tableData, outputDir).Wait(_timeout);
        }
        catch (Exception ex)
        {
            return new ConfigTableExportTestResult(
                false,
                $"导出失败: {ex.Message}",
                null,
                null,
                null
            );
        }

        var scriptContent = File.Exists(scriptPath) ? File.ReadAllText(scriptPath) : null;
        var gonContent = File.Exists(gonPath) ? File.ReadAllText(gonPath) : null;
        var binData = File.Exists(binPath) ? File.ReadAllBytes(binPath) : null;

        if (scriptContent is null || gonContent is null || binData is null)
        {
            return new ConfigTableExportTestResult(
                false,
                "导出文件不完整",
                scriptContent,
                gonContent,
                binData
            );
        }

        var scriptExpectedPath = Path.Combine(outputDir, $"{tableData.TableName}Table.script.expected");
        var gonExpectedPath = Path.Combine(outputDir, $"{tableData.TableName}Table.gon.expected");

        if (!File.Exists(scriptExpectedPath) || !File.Exists(gonExpectedPath))
        {
            if (autoGenerateExpected)
            {
                File.WriteAllText(scriptExpectedPath, scriptContent);
                File.WriteAllText(gonExpectedPath, gonContent);

                return new ConfigTableExportTestResult(
                    true,
                    $"已自动生成 expected 文件: {scriptExpectedPath}, {gonExpectedPath}",
                    scriptContent,
                    gonContent,
                    binData
                );
            }

            return new ConfigTableExportTestResult(
                false,
                "expected 文件不存在",
                scriptContent,
                gonContent,
                binData
            );
        }

        var expectedScript = File.ReadAllText(scriptExpectedPath);
        var expectedGon = File.ReadAllText(gonExpectedPath);

        if (scriptContent.Trim() != expectedScript.Trim())
        {
            return new ConfigTableExportTestResult(
                false,
                ".script 文件内容不匹配",
                scriptContent,
                gonContent,
                binData
            );
        }

        if (gonContent.Trim() != expectedGon.Trim())
        {
            return new ConfigTableExportTestResult(
                false,
                ".gon 文件内容不匹配",
                scriptContent,
                gonContent,
                binData
            );
        }

        return new ConfigTableExportTestResult(
            true,
            "导出测试通过",
            scriptContent,
            gonContent,
            binData
        );
    }

    #endregion

    #region 公共方法 - 直接解析

    /// <summary>
    /// 解析 CSV 文件为配置表数据
    /// </summary>
    public ConfigTableTestResult ParseCsv(string path)
    {
        Log($"开始解析 CSV: {path}");

        var parser = new CsvTableParser();
        var tableData = ExecuteWithTimeout(() => parser.ParseAsync(path).Result);

        Log($"解析完成，表名: {tableData.TableName}，字段数: {tableData.Schema.Fields.Count}，行数: {tableData.Rows.Count}");

        return new ConfigTableTestResult(tableData);
    }

    /// <summary>
    /// 从字符串解析配置表数据
    /// </summary>
    public ConfigTableTestResult ParseCsvFromString(string content, string tableName = "Test")
    {
        Log($"开始从字符串解析配置表: {tableName}");

        var parser = new CsvTableParser();
        var tableData = parser.ParseFromString(content, tableName);

        Log($"解析完成，字段数: {tableData.Schema.Fields.Count}，行数: {tableData.Rows.Count}");

        return new ConfigTableTestResult(tableData);
    }

    /// <summary>
    /// 验证外键约束
    /// </summary>
    public ForeignKeyCheckResult CheckForeignKeys(IReadOnlyDictionary<string, ConfigTableData> tables)
    {
        Log($"开始验证外键，表数量: {tables.Count}");

        var checker = new ConfigTableForeignKeyChecker();
        var result = checker.Check(tables);

        Log($"外键验证完成，有效: {result.IsValid}，错误数: {result.Errors.Count}");

        return result;
    }

    #endregion

    #region 文件格式

    /// <summary>
    /// 获取 expected 文件路径
    /// </summary>
    public static string GetExpectedPath(string csvPath)
    {
        return Path.ChangeExtension(csvPath, ".expected");
    }

    /// <summary>
    /// 保存 expected 文件
    /// </summary>
    public static void SaveExpectedFile(string filePath, ConfigTableData tableData)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Gnosis ConfigTable Expected Output");
        sb.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# 表名: {tableData.TableName}");
        sb.AppendLine();

        sb.AppendLine("# === Schema ===");
        sb.AppendLine($"TableName: {tableData.TableName}");
        sb.AppendLine($"FieldCount: {tableData.Schema.Fields.Count}");
        sb.AppendLine($"RowCount: {tableData.Rows.Count}");
        sb.AppendLine($"PrimaryKey: {tableData.Schema.PrimaryKeyFieldName ?? "null"}");
        sb.AppendLine();

        sb.AppendLine("# === Fields ===");
        foreach (var field in tableData.Schema.Fields)
        {
            sb.AppendLine($"Field: {field.Name}|{field.Type.Kind}|{field.Comment}|{field.IsPrimaryKey}");
        }
        sb.AppendLine();

        sb.AppendLine("# === Rows ===");
        foreach (var row in tableData.Rows)
        {
            var values = row.Values.Select(v => FormatValueForExpected(v)).ToList();
            sb.AppendLine(string.Join("\t", values));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// 加载 expected 文件
    /// </summary>
    public static ConfigTableData LoadExpectedFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var tableName = "Unknown";
        var fields = new List<TableField>();
        var rows = new List<TableRow>();
        var inSchema = false;
        var inFields = false;
        var inRows = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            {
                if (trimmed.StartsWith("# === Schema ==="))
                {
                    inSchema = true;
                    inFields = false;
                    inRows = false;
                }
                else if (trimmed.StartsWith("# === Fields ==="))
                {
                    inSchema = false;
                    inFields = true;
                    inRows = false;
                }
                else if (trimmed.StartsWith("# === Rows ==="))
                {
                    inSchema = false;
                    inFields = false;
                    inRows = true;
                }
                else if (trimmed.StartsWith("TableName: "))
                {
                    tableName = trimmed["TableName: ".Length..];
                }

                continue;
            }

            if (inFields)
            {
                var parts = trimmed.Split('|');
                if (parts.Length >= 4)
                {
                    fields.Add(new TableField
                    {
                        Name = parts[0]["Field: ".Length..],
                        Type = ParseFieldType(parts[1]),
                        Comment = parts[2],
                        IsPrimaryKey = bool.Parse(parts[3])
                    });
                }
            }
            else if (inRows)
            {
                var values = trimmed.Split('\t').Select(ParseExpectedValue).ToList();
                rows.Add(new TableRow { Values = values });
            }
        }

        var primaryKeyField = fields.FirstOrDefault(f => f.IsPrimaryKey);

        var schema = new TableSchema
        {
            TableName = tableName,
            Fields = fields,
            PrimaryKeyFieldName = primaryKeyField?.Name
        };

        return new ConfigTableData
        {
            TableName = tableName,
            Schema = schema,
            Rows = rows,
            SourcePath = filePath
        };
    }

    private static string FormatValueForExpected(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string str)
        {
            return str.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        if (value is float f)
        {
            return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
        }

        if (value is double d)
        {
            return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return value.ToString() ?? "null";
    }

    private static object? ParseExpectedValue(string raw)
    {
        if (raw == "null")
        {
            return null;
        }

        if (raw == "true")
        {
            return true;
        }

        if (raw == "false")
        {
            return false;
        }

        if (raw.EndsWith('f') && float.TryParse(raw[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f32))
        {
            return f32;
        }

        if (int.TryParse(raw, out var i32))
        {
            return i32;
        }

        if (long.TryParse(raw, out var i64))
        {
            return i64;
        }

        if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f64))
        {
            return f64;
        }

        return raw.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\r", "\r");
    }

    private static TableFieldType ParseFieldType(string typeStr)
    {
        return typeStr switch
        {
            "I32" => TableFieldType.I32,
            "I64" => TableFieldType.I64,
            "F32" => TableFieldType.F32,
            "F64" => TableFieldType.F64,
            "Bool" => TableFieldType.Bool,
            "String" => TableFieldType.String,
            _ => TableFieldType.String
        };
    }

    #endregion

    #region 结果比较

    private ConfigTableDiffResult CompareResults(ConfigTableData actual, ConfigTableData expected)
    {
        if (actual.TableName != expected.TableName)
        {
            return new ConfigTableDiffResult(
                false,
                $"表名不匹配: 期望 '{expected.TableName}'，实际 '{actual.TableName}'"
            );
        }

        if (actual.Schema.Fields.Count != expected.Schema.Fields.Count)
        {
            return new ConfigTableDiffResult(
                false,
                $"字段数量不匹配: 期望 {expected.Schema.Fields.Count}，实际 {actual.Schema.Fields.Count}"
            );
        }

        for (var i = 0; i < actual.Schema.Fields.Count; i++)
        {
            var actualField = actual.Schema.Fields[i];
            var expectedField = expected.Schema.Fields[i];

            if (actualField.Name != expectedField.Name)
            {
                return new ConfigTableDiffResult(
                    false,
                    $"字段[{i}] 名称不匹配: 期望 '{expectedField.Name}'，实际 '{actualField.Name}'"
                );
            }

            if (actualField.Type.Kind != expectedField.Type.Kind)
            {
                return new ConfigTableDiffResult(
                    false,
                    $"字段[{i}] 类型不匹配: 期望 '{expectedField.Type.Kind}'，实际 '{actualField.Type.Kind}'"
                );
            }
        }

        if (actual.Rows.Count != expected.Rows.Count)
        {
            return new ConfigTableDiffResult(
                false,
                $"行数不匹配: 期望 {expected.Rows.Count}，实际 {actual.Rows.Count}"
            );
        }

        for (var r = 0; r < actual.Rows.Count; r++)
        {
            var actualRow = actual.Rows[r];
            var expectedRow = expected.Rows[r];

            if (actualRow.Values.Count != expectedRow.Values.Count)
            {
                return new ConfigTableDiffResult(
                    false,
                    $"行[{r}] 列数不匹配: 期望 {expectedRow.Values.Count}，实际 {actualRow.Values.Count}"
                );
            }

            for (var c = 0; c < actualRow.Values.Count; c++)
            {
                var actualValue = actualRow.Values[c];
                var expectedValue = expectedRow.Values[c];

                if (!ValuesEqual(actualValue, expectedValue))
                {
                    return new ConfigTableDiffResult(
                        false,
                        $"行[{r}] 列[{c}] 值不匹配: 期望 '{FormatValueForExpected(expectedValue)}'，实际 '{FormatValueForExpected(actualValue)}'"
                    );
                }
            }
        }

        return new ConfigTableDiffResult(true, "匹配成功");
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.GetType() != b.GetType())
        {
            return false;
        }

        if (a is float f1 && b is float f2)
        {
            return Math.Abs(f1 - f2) < 0.0001f;
        }

        if (a is double d1 && b is double d2)
        {
            return Math.Abs(d1 - d2) < 0.0001;
        }

        return a.Equals(b);
    }

    #endregion

    #region 私有方法

    private T ExecuteWithTimeout<T>(Func<T> action)
    {
        var task = Task.Run(action);

        if (!task.Wait(_timeout))
        {
            throw new TimeoutException($"配置表操作超时，超过 {_timeout.TotalSeconds} 秒");
        }

        return task.Result;
    }

    private void Log(string message)
    {
        _logger?.Invoke($"[ConfigTableTester] {message}");
    }

    #endregion
}

/// <summary>
/// 配置表测试结果
/// </summary>
public record ConfigTableTestResult(ConfigTableData TableData);

/// <summary>
/// 配置表文件测试结果
/// </summary>
public record ConfigTableFileTestResult(
    ConfigTableData? ActualTable,
    ConfigTableData? ExpectedTable,
    bool IsMatch,
    string Message,
    bool IsTimeout
);

/// <summary>
/// 配置表导出测试结果
/// </summary>
public record ConfigTableExportTestResult(
    bool IsMatch,
    string Message,
    string? ScriptContent,
    string? GonContent,
    byte[]? BinaryData
);

/// <summary>
/// 配置表差异结果
/// </summary>
internal record ConfigTableDiffResult(bool IsMatch, string Message);

/// <summary>
/// 配置表测试异常
/// </summary>
public class ConfigTableTestException : Exception
{
    public ConfigTableTestException(string message) : base(message)
    {
    }
}
