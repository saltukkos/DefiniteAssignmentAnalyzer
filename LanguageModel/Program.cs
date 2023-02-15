using System.Text;

namespace LanguageModel;

public sealed class Program : List<IStatement>
{
    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var statement in this)
        {
            builder.AppendLine(statement.ToString());
        }

        return builder.ToString();
    }
}