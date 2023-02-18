using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class AnalysisRunner
{
    public IReadOnlyDictionary<IStatement, HashSet<IInspectionDescriptor>> Analise(Program program)
    {
        var inspectionsCollector = new InspectionDescriptorCollector();
        var programDeclarations = new DeclarationResolver(inspectionsCollector).Resolve(program);

        var invalidDeclarationsAnalyzer = CreateAnalyzerRunner(new InvalidDeclarationsAnalyzer(inspectionsCollector));
        var parentAssignmentAnalyzer = CreateAnalyzerRunner(new ParentAssignmentAnalyzer());
        var assignmentAnalyzer =
            CreateAnalyzerRunner(new DefiniteAssignmentAnalyzer(parentAssignmentAnalyzer, inspectionsCollector));

        invalidDeclarationsAnalyzer.AnalyzeProgram(programDeclarations);
        assignmentAnalyzer.AnalyzeProgram(programDeclarations);

        return inspectionsCollector.GetInspections();
    }

    private static IProgramAnalyzer<TContext> CreateAnalyzerRunner<TContext>(
        IPostorderMethodStateAnalyzer<TContext> analyzer)
    {
        return new ProgramInvocationPostorderWalkerWithRecursionClipping<TContext>(analyzer);
    }

    private static IProgramAnalyzer<TContext> CreateAnalyzerRunner<TContext>(
        IPreorderDeclarationsAnalyzer<TContext> analyzer)
    {
        return new ProgramDeclarationPreorderWalker<TContext>(analyzer);
    }
}