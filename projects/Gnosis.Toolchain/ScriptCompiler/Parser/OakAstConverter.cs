using Gnosis.Core.Diagnostic;
using Gnosis.Toolchain.ScriptCompiler.AST;
using OakAstNode = Oak.GGScript.AST.AstNode;
using OakNodeType = Oak.GGScript.AST.NodeType;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

/// <summary>
///     Oak.GGScript AST 到 Gnosis.Toolchain AST 的转换器
/// </summary>
public static class OakAstConverter
{
    /// <summary>
    ///     转换 Oak AST 为 Gnosis AST
    /// </summary>
    public static AstNode Convert(OakAstNode node)
    {
        return node switch
        {
            Oak.GGScript.AST.CompilationUnit n => ConvertCompilationUnit(n),
            Oak.GGScript.AST.ComponentDecl n => ConvertComponentDecl(n),
            Oak.GGScript.AST.SystemDecl n => ConvertSystemDecl(n),
            Oak.GGScript.AST.WidgetDecl n => ConvertWidgetDecl(n),
            Oak.GGScript.AST.PluginDecl n => ConvertPluginDecl(n),
            Oak.GGScript.AST.FunctionDecl n => ConvertFunctionDecl(n),
            Oak.GGScript.AST.VariableDecl n => ConvertVariableDecl(n),
            Oak.GGScript.AST.ImportDecl n => ConvertImportDecl(n),
            Oak.GGScript.AST.FieldDecl n => ConvertFieldDecl(n),
            Oak.GGScript.AST.ParameterDecl n => ConvertParameterDecl(n),
            Oak.GGScript.AST.TypeAnnotation n => ConvertTypeAnnotation(n),
            Oak.GGScript.AST.AttributeDecl n => ConvertAttributeDecl(n),
            Oak.GGScript.AST.QueryExpr n => ConvertQueryExpr(n),
            Oak.GGScript.AST.MetaBlock n => ConvertMetaBlock(n),
            Oak.GGScript.AST.BlockStmt n => ConvertBlockStmt(n),
            Oak.GGScript.AST.IfStatement n => ConvertIfStatement(n),
            Oak.GGScript.AST.ForStmt n => ConvertForStmt(n),
            Oak.GGScript.AST.LoopStmt n => ConvertLoopStmt(n),
            Oak.GGScript.AST.WhileStmt n => ConvertWhileStmt(n),
            Oak.GGScript.AST.ReturnStatement n => ConvertReturnStatement(n),
            Oak.GGScript.AST.DiscardStmt n => ConvertDiscardStmt(n),
            Oak.GGScript.AST.BinaryExpr n => ConvertBinaryExpr(n),
            Oak.GGScript.AST.AssignmentExpr n => ConvertAssignmentExpr(n),
            Oak.GGScript.AST.MemberAccessExpr n => ConvertMemberAccessExpr(n),
            Oak.GGScript.AST.SwizzleExpr n => ConvertSwizzleExpr(n),
            Oak.GGScript.AST.LiteralExpr n => ConvertLiteralExpr(n),
            Oak.GGScript.AST.IdentifierNode n => ConvertIdentifierNode(n),
            Oak.GGScript.AST.LambdaExpr n => ConvertLambdaExpr(n),
            Oak.GGScript.AST.StructDecl n => ConvertStructDecl(n),
            Oak.GGScript.AST.UniformBindingDecl n => ConvertUniformBindingDecl(n),
            Oak.GGScript.AST.NeuralDecl n => ConvertNeuralDecl(n),
            Oak.GGScript.AST.TensorTypeExpr n => ConvertTensorTypeExpr(n),
            Oak.GGScript.AST.TensorDimension n => ConvertTensorDimension(n),
            Oak.GGScript.AST.UsingDecl n => ConvertUsingDecl(n),
            Oak.GGScript.AST.TermCallExpression n => ConvertTermCallExpression(n),
            Oak.GGScript.AST.TermExpressionStatement n => ConvertTermExpressionStatement(n),
            Oak.GGScript.AST.TermIndexExpression n => ConvertTermIndexExpression(n),
            Oak.GGScript.AST.TermUnaryExpression n => ConvertTermUnaryExpression(n),
            _ => throw new ParseException($"未知的 Oak AST 节点类型: {node.GetType().Name}")
        };
    }

    private static SourceSpan? ConvertSpan(Oak.Core.Diagnostics.SourceSpan? span)
    {
        return span == null ? null : new SourceSpan("", span.Value.StartLine, span.Value.StartColumn, span.Value.EndLine, span.Value.EndColumn);
    }

    private static CompilationUnit ConvertCompilationUnit(Oak.GGScript.AST.CompilationUnit node)
    {
        return new CompilationUnit(
            node.Declarations.Select(Convert).ToList(),
            node.FilePath,
            ConvertSpan(node.Span));
    }

    private static ComponentDecl ConvertComponentDecl(Oak.GGScript.AST.ComponentDecl node)
    {
        return new ComponentDecl(
            node.Name,
            node.Fields.Select(ConvertFieldDecl).ToList(),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static SystemDecl ConvertSystemDecl(Oak.GGScript.AST.SystemDecl node)
    {
        return new SystemDecl(
            node.Name,
            node.Fields.Select(ConvertFieldDecl).ToList(),
            node.Micros.Select(ConvertFunctionDecl).ToList(),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static WidgetDecl ConvertWidgetDecl(Oak.GGScript.AST.WidgetDecl node)
    {
        return new WidgetDecl(
            node.Name,
            node.Fields.Select(ConvertFieldDecl).ToList(),
            node.Micros.Select(ConvertFunctionDecl).ToList(),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static PluginDecl ConvertPluginDecl(Oak.GGScript.AST.PluginDecl node)
    {
        return new PluginDecl(
            node.Name,
            node.Fields.Select(ConvertFieldDecl).ToList(),
            node.Micros.Select(ConvertFunctionDecl).ToList(),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static FunctionDecl ConvertFunctionDecl(Oak.GGScript.AST.FunctionDecl node)
    {
        return new FunctionDecl(
            node.Name,
            node.Parameters.Select(ConvertParameterDecl).ToList(),
            node.ReturnType == null ? null : ConvertTypeAnnotation(node.ReturnType),
            node.Body == null ? null : ConvertBlockStmt(node.Body),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static VariableDecl ConvertVariableDecl(Oak.GGScript.AST.VariableDecl node)
    {
        return new VariableDecl(
            node.Name,
            node.Type == null ? null : ConvertTypeAnnotation(node.Type),
            node.Initializer == null ? null : Convert(node.Initializer),
            ConvertSpan(node.Span));
    }

    private static ImportDecl ConvertImportDecl(Oak.GGScript.AST.ImportDecl node)
    {
        return new ImportDecl(
            node.Path,
            node.Alias,
            ConvertSpan(node.Span));
    }

    private static FieldDecl ConvertFieldDecl(Oak.GGScript.AST.FieldDecl node)
    {
        return new FieldDecl(
            node.Name,
            ConvertTypeAnnotation(node.Type),
            node.DefaultValue == null ? null : Convert(node.DefaultValue),
            ConvertSpan(node.Span));
    }

    private static ParameterDecl ConvertParameterDecl(Oak.GGScript.AST.ParameterDecl node)
    {
        return new ParameterDecl(
            node.Name,
            ConvertTypeAnnotation(node.Type),
            node.DefaultValue == null ? null : Convert(node.DefaultValue),
            ConvertSpan(node.Span));
    }

    private static TypeAnnotation ConvertTypeAnnotation(Oak.GGScript.AST.TypeAnnotation node)
    {
        return new TypeAnnotation(
            node.TypeName,
            node.IsArray,
            node.IsOptional,
            ConvertSpan(node.Span));
    }

    private static AttributeDecl ConvertAttributeDecl(Oak.GGScript.AST.AttributeDecl node)
    {
        return new AttributeDecl(
            node.Name,
            node.Arguments.Select(Convert).ToList(),
            ConvertSpan(node.Span));
    }

    private static QueryExpr ConvertQueryExpr(Oak.GGScript.AST.QueryExpr node)
    {
        return new QueryExpr(
            node.QueryType,
            node.Components.Select(ConvertIdentifierNode).ToList(),
            ConvertSpan(node.Span));
    }

    private static MetaBlock ConvertMetaBlock(Oak.GGScript.AST.MetaBlock node)
    {
        return new MetaBlock(
            node.Content,
            ConvertSpan(node.Span));
    }

    private static BlockStmt ConvertBlockStmt(Oak.GGScript.AST.BlockStmt node)
    {
        return new BlockStmt(
            node.Statements.Select(Convert).ToList(),
            ConvertSpan(node.Span));
    }

    private static IfStatement ConvertIfStatement(Oak.GGScript.AST.IfStatement node)
    {
        return new IfStatement(
            Convert(node.Condition),
            Convert(node.ThenBlock),
            node.ElseBlock == null ? null : Convert(node.ElseBlock),
            ConvertSpan(node.Span));
    }

    private static ForStmt ConvertForStmt(Oak.GGScript.AST.ForStmt node)
    {
        return new ForStmt(
            node.IteratorName,
            Convert(node.Collection),
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static LoopStmt ConvertLoopStmt(Oak.GGScript.AST.LoopStmt node)
    {
        return new LoopStmt(
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static WhileStmt ConvertWhileStmt(Oak.GGScript.AST.WhileStmt node)
    {
        return new WhileStmt(
            Convert(node.Condition),
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static ReturnStatement ConvertReturnStatement(Oak.GGScript.AST.ReturnStatement node)
    {
        return new ReturnStatement(
            node.Value == null ? null : Convert(node.Value),
            ConvertSpan(node.Span));
    }

    private static DiscardStmt ConvertDiscardStmt(Oak.GGScript.AST.DiscardStmt node)
    {
        return new DiscardStmt(ConvertSpan(node.Span));
    }

    private static BinaryExpr ConvertBinaryExpr(Oak.GGScript.AST.BinaryExpr node)
    {
        return new BinaryExpr(
            Convert(node.Left),
            node.Operator,
            Convert(node.Right),
            ConvertSpan(node.Span));
    }

    private static AssignmentExpr ConvertAssignmentExpr(Oak.GGScript.AST.AssignmentExpr node)
    {
        return new AssignmentExpr(
            Convert(node.Left),
            node.Operator,
            Convert(node.Right),
            ConvertSpan(node.Span));
    }

    private static MemberAccessExpr ConvertMemberAccessExpr(Oak.GGScript.AST.MemberAccessExpr node)
    {
        return new MemberAccessExpr(
            Convert(node.Object),
            node.MemberName,
            ConvertSpan(node.Span));
    }

    private static SwizzleExpr ConvertSwizzleExpr(Oak.GGScript.AST.SwizzleExpr node)
    {
        return new SwizzleExpr(
            Convert(node.Object),
            node.Components,
            ConvertSpan(node.Span));
    }

    private static LiteralExpr ConvertLiteralExpr(Oak.GGScript.AST.LiteralExpr node)
    {
        return new LiteralExpr(
            node.Value,
            ConvertSpan(node.Span));
    }

    private static IdentifierNode ConvertIdentifierNode(Oak.GGScript.AST.IdentifierNode node)
    {
        return new IdentifierNode(
            node.Name,
            ConvertSpan(node.Span));
    }

    private static LambdaExpr ConvertLambdaExpr(Oak.GGScript.AST.LambdaExpr node)
    {
        return new LambdaExpr(
            node.Parameters.Select(ConvertParameterDecl).ToList(),
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static StructDecl ConvertStructDecl(Oak.GGScript.AST.StructDecl node)
    {
        return new StructDecl(
            node.Name,
            node.Fields.Select(ConvertFieldDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static UniformBindingDecl ConvertUniformBindingDecl(Oak.GGScript.AST.UniformBindingDecl node)
    {
        return new UniformBindingDecl(
            node.Name,
            ConvertTypeAnnotation(node.Type),
            node.Slot,
            ConvertSpan(node.Span));
    }

    private static NeuralDecl ConvertNeuralDecl(Oak.GGScript.AST.NeuralDecl node)
    {
        return new NeuralDecl(
            node.Name,
            ConvertTypeAnnotation(node.Type),
            node.Dimensions.Select(ConvertTensorDimension).ToList(),
            ConvertSpan(node.Span));
    }

    private static TensorTypeExpr ConvertTensorTypeExpr(Oak.GGScript.AST.TensorTypeExpr node)
    {
        return new TensorTypeExpr(
            node.ElementType,
            node.Dimensions.Select(ConvertTensorDimension).ToList(),
            ConvertSpan(node.Span));
    }

    private static TensorDimension ConvertTensorDimension(Oak.GGScript.AST.TensorDimension node)
    {
        return new TensorDimension(
            node.Size,
            ConvertSpan(node.Span));
    }

    private static UsingDecl ConvertUsingDecl(Oak.GGScript.AST.UsingDecl node)
    {
        return new UsingDecl(
            node.Namespace,
            ConvertSpan(node.Span));
    }

    private static TermCallExpression ConvertTermCallExpression(Oak.GGScript.AST.TermCallExpression node)
    {
        return new TermCallExpression(
            Convert(node.Callee),
            node.Arguments.Select(Convert).ToList(),
            ConvertSpan(node.Span));
    }

    private static TermExpressionStatement ConvertTermExpressionStatement(Oak.GGScript.AST.TermExpressionStatement node)
    {
        return new TermExpressionStatement(
            Convert(node.Expression),
            ConvertSpan(node.Span));
    }

    private static TermIndexExpression ConvertTermIndexExpression(Oak.GGScript.AST.TermIndexExpression node)
    {
        return new TermIndexExpression(
            Convert(node.Object),
            Convert(node.Index),
            ConvertSpan(node.Span));
    }

    private static TermUnaryExpression ConvertTermUnaryExpression(Oak.GGScript.AST.TermUnaryExpression node)
    {
        return new TermUnaryExpression(
            node.Operator,
            Convert(node.Operand),
            ConvertSpan(node.Span));
    }
}
