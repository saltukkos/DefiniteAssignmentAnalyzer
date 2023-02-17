using LanguageModel;

namespace Analysis;

public abstract class WalkerBase<TContext>
{
    private readonly IProgramDeclarations _programDeclarations;
    private readonly UniqueElementsStack<AnalyzingStackFrame> _analyzingStack;

    protected WalkerBase(IProgramDeclarations programDeclarations)
    {
        _programDeclarations = programDeclarations;
        _analyzingStack = new UniqueElementsStack<AnalyzingStackFrame>();
    }

    public void AnalyzeProgram()
    {
        _analyzingStack.Push(new AnalyzingStackFrame(_programDeclarations, CreateContext(_programDeclarations)));
        while (_analyzingStack.TryPeek(out var stackFrame))
        {
            var program = stackFrame.Declaration.Program;
            var position = stackFrame.Position;
            if (position == program.Count)
            {
                _analyzingStack.Pop();
                OnDeclarationProcessingFinished(stackFrame.Declaration, stackFrame.Context);
                continue;
            }

            var nextStatement = program[position];
            if (TryProcessStatement(nextStatement, stackFrame.Context, stackFrame.Declaration))
            {
                stackFrame.Position++;
            }
        }
    }

    protected bool TryPushDeclarationToProcess(IProgramDeclarations declarations, TContext context)
    {
        var nextFrame = new AnalyzingStackFrame(declarations, context);
        return _analyzingStack.TryPush(nextFrame);
    }

    protected abstract bool TryProcessStatement(IStatement statement, TContext context,
        IProgramDeclarations declarations);

    protected abstract TContext CreateContext(IProgramDeclarations programDeclarations);

    protected abstract void OnDeclarationProcessingFinished(IProgramDeclarations declarations, TContext context);

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
}