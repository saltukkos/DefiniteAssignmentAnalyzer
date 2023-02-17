using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using LanguageModel;

namespace Analysis;

public sealed class ProgramDeclarationPreorderWalker<TContext> : WalkerBase<TContext>
{
    private readonly IPreorderDeclarationsAnalyzer<TContext> _methodStateAnalyzer;

    public ProgramDeclarationPreorderWalker(
        IProgramDeclarations programDeclarations,
        IPreorderDeclarationsAnalyzer<TContext> methodStateAnalyzer)
        : base(programDeclarations)
    {
        _methodStateAnalyzer = methodStateAnalyzer;
    }

    protected override bool TryProcessStatement(
        IStatement statement, TContext context, IProgramDeclarations declarations)
    {
        var visitor = new StatementVisitor(_methodStateAnalyzer, declarations, context);
        statement.Accept(visitor);
        if (visitor.SawNewDeclaration)
        {
            var pushed = TryPushDeclarationToProcess(visitor.NewDeclaration, visitor.CurrentContext);
            Debug.Assert(pushed, "Declarations analysis could not lead to cycle. Is AST not actually a tree?");
        }

        return true;
    }

    protected override TContext CreateContext(IProgramDeclarations programDeclarations)
    {
        return _methodStateAnalyzer.CreateEmptyContext(programDeclarations);
    }

    protected override void OnDeclarationProcessingFinished(IProgramDeclarations declarations, TContext context)
    {
    }
    
    private sealed class StatementVisitor : IStatementVisitor
    {
        private readonly IPreorderDeclarationsAnalyzer<TContext> _declarationsAnalyzer;
        private readonly IProgramDeclarations _declarations;

        public StatementVisitor(
            IPreorderDeclarationsAnalyzer<TContext> declarationsAnalyzer,
            IProgramDeclarations declarations,
            TContext currentContext)
        {
            _declarationsAnalyzer = declarationsAnalyzer;
            _declarations = declarations;
            CurrentContext = currentContext;
        }

        public TContext CurrentContext { get; private set; }

        [MemberNotNullWhen(true, nameof(NewDeclaration))]
        public bool SawNewDeclaration { get; private set; }
        
        public IProgramDeclarations? NewDeclaration { get; private set; }

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
            if (!_declarations.AllAvailableDeclarations.TryGetValue(statement.FunctionName, out var nestedDeclarations))
            {
                return;
            }

            SawNewDeclaration = true;
            NewDeclaration = nestedDeclarations;
            CurrentContext = _declarationsAnalyzer.CreateChildContext(CurrentContext, nestedDeclarations);
        }
    }
}