namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 AST 节点类型枚举
/// </summary>
public enum NodeType
{
    /// <summary>
    /// 编译单元（源文件根节点）
    /// </summary>
    CompilationUnit,

    /// <summary>
    /// ECS 组件声明
    /// </summary>
    ComponentDecl,

    /// <summary>
    /// ECS 系统声明
    /// </summary>
    SystemDecl,

    /// <summary>
    /// UI 组件声明
    /// </summary>
    WidgetDecl,

    /// <summary>
    /// 场景声明
    /// </summary>
    SceneDecl,

    /// <summary>
    /// 插件声明
    /// </summary>
    PluginDecl,

    /// <summary>
    /// 函数声明
    /// </summary>
    FunctionDecl,

    /// <summary>
    /// 变量声明
    /// </summary>
    VariableDecl,

    /// <summary>
    /// 模块导入声明
    /// </summary>
    ImportDecl,

    /// <summary>
    /// 字段声明
    /// </summary>
    FieldDecl,

    /// <summary>
    /// 参数声明
    /// </summary>
    ParameterDecl,

    /// <summary>
    /// 类型注解
    /// </summary>
    TypeAnnotation,

    /// <summary>
    /// 特性声明
    /// </summary>
    AttributeDecl,

    /// <summary>
    /// 实体查询表达式
    /// </summary>
    QueryExpr,

    /// <summary>
    /// 元数据块
    /// </summary>
    MetaBlock,

    /// <summary>
    /// 代码块语句
    /// </summary>
    BlockStmt,

    /// <summary>
    /// 条件语句
    /// </summary>
    IfStmt,

    /// <summary>
    /// for-each 循环语句
    /// </summary>
    LoopStmt,

    /// <summary>
    /// while 循环语句
    /// </summary>
    WhileStmt,

    /// <summary>
    /// 返回语句
    /// </summary>
    ReturnStmt,

    /// <summary>
    /// 表达式语句
    /// </summary>
    ExprStmt,

    /// <summary>
    /// 二元表达式
    /// </summary>
    BinaryExpr,

    /// <summary>
    /// 一元表达式
    /// </summary>
    UnaryExpr,

    /// <summary>
    /// 函数调用表达式
    /// </summary>
    CallExpr,

    /// <summary>
    /// 成员访问表达式
    /// </summary>
    MemberAccessExpr,

    /// <summary>
    /// 索引访问表达式
    /// </summary>
    IndexExpr,

    /// <summary>
    /// 字面量表达式
    /// </summary>
    LiteralExpr,

    /// <summary>
    /// 标识符表达式
    /// </summary>
    IdentifierExpr,

    /// <summary>
    /// Lambda 表达式
    /// </summary>
    LambdaExpr,

    /// <summary>
    /// 赋值表达式
    /// </summary>
    AssignmentExpr,

    /// <summary>
    /// 结构体声明
    /// </summary>
    StructDecl,

    /// <summary>
    /// for 循环语句
    /// </summary>
    ForStmt,

    /// <summary>
    /// 丢弃语句
    /// </summary>
    DiscardStmt,

    /// <summary>
    /// 向量分量重组表达式
    /// </summary>
    SwizzleExpr,

    /// <summary>
    /// 命名空间使用声明
    /// </summary>
    UsingDecl,

    /// <summary>
    /// Uniform 绑定声明
    /// </summary>
    UniformBindingDecl
}
