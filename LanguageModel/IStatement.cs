namespace LanguageModel;

public interface IStatement
{
    void Accept(IStatementVisitor visitor);
}