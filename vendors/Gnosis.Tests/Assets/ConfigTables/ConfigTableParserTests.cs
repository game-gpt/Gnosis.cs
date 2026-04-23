using Gnosis.Asset.ConfigTable.Testing;
using Gnosis.Asset.Format.ConfigTables;
using NUnit.Framework;

namespace Gnosis.Assets.ConfigTables;

/// <summary>
/// 配置表解析器测试 - 基于文件驱动
/// </summary>
[TestFixture]
public class ConfigTableParserTests
{
    #region 字段

    private ConfigTableTester _tester = null!;
    private string _tablesDirectory = null!;

    #endregion

    [SetUp]
    public void SetUp()
    {
        _tester = new ConfigTableTester(TimeSpan.FromSeconds(5), Console.WriteLine);

        var assemblyLocation = typeof(ConfigTableParserTests).Assembly.Location;
        var projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", ".."));
        _tablesDirectory = Path.Combine(projectRoot, "Assets", "ConfigTables", "Data");
    }

    #region 文件驱动测试

    [Test]
    public void RunAllConfigTableTests()
    {
        Assert.That(Directory.Exists(_tablesDirectory), Is.True, $"配置表目录不存在: {_tablesDirectory}");

        var results = _tester.RunDirectoryTests(_tablesDirectory, autoGenerateExpected: true);

        var failed = results.Where(r => !r.IsMatch && !r.Message.Contains("已自动生成")).ToList();

        if (failed.Count > 0)
        {
            var messages = failed.Select(f => f.Message);
            Assert.Fail($"以下测试失败:\n{string.Join("\n", messages)}");
        }
    }

    [Test]
    public void Parser_BasicItems_Passes()
    {
        var csvPath = Path.Combine(_tablesDirectory, "basic_items.csv");
        var result = _tester.RunParseTest(csvPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_PlayerLevels_Passes()
    {
        var csvPath = Path.Combine(_tablesDirectory, "player_levels.csv");
        var result = _tester.RunParseTest(csvPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    #endregion

    #region 直接测试方法 - CSV 解析

    [Test]
    public void ParseCsv_BasicData_ReturnsCorrectSchema()
    {
        var csv = @"
            道具ID,道具名称,类型,数值,是否可用
            id,name,type,value,isUsable
            i32,string,string,i32,bool
            1,生命药水,consumable,100,true
            2,魔法药水,consumable,50,true
            3,铁剑,equipment,15,false
        ";

        var result = _tester.ParseCsvFromString(csv, "Items");

        Assert.That(result.TableData.TableName, Is.EqualTo("Items"));
        Assert.That(result.TableData.Schema.Fields.Count, Is.EqualTo(5));
        Assert.That(result.TableData.Schema.Fields[0].Name, Is.EqualTo("id"));
        Assert.That(result.TableData.Schema.Fields[0].Type.Kind, Is.EqualTo(TableFieldTypeKind.I32));
        Assert.That(result.TableData.Schema.Fields[0].IsPrimaryKey, Is.True);
        Assert.That(result.TableData.Schema.Fields[1].Name, Is.EqualTo("name"));
        Assert.That(result.TableData.Schema.Fields[1].Type.Kind, Is.EqualTo(TableFieldTypeKind.String));
        Assert.That(result.TableData.Schema.Fields[4].Name, Is.EqualTo("isUsable"));
        Assert.That(result.TableData.Schema.Fields[4].Type.Kind, Is.EqualTo(TableFieldTypeKind.Bool));
        Assert.That(result.TableData.Rows.Count, Is.EqualTo(3));
    }

    [Test]
    public void ParseCsv_VariousTypes_ParsesValuesCorrectly()
    {
        var csv = @"
            ID,整数,长整数,浮点数,双精度,布尔值,字符串
            id,i32Val,i64Val,f32Val,f64Val,boolVal,strVal
            i32,i32,i64,f32,f64,bool,string
            1,42,9999999999,3.14,2.71828,true,hello
            2,0,0,0.0,0.0,false,world
        ";

        var result = _tester.ParseCsvFromString(csv, "Types");

        Assert.That(result.TableData.Rows.Count, Is.EqualTo(2));

        var row0 = result.TableData.Rows[0];
        Assert.That(row0.Values[0], Is.EqualTo(1));
        Assert.That(row0.Values[1], Is.EqualTo(42));
        Assert.That(row0.Values[2], Is.EqualTo(9999999999L));
        Assert.That(row0.Values[3], Is.EqualTo(3.14f).Within(0.001f));
        Assert.That(row0.Values[4], Is.EqualTo(2.71828).Within(0.0001));
        Assert.That(row0.Values[5], Is.EqualTo(true));
        Assert.That(row0.Values[6], Is.EqualTo("hello"));
    }

    [Test]
    public void ParseCsv_NullValues_HandlesCorrectly()
    {
        var csv = @"
            ID,名称,数值
            id,name,value
            i32,string,i32
            1,有效项,100
            2,null,null
            3,,50
        ";

        var result = _tester.ParseCsvFromString(csv, "Nullable");

        Assert.That(result.TableData.Rows.Count, Is.EqualTo(3));
        Assert.That(result.TableData.Rows[0].Values[2], Is.EqualTo(100));
        Assert.That(result.TableData.Rows[1].Values[1], Is.Null);
        Assert.That(result.TableData.Rows[1].Values[2], Is.Null);
        Assert.That(result.TableData.Rows[2].Values[1], Is.Null);
        Assert.That(result.TableData.Rows[2].Values[2], Is.EqualTo(50));
    }

    [Test]
    public void ParseCsv_ListType_ParsesListValues()
    {
        var csv = @"
            ID,标签列表
            id,tags
            i32,[string]
            1,""[fire,ice,wind]""
            2,""[]""
        ";

        var result = _tester.ParseCsvFromString(csv, "ListTest");

        Assert.That(result.TableData.Schema.Fields[1].Type.Kind, Is.EqualTo(TableFieldTypeKind.List));
        Assert.That(result.TableData.Rows[0].Values[1], Is.InstanceOf<List<object?>>());

        var list = (List<object?>)result.TableData.Rows[0].Values[1]!;
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0], Is.EqualTo("fire"));
        Assert.That(list[1], Is.EqualTo("ice"));
        Assert.That(list[2], Is.EqualTo("wind"));
    }

    [Test]
    public void ParseCsv_ReferenceType_ParsesReferenceValues()
    {
        var csv = @"
            ID,父级ID
            id,parentId
            i32,&Items
            1,null
            2,1
        ";

        var result = _tester.ParseCsvFromString(csv, "Refs");

        Assert.That(result.TableData.Schema.Fields[1].Type.Kind, Is.EqualTo(TableFieldTypeKind.Reference));
        Assert.That(result.TableData.Schema.Fields[1].Type.GetReferenceTarget(), Is.EqualTo("Items"));
        Assert.That(result.TableData.Rows[0].Values[1], Is.Null);
        Assert.That(result.TableData.Rows[1].Values[1], Is.EqualTo(1));
    }

    [Test]
    public void ParseCsv_QuotedStrings_HandlesQuotes()
    {
        var csv = @"
            ID,描述
            id,desc
            i32,string
            1,""包含,逗号的文本""
            2,""包含""""引号""""的文本""
        ";

        var result = _tester.ParseCsvFromString(csv, "Quotes");

        Assert.That(result.TableData.Rows[0].Values[1], Is.EqualTo("包含,逗号的文本"));
        Assert.That(result.TableData.Rows[1].Values[1], Is.EqualTo("包含\"引号\"的文本"));
    }

    #endregion

    #region 直接测试方法 - 导出

    [Test]
    public void ExportTable_GeneratesScriptAndGon()
    {
        var csv = @"
            ID,名称,攻击力
            id,name,attack
            i32,string,i32
            1,铁剑,10
            2,钢剑,25
        ";

        var parseResult = _tester.ParseCsvFromString(csv, "Weapons");
        var tempDir = Path.Combine(Path.GetTempPath(), "gnosis_config_test", Guid.NewGuid().ToString());

        try
        {
            var exportResult = _tester.RunExportTest(parseResult.TableData, tempDir, autoGenerateExpected: true);

            Assert.That(exportResult.IsMatch || exportResult.Message.Contains("已自动生成"), Is.True, exportResult.Message);
            Assert.That(exportResult.ScriptContent, Is.Not.Null);
            Assert.That(exportResult.GonContent, Is.Not.Null);
            Assert.That(exportResult.ScriptContent, Does.Contain("export class WeaponsTable"));
            Assert.That(exportResult.ScriptContent, Does.Contain("export record WeaponsRow"));
            Assert.That(exportResult.GonContent, Does.Contain("WeaponsRow"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public void ExportTable_ScriptContainsCorrectMethods()
    {
        var csv = @"
            ID,名称,类型
            id,name,type
            i32,string,string
            1,物品A,normal
            2,物品B,rare
        ";

        var parseResult = _tester.ParseCsvFromString(csv, "Items");
        var tempDir = Path.Combine(Path.GetTempPath(), "gnosis_config_test", Guid.NewGuid().ToString());

        try
        {
            var exportResult = _tester.RunExportTest(parseResult.TableData, tempDir, autoGenerateExpected: true);

            Assert.That(exportResult.ScriptContent, Is.Not.Null);
            Assert.That(exportResult.ScriptContent, Does.Contain("static func load()"));
            Assert.That(exportResult.ScriptContent, Does.Contain("func get(id: i32)"));
            Assert.That(exportResult.ScriptContent, Does.Contain("func getAll()"));
            Assert.That(exportResult.ScriptContent, Does.Contain("func findByName"));
            Assert.That(exportResult.ScriptContent, Does.Contain("func findByType"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region 直接测试方法 - 外键检查

    [Test]
    public void CheckForeignKeys_ValidReferences_ReturnsValid()
    {
        var parentCsv = @"
            ID,名称
            id,name
            i32,string
            1,父项A
            2,父项B
        ";

        var childCsv = @"
            ID,父级ID
            id,parentId
            i32,&Parent
            10,1
            11,2
        ";

        var parentParser = new CsvTableParser();
        var childParser = new CsvTableParser();

        var parentTable = parentParser.ParseFromString(parentCsv, "Parent");
        var childTable = childParser.ParseFromString(childCsv, "Child");

        var tables = new Dictionary<string, ConfigTableData>
        {
            ["Parent"] = parentTable,
            ["Child"] = childTable
        };

        var result = _tester.CheckForeignKeys(tables);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void CheckForeignKeys_InvalidReferences_ReturnsErrors()
    {
        var parentCsv = @"
            ID,名称
            id,name
            i32,string
            1,父项A
        ";

        var childCsv = @"
            ID,父级ID
            id,parentId
            i32,&Parent
            10,1
            11,999
        ";

        var parentParser = new CsvTableParser();
        var childParser = new CsvTableParser();

        var parentTable = parentParser.ParseFromString(parentCsv, "Parent");
        var childTable = childParser.ParseFromString(childCsv, "Child");

        var tables = new Dictionary<string, ConfigTableData>
        {
            ["Parent"] = parentTable,
            ["Child"] = childTable
        };

        var result = _tester.CheckForeignKeys(tables);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThan(0));
        Assert.That(result.Errors[0].InvalidValue, Is.EqualTo(999));
    }

    [Test]
    public void CheckForeignKeys_MissingTargetTable_ReturnsErrors()
    {
        var childCsv = @"
            ID,父级ID
            id,parentId
            i32,&NonExistent
            10,1
        ";

        var childParser = new CsvTableParser();
        var childTable = childParser.ParseFromString(childCsv, "Child");

        var tables = new Dictionary<string, ConfigTableData>
        {
            ["Child"] = childTable
        };

        var result = _tester.CheckForeignKeys(tables);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThan(0));
        Assert.That(result.Errors[0].TargetTable, Is.EqualTo("NonExistent"));
    }

    #endregion

    #region 边界测试

    [Test]
    public void ParseCsv_EmptyData_ReturnsEmptyRows()
    {
        var csv = @"
            ID,名称
            id,name
            i32,string
        ";

        var result = _tester.ParseCsvFromString(csv, "Empty");

        Assert.That(result.TableData.Rows.Count, Is.EqualTo(0));
    }

    [Test]
    public void ParseCsv_SingleRow_ReturnsOneRow()
    {
        var csv = @"
            ID,名称
            id,name
            i32,string
            1,唯一项
        ";

        var result = _tester.ParseCsvFromString(csv, "Single");

        Assert.That(result.TableData.Rows.Count, Is.EqualTo(1));
        Assert.That(result.TableData.Rows[0].Values[0], Is.EqualTo(1));
        Assert.That(result.TableData.Rows[0].Values[1], Is.EqualTo("唯一项"));
    }

    [Test]
    public void ParseCsv_ExtraWhitespace_TrimmedCorrectly()
    {
        var csv = @"
            ID , 名称 , 数值
            id , name , value
            i32 , string , i32
            1 ,  测试  ,  100
        ";

        var result = _tester.ParseCsvFromString(csv, "Whitespace");

        Assert.That(result.TableData.Schema.Fields[0].Name, Is.EqualTo("id"));
        Assert.That(result.TableData.Schema.Fields[1].Name, Is.EqualTo("name"));
        Assert.That(result.TableData.Rows[0].Values[1], Is.EqualTo("测试"));
        Assert.That(result.TableData.Rows[0].Values[2], Is.EqualTo(100));
    }

    #endregion
}
