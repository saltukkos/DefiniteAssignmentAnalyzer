namespace Analysis;

public interface IInvokedMethodContextProvider
{
    TContext GetContext<TContext>(IProgramAnalyzer<TContext> analyzer);
}

public sealed class InvokedMethodContextProvider : IInvokedMethodContextProvider
{
    private readonly IDeclarationScope _invokedMethodDeclarationScope;

    public InvokedMethodContextProvider(IDeclarationScope invokedMethodDeclarationScope)
    {
        _invokedMethodDeclarationScope = invokedMethodDeclarationScope;
    }

    public TContext GetContext<TContext>(IProgramAnalyzer<TContext> analyzer)
    {
        return analyzer.AnalyzeProgram(_invokedMethodDeclarationScope);
    }
}