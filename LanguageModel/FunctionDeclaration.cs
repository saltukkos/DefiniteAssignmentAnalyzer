using System.Text;

namespace LanguageModel;

public sealed class FunctionDeclaration : IStatement
{
    public FunctionDeclaration(string functionName)
    {
        FunctionName = functionName;
    }

    public string FunctionName { get; }
    
    public Program Body { get; } = new();

    public void Accept(IStatementVisitor visitor) => visitor.VisitFunctionDeclaration(this);

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("func ").Append(FunctionName).AppendLine(" {");
        builder.Append(Body);
        builder.Append('}');

        return builder.ToString();
    }
}