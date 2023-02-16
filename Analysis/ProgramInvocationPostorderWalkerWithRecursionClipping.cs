using LanguageModel;

namespace Analysis;

public sealed class ProgramInvocationPostorderWalkerWithRecursionClipping<TContext> : WalkerBase<TContext>
{
    private readonly IPostorderMethodStateAnalyzer<TContext> _methodStateAnalyzer;
    private readonly AnalyzerResultsStorage _analyzerResultsStorage;
    private readonly Dictionary<IProgramDeclarations, TContext> _builtContexts = new();

    private int _topRecursionPosition;
    private bool _stackReturn;
    private TContext? _stackReturnValue;
    
    public ProgramInvocationPostorderWalkerWithRecursionClipping(
        IProgramDeclarations programDeclarations,
        IPostorderMethodStateAnalyzer<TContext> methodStateAnalyzer,
        AnalyzerResultsStorage analyzerResultsStorage) 
        : base(programDeclarations)
    {
        _methodStateAnalyzer = methodStateAnalyzer;
        _analyzerResultsStorage = analyzerResultsStorage;
        analyzerResultsStorage.RegisterResults(methodStateAnalyzer, _builtContexts);
    }

    protected override bool TryProcessStatement(
        IStatement statement, TContext context, IProgramDeclarations declarations, int stackDepth)
    {
        if (statement is not Invocation invocation)
        {
            var statementVisitor = new StatementVisitor(_methodStateAnalyzer, context);
            statement.Accept(statementVisitor);
            return true;
        }
        
        if (!_methodStateAnalyzer.NeedToProcessInvocation(invocation) ||
            !declarations.AllAvailableDeclarations.TryGetValue(invocation.FunctionName, out var declaration))
        {
            return true;
        }

        if (_stackReturn)
        {
            _stackReturn = false;
            var invokedMethodContextProvider = _analyzerResultsStorage.GetProviderFor(declaration);
            _methodStateAnalyzer.AnalyzeInvocation(context, invocation, _stackReturnValue!, invokedMethodContextProvider);
            return true;
        }
        
        if (_builtContexts.TryGetValue(declaration, out var builtContext))
        {
            var invokedMethodContextProvider = _analyzerResultsStorage.GetProviderFor(declaration);
            _methodStateAnalyzer.AnalyzeInvocation(context, invocation, builtContext, invokedMethodContextProvider);
            return true;
        }

        var emptyContext = _methodStateAnalyzer.CreateEmptyContext(declaration);
        if (!TryPushDeclarationToProcess(declaration, emptyContext, out var recursionPosition))
        {
            var invokedMethodContextProvider = _analyzerResultsStorage.GetProviderFor(declaration);
            _methodStateAnalyzer.AnalyzeInvocation(context, invocation, emptyContext, invokedMethodContextProvider);
            _topRecursionPosition = Math.Min(recursionPosition, _topRecursionPosition);
            return true;
        }

        if (_topRecursionPosition == stackDepth - 1)
        {
            _topRecursionPosition++;
        }

        return false;
    }

    protected override TContext CreateContext(IProgramDeclarations programDeclarations)
    {
        return _methodStateAnalyzer.CreateEmptyContext(programDeclarations);
    }

    protected override void OnDeclarationProcessingFinished(
        IProgramDeclarations declarations, TContext context, int stackDepth)
    {
        _stackReturn = true;
        _stackReturnValue = context;
        //TODO We can save result locally to reuse it in case of the same recursive call from the current method
        if (stackDepth <= _topRecursionPosition)
        {
            _builtContexts.Add(declarations, context);
        }
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