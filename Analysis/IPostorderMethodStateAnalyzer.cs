using LanguageModel;

namespace Analysis;

public interface IPostorderMethodStateAnalyzer<TContext>
{
    TContext CreateEmptyContext();

    void AnalyzeAssignVariable(TContext context, AssignVariable statement);

    void AnalyzePrintVariable(TContext context, PrintVariable statement);

    void AnalyzeInvocation(TContext context, Invocation invocation, TContext invokedMethodContext,
        IInvokedMethodContextProvider contextProvider);
}