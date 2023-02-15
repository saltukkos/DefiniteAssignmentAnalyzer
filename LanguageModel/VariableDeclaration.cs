namespace LanguageModel;

public sealed class VariableDeclaration : IStatement
{
    public VariableDeclaration(string variableName)
    {
        VariableName = variableName;
    }

    public string VariableName { get; }

    public void Accept(IStatementVisitor visitor) => visitor.VisitVariableDeclaration(this);

    public override string ToString() => $"var {VariableName};";
}