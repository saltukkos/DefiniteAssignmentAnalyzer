using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class AnalysisRunner
{
    public IReadOnlyDictionary<IStatement, HashSet<IInspectionDescriptor>> Analise(Program program)
    {
        var inspectionsCollector = new InspectionDescriptorCollector();
        var programDeclarations = new DeclarationResolver(inspectionsCollector).Resolve(program);

        var analyzerResultsStorage = new AnalyzerResultsStorage();
        var invalidDeclarationsAnalyzer = new InvalidDeclarationsAnalyzer(inspectionsCollector);
        var parentAssignmentAnalyzer = new ParentAssignmentAnalyzer();
        var assignmentAnalyzer = new DefiniteAssignmentAnalyzer(parentAssignmentAnalyzer, inspectionsCollector);

        RunAnalyzer(invalidDeclarationsAnalyzer, programDeclarations);
        RunAnalyzer(parentAssignmentAnalyzer, programDeclarations, analyzerResultsStorage);
        RunAnalyzer(assignmentAnalyzer, programDeclarations, analyzerResultsStorage);

        return inspectionsCollector.GetInspections();
    }

    private static void RunAnalyzer<TContext>(IPostorderMethodStateAnalyzer<TContext> analyzer,
        IProgramDeclarations programDeclarations, AnalyzerResultsStorage analyzerResultsStorage)
    {
        var runner =
            new ProgramInvocationPostorderWalkerWithRecursionClipping<TContext>(programDeclarations, analyzer,
                analyzerResultsStorage);

        runner.AnalyzeProgram();
    }

    private static void RunAnalyzer<TContext>(IPreorderDeclarationsAnalyzer<TContext> analyzer,
        IProgramDeclarations programDeclarations)
    {
        var runner = new ProgramDeclarationPreorderWalker<TContext>(programDeclarations, analyzer);
        runner.AnalyzeProgram();
    }
}