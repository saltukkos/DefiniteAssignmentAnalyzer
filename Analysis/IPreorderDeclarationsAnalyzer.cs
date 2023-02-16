using LanguageModel;

namespace Analysis;

public interface IPreorderDeclarationsAnalyzer<TContext>
{
    TContext CreateEmptyContext(IProgramDeclarations declarations);

    TContext CreateChildContext(TContext context, IProgramDeclarations functionNestedDeclarations);

    void AnalyzeVariableDeclaration(TContext context, VariableDeclaration declaration);

    void AnalyzeAssignVariable(TContext context, AssignVariable statement);

    void AnalyzePrintVariable(TContext context, PrintVariable statement);

    void AnalyzeInvocation(TContext context, Invocation invocation);
}