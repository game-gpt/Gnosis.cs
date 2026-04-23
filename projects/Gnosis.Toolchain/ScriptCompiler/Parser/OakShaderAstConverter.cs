using Gnosis.Core.Diagnostic;
using Gnosis.Toolchain.ScriptCompiler.AST;
using OakAstNode = Oak.GGShader.AST.AstNode;

namespace Gnosis.Toolchain.ScriptCompiler.Parser;

/// <summary>
///     Oak.GGShader AST 到 Gnosis.Toolchain AST 的转换器
/// </summary>
public static class OakShaderAstConverter
{
    /// <summary>
    ///     转换 Oak AST 为 Gnosis AST
    /// </summary>
    public static AstNode Convert(OakAstNode node)
    {
        return node switch
        {
            Oak.GGShader.AST.CompilationUnit n => ConvertCompilationUnit(n),
            Oak.GGShader.AST.ImportDecl n => ConvertImportDecl(n),
            Oak.GGShader.AST.UsingDecl n => ConvertUsingDecl(n),
            Oak.GGShader.AST.VariableDecl n => ConvertVariableDecl(n),
            Oak.GGShader.AST.UniformBindingDecl n => ConvertUniformBindingDecl(n),
            Oak.GGShader.AST.StructDecl n => ConvertStructDecl(n),
            Oak.GGShader.AST.ComponentDecl n => ConvertComponentDecl(n),
            Oak.GGShader.AST.FunctionDecl n => ConvertFunctionDecl(n),
            Oak.GGShader.AST.ParameterDecl n => ConvertParameterDecl(n),
            Oak.GGShader.AST.FieldDecl n => ConvertFieldDecl(n),
            Oak.GGShader.AST.AttributeDecl n => ConvertAttributeDecl(n),
            Oak.GGShader.AST.BlockStmt n => ConvertBlockStmt(n),
            Oak.GGShader.AST.IfStatement n => ConvertIfStatement(n),
            Oak.GGShader.AST.ForStmt n => ConvertForStmt(n),
            Oak.GGShader.AST.WhileStmt n => ConvertWhileStmt(n),
            Oak.GGShader.AST.LoopStmt n => ConvertLoopStmt(n),
            Oak.GGShader.AST.ReturnStatement n => ConvertReturnStatement(n),
            Oak.GGShader.AST.DiscardStmt n => ConvertDiscardStmt(n),
            Oak.GGShader.AST.TermExpressionStatement n => ConvertTermExpressionStatement(n),
            Oak.GGShader.AST.BinaryExpr n => ConvertBinaryExpr(n),
            Oak.GGShader.AST.AssignmentExpr n => ConvertAssignmentExpr(n),
            Oak.GGShader.AST.MemberAccessExpr n => ConvertMemberAccessExpr(n),
            Oak.GGShader.AST.SwizzleExpr n => ConvertSwizzleExpr(n),
            Oak.GGShader.AST.LiteralExpr n => ConvertLiteralExpr(n),
            Oak.GGShader.AST.IdentifierNode n => ConvertIdentifierNode(n),
            Oak.GGShader.AST.TermCallExpression n => ConvertTermCallExpression(n),
            Oak.GGShader.AST.TermIndexExpression n => ConvertTermIndexExpression(n),
            Oak.GGShader.AST.TermUnaryExpression n => ConvertTermUnaryExpression(n),
            Oak.GGShader.AST.LambdaExpr n => ConvertLambdaExpr(n),
            Oak.GGShader.AST.TypeAnnotation n => ConvertTypeAnnotation(n),
            Oak.GGShader.AST.MetaBlock n => ConvertMetaBlock(n),
            Oak.GGShader.AST.NeuralDecl n => ConvertNeuralDecl(n),
            _ => throw new ParseException($"未知的 Oak GGShader AST 节点类型: {node.GetType().Name}")
        };
    }

    private static SourceSpan? ConvertSpan(Oak.Core.Diagnostics.SourceSpan? span)
    {
        return span == null ? null : new SourceSpan("", span.Value.StartLine, span.Value.StartColumn, span.Value.EndLine, span.Value.EndColumn);
    }

    private static CompilationUnit ConvertCompilationUnit(Oak.GGShader.AST.CompilationUnit node)
    {
        return new CompilationUnit(
            node.Declarations.Select(Convert).ToList(),
            node.FilePath,
            ConvertSpan(node.Span));
    }

    private static ImportDecl ConvertImportDecl(Oak.GGShader.AST.ImportDecl node)
    {
        return new ImportDecl(node.Path, node.Alias, ConvertSpan(node.Span));
    }

    private static UsingDecl ConvertUsingDecl(Oak.GGShader.AST.UsingDecl node)
    {
        return new UsingDecl(node.Namespace, node.Selections, ConvertSpan(node.Span));
    }

    private static VariableDecl ConvertVariableDecl(Oak.GGShader.AST.VariableDecl node)
    {
        return new VariableDecl(
            node.Name,
            node.Type == null ? null : ConvertTypeAnnotation(node.Type),
            node.Initializer == null ? null : Convert(node.Initializer),
            node.IsMutable,
            ConvertSpan(node.Span));
    }

    private static UniformBindingDecl ConvertUniformBindingDecl(Oak.GGShader.AST.UniformBindingDecl node)
    {
        return new UniformBindingDecl(
            node.Name,
            node.BindingType,
            ConvertTypeAnnotation(node.TypeAnnotation),
            node.Group,
            node.Binding,
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static StructDecl ConvertStructDecl(Oak.GGShader.AST.StructDecl node)
    {
        return new StructDecl(
            node.Name,
            node.Fields.Select(ConvertFieldDecl).ToList(),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static ComponentDecl ConvertComponentDecl(Oak.GGShader.AST.ComponentDecl node)
    {
        return new ComponentDecl(
            node.Name,
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            node.Fields.Select(ConvertFieldDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static FunctionDecl ConvertFunctionDecl(Oak.GGShader.AST.FunctionDecl node)
    {
        return new FunctionDecl(
            node.Name,
            node.Parameters.Select(ConvertParameterDecl).ToList(),
            node.ReturnType == null ? null : ConvertTypeAnnotation(node.ReturnType),
            node.Body == null ? null : ConvertBlockStmt(node.Body),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static ParameterDecl ConvertParameterDecl(Oak.GGShader.AST.ParameterDecl node)
    {
        return new ParameterDecl(
            node.Name,
            ConvertTypeAnnotation(node.Type),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static FieldDecl ConvertFieldDecl(Oak.GGShader.AST.FieldDecl node)
    {
        return new FieldDecl(
            node.Name,
            ConvertTypeAnnotation(node.Type),
            node.DefaultValue == null ? null : Convert(node.DefaultValue),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }

    private static AttributeDecl ConvertAttributeDecl(Oak.GGShader.AST.AttributeDecl node)
    {
        return new AttributeDecl(
            node.Name,
            node.Arguments.Select(a => new KeyValuePair<string, string>(a.Key, a.Value)).ToList(),
            ConvertSpan(node.Span));
    }

    private static BlockStmt ConvertBlockStmt(Oak.GGShader.AST.BlockStmt node)
    {
        return new BlockStmt(
            node.Statements.Select(Convert).ToList(),
            ConvertSpan(node.Span));
    }

    private static IfStatement ConvertIfStatement(Oak.GGShader.AST.IfStatement node)
    {
        return new IfStatement(
            Convert(node.Condition),
            Convert(node.ThenBlock),
            node.ElseBlock == null ? null : Convert(node.ElseBlock),
            ConvertSpan(node.Span));
    }

    private static ForStmt ConvertForStmt(Oak.GGShader.AST.ForStmt node)
    {
        return new ForStmt(
            node.Initializer == null ? null : Convert(node.Initializer),
            node.Condition == null ? null : Convert(node.Condition),
            node.Update == null ? null : Convert(node.Update),
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static WhileStmt ConvertWhileStmt(Oak.GGShader.AST.WhileStmt node)
    {
        return new WhileStmt(
            Convert(node.Condition),
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static LoopStmt ConvertLoopStmt(Oak.GGShader.AST.LoopStmt node)
    {
        return new LoopStmt(
            node.IteratorName,
            node.Iterable == null ? null : Convert(node.Iterable),
            ConvertBlockStmt(node.Body),
            ConvertSpan(node.Span));
    }

    private static ReturnStatement ConvertReturnStatement(Oak.GGShader.AST.ReturnStatement node)
    {
        return new ReturnStatement(
            node.Value == null ? null : Convert(node.Value),
            ConvertSpan(node.Span));
    }

    private static DiscardStmt ConvertDiscardStmt(Oak.GGShader.AST.DiscardStmt node)
    {
        return new DiscardStmt(ConvertSpan(node.Span));
    }

    private static TermExpressionStatement ConvertTermExpressionStatement(Oak.GGShader.AST.TermExpressionStatement node)
    {
        return new TermExpressionStatement(
            Convert(node.Expression),
            ConvertSpan(node.Span));
    }

    private static BinaryExpr ConvertBinaryExpr(Oak.GGShader.AST.BinaryExpr node)
    {
        return new BinaryExpr(
            Convert(node.Left),
            node.Operator,
            Convert(node.Right),
            ConvertSpan(node.Span));
    }

    private static AssignmentExpr ConvertAssignmentExpr(Oak.GGShader.AST.AssignmentExpr node)
    {
        return new AssignmentExpr(
            Convert(node.Left),
            node.Operator,
            Convert(node.Right),
            ConvertSpan(node.Span));
    }

    private static MemberAccessExpr ConvertMemberAccessExpr(Oak.GGShader.AST.MemberAccessExpr node)
    {
        return new MemberAccessExpr(
            Convert(node.Object),
            node.MemberName,
            ConvertSpan(node.Span));
    }

    private static SwizzleExpr ConvertSwizzleExpr(Oak.GGShader.AST.SwizzleExpr node)
    {
        return new SwizzleExpr(
            Convert(node.Object),
            node.Components,
            ConvertSpan(node.Span));
    }

    private static LiteralExpr ConvertLiteralExpr(Oak.GGShader.AST.LiteralExpr node)
    {
        var kind = node.LiteralKind switch
        {
            Oak.GGShader.AST.LiteralType.Number => LiteralType.Number,
            Oak.GGShader.AST.LiteralType.String => LiteralType.String,
            Oak.GGShader.AST.LiteralType.Boolean => LiteralType.Boolean,
            Oak.GGShader.AST.LiteralType.Null => LiteralType.Null,
            _ => LiteralType.Null
        };

        return new LiteralExpr(kind, node.Value, ConvertSpan(node.Span));
    }

    private static IdentifierNode ConvertIdentifierNode(Oak.GGShader.AST.IdentifierNode node)
    {
        return new IdentifierNode(node.Name, ConvertSpan(node.Span));
    }

    private static TermCallExpression ConvertTermCallExpression(Oak.GGShader.AST.TermCallExpression node)
    {
        return new TermCallExpression(
            Convert(node.Callee),
            node.Arguments.Select(Convert).ToList(),
            ConvertSpan(node.Span));
    }

    private static TermIndexExpression ConvertTermIndexExpression(Oak.GGShader.AST.TermIndexExpression node)
    {
        return new TermIndexExpression(
            Convert(node.Object),
            Convert(node.Index),
            ConvertSpan(node.Span));
    }

    private static TermUnaryExpression ConvertTermUnaryExpression(Oak.GGShader.AST.TermUnaryExpression node)
    {
        return new TermUnaryExpression(
            node.Operator,
            Convert(node.Operand),
            node.IsPrefix,
            ConvertSpan(node.Span));
    }

    private static LambdaExpr ConvertLambdaExpr(Oak.GGShader.AST.LambdaExpr node)
    {
        return new LambdaExpr(
            node.Parameters.Select(ConvertParameterDecl).ToList(),
            Convert(node.Body),
            ConvertSpan(node.Span));
    }

    private static TypeAnnotation ConvertTypeAnnotation(Oak.GGShader.AST.TypeAnnotation node)
    {
        return new TypeAnnotation(
            node.Name,
            node.GenericArguments.Select(ConvertTypeAnnotation).ToList(),
            ConvertSpan(node.Span));
    }

    private static MetaBlock ConvertMetaBlock(Oak.GGShader.AST.MetaBlock node)
    {
        return new MetaBlock(node.Content, ConvertSpan(node.Span));
    }

    private static NeuralDecl ConvertNeuralDecl(Oak.GGShader.AST.NeuralDecl node)
    {
        return new NeuralDecl(
            node.Name,
            node.GenericParameters.Select(ConvertParameterDecl).ToList(),
            node.Weights.Select(ConvertFieldDecl).ToList(),
            ConvertFunctionDecl(node.ForwardFunction),
            node.Attributes.Select(ConvertAttributeDecl).ToList(),
            ConvertSpan(node.Span));
    }
}
