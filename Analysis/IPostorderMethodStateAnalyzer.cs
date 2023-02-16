using LanguageModel;

namespace Analysis;

public interface IPostorderMethodStateAnalyzer<TContext>
{
    TContext CreateEmptyContext(IProgramDeclarations declarations);

    void AnalyzeAssignVariable(TContext context, AssignVariable statement);

    void AnalyzePrintVariable(TContext context, PrintVariable statement);

    bool NeedToProcessInvocation(Invocation invocation);

    void AnalyzeInvocation(TContext context, Invocation invocation, TContext invokedMethodContext,
        IInvokedMethodContextProvider contextProvider);
}