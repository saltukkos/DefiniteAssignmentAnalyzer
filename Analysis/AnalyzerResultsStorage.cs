using System.Collections;

namespace Analysis;

public sealed class AnalyzerResultsStorage
{
    private readonly Dictionary<object, IDictionary> _storages = new();

    public void RegisterResults<TContext>(
        IPostorderMethodStateAnalyzer<TContext> analyzer,
        Dictionary<IProgramDeclarations, TContext> results)
    {
        _storages[analyzer] = results;
    }

    public IInvokedMethodContextProvider GetProviderFor(IProgramDeclarations declarations)
    {
        return new InvokedMethodContextProvider(declarations, _storages);
    }

    private sealed class InvokedMethodContextProvider : IInvokedMethodContextProvider
    {
        private readonly IProgramDeclarations _declarations;
        private readonly Dictionary<object, IDictionary> _storages;

        public InvokedMethodContextProvider(IProgramDeclarations declarations, Dictionary<object, IDictionary> storages)
        {
            _declarations = declarations;
            _storages = storages;
        }

        public TContext GetContext<TContext>(IPostorderMethodStateAnalyzer<TContext> analyzer)
        {
            var storage = (IReadOnlyDictionary<IProgramDeclarations, TContext>) _storages[analyzer];
            return storage[_declarations];
        }
    }
}