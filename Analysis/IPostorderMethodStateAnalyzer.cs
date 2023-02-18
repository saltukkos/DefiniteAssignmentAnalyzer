using LanguageModel;

namespace Analysis;

public interface IPostorderMethodStateAnalyzer<TContext>
{
    TContext CreateEmptyContext(IDeclarationScope declarations);

    void AnalyzeAssignVariable(TContext context, AssignVariable statement);

    void AnalyzePrintVariable(TContext context, PrintVariable statement);

    bool NeedToProcessInvocation(Invocation invocation);

    void AnalyzeInvocation(TContext context, Invocation invocation, TContext invokedMethodContext,
        IInvokedMethodContextProvider contextProvider);
}