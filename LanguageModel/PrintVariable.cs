namespace LanguageModel;

public sealed class PrintVariable : IStatement
{
    public PrintVariable(string variableName)
    {
        VariableName = variableName;
    }

    public string VariableName { get; }

    public void Accept(IStatementVisitor visitor) => visitor.VisitPrintVariable(this);

    public override string ToString() => $"print({VariableName});";
}