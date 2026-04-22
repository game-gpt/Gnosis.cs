namespace Gnosis.Compiler.AST;

/// <summary>
/// AST 访问者接口，用于实现访问者模式
/// </summary>
/// <typeparam name="T">访问操作的返回类型</typeparam>
/// <remarks>
/// 使用示例：
/// <code>
/// class Printer : IAstVisitor&lt;string&gt;
/// {
///     public string VisitBinaryExpr(BinaryExpr node) =&gt;
///         $"({Visit(node.Left)} {node.Operator} {Visit(node.Right)})";
///     
///     public string VisitLiteralExpr(LiteralExpr node) =&gt;
///         node.Value?.ToString() ?? "null";
///     
///     // ... 其他 Visit 方法
/// }
/// </code>
/// </remarks>
public interface IAstVisitor<out T>
{
    T VisitCompilationUnit(CompilationUnit node);
    T VisitComponentDecl(ComponentDecl node);
    T VisitSystemDecl(SystemDecl node);
    T VisitWidgetDecl(WidgetDecl node);
    T VisitPluginDecl(PluginDecl node);
    T VisitFunctionDecl(FunctionDecl node);
    T VisitVariableDecl(VariableDecl node);
    T VisitImportDecl(ImportDecl node);
    T VisitFieldDecl(FieldDecl node);
    T VisitParameterDecl(ParameterDecl node);
    T VisitTypeAnnotation(TypeAnnotation node);
    T VisitAttributeDecl(AttributeDecl node);
    T VisitQueryExpr(QueryExpr node);
    T VisitMetaBlock(MetaBlock node);
    T VisitBlockStmt(BlockStmt node);
    T VisitIfStmt(IfStatement node);
    T VisitLoopStmt(LoopStmt node);
    T VisitWhileStmt(WhileStmt node);
    T VisitReturnStmt(ReturnStatement node);
    T VisitExprStmt(TermExpressionStatement node);
    T VisitBinaryExpr(BinaryExpr node);
    T VisitUnaryExpr(TermUnaryExpression node);
    T VisitCallExpr(TermCallExpression node);
    T VisitMemberAccessExpr(MemberAccessExpr node);
    T VisitIndexExpr(TermIndexExpression node);
    T VisitLiteralExpr(LiteralExpr node);
    T VisitIdentifierExpr(IdentifierNode node);
    T VisitLambdaExpr(LambdaExpr node);
    T VisitAssignmentExpr(AssignmentExpr node);
    T VisitStructDecl(StructDecl node);
    T VisitForStmt(ForStmt node);
    T VisitDiscardStmt(DiscardStmt node);
    T VisitSwizzleExpr(SwizzleExpr node);
    T VisitUsingDecl(UsingDecl node);
    T VisitUniformBindingDecl(UniformBindingDecl node);
    T VisitNeuralDecl(NeuralDecl node);
    T VisitTensorTypeExpr(TensorTypeExpr node);
    T VisitTensorDimension(TensorDimension node);
}
