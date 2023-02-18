namespace Analysis;

public interface IProgramAnalyzer<out TContext>
{
    TContext AnalyzeProgram(IDeclarationScope declarationScope);
}