namespace Gnosis.Compiler.AST;

public interface IAstVisitor<out T>
{
    T VisitCompilationUnit(CompilationUnit node);
    T VisitComponentDecl(ComponentDecl node);
    T VisitSystemDecl(SystemDecl node);
    T VisitWidgetDecl(WidgetDecl node);
    T VisitSceneDecl(SceneDecl node);
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
    T VisitIfStmt(IfStmt node);
    T VisitLoopStmt(LoopStmt node);
    T VisitWhileStmt(WhileStmt node);
    T VisitReturnStmt(ReturnStmt node);
    T VisitExprStmt(ExprStmt node);
    T VisitBinaryExpr(BinaryExpr node);
    T VisitUnaryExpr(UnaryExpr node);
    T VisitCallExpr(CallExpr node);
    T VisitMemberAccessExpr(MemberAccessExpr node);
    T VisitIndexExpr(IndexExpr node);
    T VisitLiteralExpr(LiteralExpr node);
    T VisitIdentifierExpr(IdentifierExpr node);
    T VisitLambdaExpr(LambdaExpr node);
    T VisitAssignmentExpr(AssignmentExpr node);
    T VisitStructDecl(StructDecl node);
    T VisitForStmt(ForStmt node);
    T VisitDiscardStmt(DiscardStmt node);
    T VisitSwizzleExpr(SwizzleExpr node);
    T VisitUsingDecl(UsingDecl node);
    T VisitUniformBindingDecl(UniformBindingDecl node);
}
