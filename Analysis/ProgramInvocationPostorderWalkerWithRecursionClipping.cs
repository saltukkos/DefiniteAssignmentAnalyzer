using LanguageModel;

namespace Analysis;

public sealed class ProgramInvocationPostorderWalkerWithRecursionClipping<TContext> : WalkerBase<TContext>
{
    private readonly IPostorderMethodStateAnalyzer<TContext> _methodStateAnalyzer;
    private readonly AnalyzerResultsStorage _analyzerResultsStorage;
    private readonly Dictionary<IDeclarationScope, TContext> _builtContexts = new();

    public ProgramInvocationPostorderWalkerWithRecursionClipping(
        IDeclarationScope declarationScope,
        IPostorderMethodStateAnalyzer<TContext> methodStateAnalyzer,
        AnalyzerResultsStorage analyzerResultsStorage) 
        : base(declarationScope)
    {
        _methodStateAnalyzer = methodStateAnalyzer;
        _analyzerResultsStorage = analyzerResultsStorage;
        analyzerResultsStorage.RegisterResults(methodStateAnalyzer, _builtContexts);
    }

    protected override bool TryProcessStatement(
        IStatement statement, TContext context, IDeclarationScope declarations)
    {
        if (statement is not Invocation invocation)
        {
            var statementVisitor = new StatementVisitor(_methodStateAnalyzer, context);
            statement.Accept(statementVisitor);
            return true;
        }
        
        if (!_methodStateAnalyzer.NeedToProcessInvocation(invocation) ||
            !declarations.AllAvailableFunctionDeclarations.TryGetValue(invocation.FunctionName, out var declaration))
        {
            return true;
        }

        if (_builtContexts.TryGetValue(declaration, out var builtContext))
        {
            var invokedMethodContextProvider = _analyzerResultsStorage.GetProviderFor(declaration);
            _methodStateAnalyzer.AnalyzeInvocation(context, invocation, builtContext, invokedMethodContextProvider);
            return true;
        }

        var emptyContext = _methodStateAnalyzer.CreateEmptyContext(declaration);
        if (!TryPushDeclarationToProcess(declaration, emptyContext))
        {
            var invokedMethodContextProvider = _analyzerResultsStorage.GetProviderFor(declaration);
            _methodStateAnalyzer.AnalyzeInvocation(context, invocation, emptyContext, invokedMethodContextProvider);
            return true;
        }

        return false;
    }

    protected override TContext CreateContext(IDeclarationScope declarationScope)
    {
        return _methodStateAnalyzer.CreateEmptyContext(declarationScope);
    }

    protected override void OnDeclarationProcessingFinished(IDeclarationScope declarations, TContext context)
    {
        _builtContexts.Add(declarations, context);
    }

    private sealed class StatementVisitor : IStatementVisitor
    {
        private readonly IPostorderMethodStateAnalyzer<TContext> _methodStateAnalyzer;
        private readonly TContext _currentContext;

        public StatementVisitor(IPostorderMethodStateAnalyzer<TContext> methodStateAnalyzer, TContext currentContext)
        {
            _methodStateAnalyzer = methodStateAnalyzer;
            _currentContext = currentContext;
        }

        public void VisitInvocation(Invocation statement)
        {
            throw new NotSupportedException("Should be handled by calling code");
        }

        public void VisitAssignVariable(AssignVariable statement)
        {
            _methodStateAnalyzer.AnalyzeAssignVariable(_currentContext, statement);
        }

        public void VisitPrintVariable(PrintVariable statement)
        {
            _methodStateAnalyzer.AnalyzePrintVariable(_currentContext, statement);
        }

        public void VisitVariableDeclaration(VariableDeclaration statement)
        {
        }

        public void VisitFunctionDeclaration(FunctionDeclaration statement)
        {
        }
    }
}