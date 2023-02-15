namespace LanguageModel;

public interface IStatementVisitor
{
    void VisitInvocation(Invocation statement);
    void VisitAssignVariable(AssignVariable statement);
    void VisitPrintVariable(PrintVariable statement);
    void VisitVariableDeclaration(VariableDeclaration statement);
    void VisitFunctionDeclaration(FunctionDeclaration statement);
}