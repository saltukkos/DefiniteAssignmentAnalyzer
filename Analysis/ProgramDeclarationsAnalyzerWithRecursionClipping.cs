using LanguageModel;

namespace Analysis;

public sealed class ProgramDeclarationsAnalyzerWithRecursionClipping<TContext>
{
    private readonly IPostorderMethodStateAnalyzer<TContext> _methodStateAnalyzer;
    private readonly StatementVisitor _statementVisitor;

    public ProgramDeclarationsAnalyzerWithRecursionClipping(IPostorderMethodStateAnalyzer<TContext> methodStateAnalyzer)
    {
        _methodStateAnalyzer = methodStateAnalyzer;
        _statementVisitor = new StatementVisitor(methodStateAnalyzer);
    }

    public void AnalyzeProgram(IProgramDeclarations programDeclarations)
    {
        var builtContexts = new Dictionary<IProgramDeclarations, TContext>();
        var analyzingStack = new UniqueElementsStack<AnalyzingStackFrame>();
        analyzingStack.Push(new AnalyzingStackFrame(programDeclarations,
            _methodStateAnalyzer.CreateEmptyContext(programDeclarations)));
        var topRecursionPosition = 0;
        while (analyzingStack.TryPeek(out var stackFrame))
        {
            var program = stackFrame.Declaration.Program;
            var position = stackFrame.Position;
            if (position == program.Count)
            {
                analyzingStack.Pop();
                //TODO We can save result locally to reuse it in case of the same call from current method
                if (analyzingStack.Count <= topRecursionPosition)
                {
                    builtContexts.Add(stackFrame.Declaration, stackFrame.Context);
                }

                continue;
            }

            var nextStatement = program[position];
            if (nextStatement is Invocation invocation)
            {
                if (!TryProcessMethodInvocation(invocation, stackFrame, builtContexts, analyzingStack,
                        ref topRecursionPosition))
                {
                    continue;
                }
            }
            else
            {
                _statementVisitor.CurrentContext = stackFrame.Context;
                nextStatement.Accept(_statementVisitor);
            }

            stackFrame.Position++;
        }
    }

    private bool TryProcessMethodInvocation(
        Invocation invocation,
        AnalyzingStackFrame stackFrame,
        Dictionary<IProgramDeclarations, TContext> builtContexts,
        UniqueElementsStack<AnalyzingStackFrame> analyzingStack,
        ref int topRecursionPosition)
    {
        if (!_methodStateAnalyzer.NeedToProcessInvocation(invocation) ||
            !stackFrame.Declaration.AllAvailableDeclarations.TryGetValue(invocation.FunctionName, out var declaration))
        {
            return true;
        }

        if (builtContexts.TryGetValue(declaration, out var builtContext))
        {
            _methodStateAnalyzer.AnalyzeInvocation(stackFrame.Context, invocation, builtContext, default!);
            return true;
        }

        var nextContext = _methodStateAnalyzer.CreateEmptyContext(declaration);
        var nextFrame = new AnalyzingStackFrame(declaration, nextContext);
        if (!analyzingStack.TryPush(nextFrame, out var recursionPosition))
        {
            _methodStateAnalyzer.AnalyzeInvocation(stackFrame.Context, invocation, nextContext, default!);
            topRecursionPosition = Math.Min(recursionPosition, topRecursionPosition);
            return true;
        }

        if (topRecursionPosition == analyzingStack.Count - 2)
        {
            topRecursionPosition++;
        }

        return false;
    }

    private sealed class AnalyzingStackFrame
    {
        public AnalyzingStackFrame(IProgramDeclarations declaration, TContext context)
        {
            Declaration = declaration;
            Context = context;
            Position = 0;
        }

        public IProgramDeclarations Declaration { get; }
        public int Position { get; set; }
        public TContext Context { get; }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) ||
                   (obj is AnalyzingStackFrame other && Declaration.Equals(other.Declaration));
        }

        public override int GetHashCode()
        {
            return Declaration.GetHashCode();
        }
    }

    private sealed class StatementVisitor : IStatementVisitor
    {
        private readonly IPostorderMethodStateAnalyzer<TContext> _methodStateAnalyzer;

        public StatementVisitor(IPostorderMethodStateAnalyzer<TContext> methodStateAnalyzer)
        {
            _methodStateAnalyzer = methodStateAnalyzer;
        }

        public TContext? CurrentContext { get; set; }

        public void VisitInvocation(Invocation statement)
        {
            throw new NotSupportedException("Should be handled by calling code");
        }

        public void VisitAssignVariable(AssignVariable statement)
        {
            _methodStateAnalyzer.AnalyzeAssignVariable(CurrentContext!, statement);
        }

        public void VisitPrintVariable(PrintVariable statement)
        {
            _methodStateAnalyzer.AnalyzePrintVariable(CurrentContext!, statement);
        }

        public void VisitVariableDeclaration(VariableDeclaration statement)
        {
        }

        public void VisitFunctionDeclaration(FunctionDeclaration statement)
        {
        }
    }
}