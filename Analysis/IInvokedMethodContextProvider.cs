namespace Analysis;

public interface IInvokedMethodContextProvider
{
    TContext GetContext<TContext>(IPostorderMethodStateAnalyzer<TContext> analyzer);
}