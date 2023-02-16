using LanguageModel;

namespace Analysis;

public interface IProgramDeclarations
{
    Program Program { get; }

    IReadOnlyDictionary<string, IProgramDeclarations> AllAvailableDeclarations { get; }

    IReadOnlySet<string> CurrentContextVariables { get; }
}