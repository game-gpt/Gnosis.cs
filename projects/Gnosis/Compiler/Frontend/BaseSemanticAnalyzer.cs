using Gnosis.Compiler.AST;
using Gnosis.Compiler.Diagnostics;

namespace Gnosis.Compiler.Frontend;

public abstract class BaseSemanticAnalyzer
{
    #region Fields

    protected readonly DiagnosticSink _diagnostics;
    protected readonly Dictionary<string, FunctionDecl> _functions = new(StringComparer.Ordinal);
    protected readonly Dictionary<string, VariableDecl> _variables = new(StringComparer.Ordinal);
    protected readonly Stack<Dictionary<string, TypeAnnotation?>> _scopes = new();

    #endregion

    #region Constructors

    protected BaseSemanticAnalyzer(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region Public Methods

    public CompilationUnit Analyze(CompilationUnit unit)
    {
        BuildSymbolTable(unit);
        ValidateDeclarations(unit);
        return unit;
    }

    #endregion

    #region Protected Methods

    protected abstract void BuildSymbolTable(CompilationUnit unit);

    protected abstract void ValidateDeclarations(CompilationUnit unit);

    protected virtual bool IsKnownIdentifier(string name)
    {
        return _functions.ContainsKey(name);
    }

    protected void PushScope()
    {
        _scopes.Push(new Dictionary<string, TypeAnnotation?>(StringComparer.Ordinal));
    }

    protected void PopScope()
    {
        if (_scopes.Count > 0)
        {
            _scopes.Pop();
        }
    }

    protected void AddVariable(string name, TypeAnnotation? type)
    {
        if (_scopes.Count > 0)
        {
            _scopes.Peek()[name] = type;
        }
    }

    protected bool IsVariableInScope(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                return true;
            }
        }

        return false;
    }

    protected void ValidateExpression(AstNode expr)
    {
        switch (expr.Type)
        {
            case NodeType.IdentifierExpr:
                var idExpr = (IdentifierExpr)expr;
                if (!IsVariableInScope(idExpr.Name) && !IsKnownIdentifier(idExpr.Name))
                {
                    _diagnostics.AddWarning(
                        string.Empty,
                        idExpr.Span,
                        "GG0430",
                        $"未定义的标识符: {idExpr.Name}");
                }
                break;

            case NodeType.BinaryExpr:
                var binExpr = (BinaryExpr)expr;
                ValidateExpression(binExpr.Left);
                ValidateExpression(binExpr.Right);
                break;

            case NodeType.UnaryExpr:
                ValidateExpression(((UnaryExpr)expr).Operand);
                break;

            case NodeType.CallExpr:
                var callExpr = (CallExpr)expr;
                ValidateExpression(callExpr.Callee);
                foreach (var arg in callExpr.Arguments)
                {
                    ValidateExpression(arg);
                }
                break;

            case NodeType.MemberAccessExpr:
                var memberExpr = (MemberAccessExpr)expr;
                ValidateExpression(memberExpr.Object);
                break;

            case NodeType.IndexExpr:
                var indexExpr = (IndexExpr)expr;
                ValidateExpression(indexExpr.Object);
                ValidateExpression(indexExpr.Index);
                break;

            case NodeType.AssignmentExpr:
                var assignExpr = (AssignmentExpr)expr;
                ValidateExpression(assignExpr.Target);
                ValidateExpression(assignExpr.Value);
                break;

            case NodeType.LambdaExpr:
                var lambdaExpr = (LambdaExpr)expr;
                PushScope();
                foreach (var param in lambdaExpr.Parameters)
                {
                    AddVariable(param.Name, param.ParamType);
                }
                ValidateExpression(lambdaExpr.Body);
                PopScope();
                break;

            case NodeType.SwizzleExpr:
                var swizzleExpr = (SwizzleExpr)expr;
                ValidateExpression(swizzleExpr.Object);
                break;
        }
    }

    protected void ValidateBlockStmt(BlockStmt block)
    {
        PushScope();

        foreach (var stmt in block.Statements)
        {
            ValidateStatement(stmt);
        }

        PopScope();
    }

    protected void ValidateStatement(AstNode stmt)
    {
        switch (stmt.Type)
        {
            case NodeType.VariableDecl:
                var varDecl = (VariableDecl)stmt;
                AddVariable(varDecl.Name, varDecl.VarType);
                if (varDecl.Initializer is not null)
                {
                    ValidateExpression(varDecl.Initializer);
                }
                break;

            case NodeType.IfStmt:
                var ifStmt = (IfStmt)stmt;
                ValidateExpression(ifStmt.Condition);
                ValidateStatement(ifStmt.ThenBlock);
                if (ifStmt.ElseBlock is not null)
                {
                    ValidateStatement(ifStmt.ElseBlock);
                }
                break;

            case NodeType.LoopStmt:
                var loopStmt = (LoopStmt)stmt;
                if (loopStmt.Iterable is not null)
                {
                    ValidateExpression(loopStmt.Iterable);
                }
                ValidateBlockStmt(loopStmt.Body);
                break;

            case NodeType.WhileStmt:
                var whileStmt = (WhileStmt)stmt;
                ValidateExpression(whileStmt.Condition);
                ValidateBlockStmt(whileStmt.Body);
                break;

            case NodeType.ReturnStmt:
                var returnStmt = (ReturnStmt)stmt;
                if (returnStmt.Value is not null)
                {
                    ValidateExpression(returnStmt.Value);
                }
                break;

            case NodeType.ExprStmt:
                ValidateExpression(((ExprStmt)stmt).Expression);
                break;

            case NodeType.BlockStmt:
                ValidateBlockStmt((BlockStmt)stmt);
                break;

            case NodeType.ForStmt:
                var forStmt = (ForStmt)stmt;
                PushScope();
                if (forStmt.Initializer is not null)
                {
                    ValidateStatement(forStmt.Initializer);
                }
                if (forStmt.Condition is not null)
                {
                    ValidateExpression(forStmt.Condition);
                }
                if (forStmt.Update is not null)
                {
                    ValidateExpression(forStmt.Update);
                }
                ValidateBlockStmt(forStmt.Body);
                PopScope();
                break;

            case NodeType.DiscardStmt:
                break;
        }
    }

    #endregion
}
