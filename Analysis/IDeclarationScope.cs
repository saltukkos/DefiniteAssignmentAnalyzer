using LanguageModel;

namespace Analysis;

public interface IDeclarationScope
{
    Program Program { get; }

    IReadOnlyDictionary<string, IDeclarationScope> AllAvailableFunctionDeclarations { get; }

    IReadOnlyDictionary<string, VariableDeclaration> AllAvailableVariableDeclarations { get; }

    IReadOnlySet<VariableDeclaration> CurrentContextVariableDeclarations { get; }
}