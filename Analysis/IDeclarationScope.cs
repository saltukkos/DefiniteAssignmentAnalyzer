using LanguageModel;

namespace Analysis;

public interface IDeclarationScope
{
    Program Program { get; }

    IReadOnlyDictionary<string, IDeclarationScope> AllAvailableFunctionDeclarations { get; }

    IReadOnlySet<string> CurrentContextVariables { get; }
}