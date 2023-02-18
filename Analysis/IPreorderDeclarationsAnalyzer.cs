using LanguageModel;

namespace Analysis;

public interface IPreorderDeclarationsAnalyzer<TContext>
{
    TContext CreateEmptyContext(IDeclarationScope declarations);

    TContext CreateChildContext(TContext context, IDeclarationScope nestedDeclarations);

    void AnalyzeVariableDeclaration(TContext context, VariableDeclaration declaration);

    void AnalyzeAssignVariable(TContext context, AssignVariable statement);

    void AnalyzePrintVariable(TContext context, PrintVariable statement);

    void AnalyzeInvocation(TContext context, Invocation invocation);
}