namespace LanguageModel;

public sealed class Invocation : IStatement
{
    public Invocation(string functionName, bool isConditional)
    {
        FunctionName = functionName;
        IsConditional = isConditional;
    }

    public string FunctionName { get; }

    public bool IsConditional { get; }

    public void Accept(IStatementVisitor visitor) => visitor.VisitInvocation(this);

    public override string ToString()
    {
        if (IsConditional)
            return $"if (smth) {FunctionName}();";

        return $"{FunctionName}();";
    }
}