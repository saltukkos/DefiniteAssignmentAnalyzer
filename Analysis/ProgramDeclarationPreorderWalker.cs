using System.Diagnostics.CodeAnalysis;
using LanguageModel;

namespace Analysis;

public sealed class ProgramDeclarationPreorderWalker<TContext> : WalkerBase<TContext>
{
    private readonly IPreorderDeclarationsAnalyzer<TContext> _analyzer;

    public ProgramDeclarationPreorderWalker(IPreorderDeclarationsAnalyzer<TContext> analyzer)
    {
        _analyzer = analyzer;
    }

    protected override bool TryProcessStatement(
        IStatement statement, TContext context, IDeclarationScope declarations)
    {
        var visitor = new StatementVisitor(_analyzer, declarations, context);
        statement.Accept(visitor);
        if (visitor.SawNewDeclaration)
        {
            TryPushDeclarationToProcess(visitor.NewDeclaration, visitor.CurrentContext);
        }

        return true;
    }

    protected override TContext CreateContext(IDeclarationScope declarationScope)
    {
        return _analyzer.CreateEmptyContext(declarationScope);
    }

    private sealed class StatementVisitor : IStatementVisitor
    {
        private readonly IPreorderDeclarationsAnalyzer<TContext> _declarationsAnalyzer;
        private readonly IDeclarationScope _declarations;

        public StatementVisitor(
            IPreorderDeclarationsAnalyzer<TContext> declarationsAnalyzer,
            IDeclarationScope declarations,
            TContext currentContext)
        {
            _declarationsAnalyzer = declarationsAnalyzer;
            _declarations = declarations;
            CurrentContext = currentContext;
        }

        public TContext CurrentContext { get; private set; }

        [MemberNotNullWhen(true, nameof(NewDeclaration))]
        public bool SawNewDeclaration { get; private set; }
        
        public IDeclarationScope? NewDeclaration { get; private set; }

        public void VisitInvocation(Invocation statement)
        {
            _declarationsAnalyzer.AnalyzeInvocation(CurrentContext, statement);
        }

        public void VisitAssignVariable(AssignVariable statement)
        {
            _declarationsAnalyzer.AnalyzeAssignVariable(CurrentContext, statement);
        }

        public void VisitPrintVariable(PrintVariable statement)
        {
            _declarationsAnalyzer.AnalyzePrintVariable(CurrentContext, statement);
        }

        public void VisitVariableDeclaration(VariableDeclaration statement)
        {
            _declarationsAnalyzer.AnalyzeVariableDeclaration(CurrentContext, statement);
        }

        public void VisitFunctionDeclaration(FunctionDeclaration statement)
        {
            var declarations = _declarations.AllAvailableFunctionDeclarations;
            if (!declarations.TryGetValue(statement.FunctionName, out var nestedDeclarations))
            {
                return;
            }

            SawNewDeclaration = true;
            NewDeclaration = nestedDeclarations;
            CurrentContext = _declarationsAnalyzer.CreateChildContext(CurrentContext, nestedDeclarations);
        }
    }
}