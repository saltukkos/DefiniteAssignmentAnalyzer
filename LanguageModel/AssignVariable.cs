namespace LanguageModel;

public sealed class AssignVariable : IStatement
{
    public AssignVariable(string variableName)
    {
        VariableName = variableName;
    }

    public string VariableName { get; }

    public void Accept(IStatementVisitor visitor) => visitor.VisitAssignVariable(this);
    
    public override string ToString() => $"{VariableName} = smth;";
}