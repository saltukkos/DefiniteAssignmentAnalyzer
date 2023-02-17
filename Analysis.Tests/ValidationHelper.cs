using System.Text;
using System.Text.RegularExpressions;
using Analysis.InspectionDescriptors;
using LanguageModel;
using NUnit.Framework;

namespace Analysis.Tests;

public static class ValidationHelper
{
    public static void ValidateResult(Program program, string expectedOutput)
    {
        var analysisRunner = new AnalysisRunner();
        var result = analysisRunner.Analise(program);

        var actualResult = new StringBuilder();
        FillResultRecursive(actualResult, program, result);

        TestContext.Out.WriteLine($"Actual result is: {Environment.NewLine}{actualResult}");
            
        Assert.That(actualResult.ToString(),
            Is.EqualTo(expectedOutput).Using<string>((s1, s2) =>
                Regex.Replace(s1, @"\s+", "").Equals(Regex.Replace(s2, @"\s+", ""), StringComparison.Ordinal)));
    }

    public static string Error(Func<IStatement, IInspectionDescriptor> descriptorFunc)
    {
        return "// " + descriptorFunc.Invoke(TestStatement.Instance);
    }

    public static string Errors(Func<IStatement, IReadOnlyCollection<IInspectionDescriptor>> descriptorsFunc)
    {
        var inspections = descriptorsFunc.Invoke(TestStatement.Instance);
        return JoinInspections(inspections);
    }

    private static void FillResultRecursive(StringBuilder result, Program program,
        IReadOnlyDictionary<IStatement, HashSet<IInspectionDescriptor>> inspections)
    {
        foreach (var statement in program)
        {
            if (statement is FunctionDeclaration functionDeclaration)
            {
                result.Append("func ").Append(functionDeclaration.FunctionName).AppendLine(" {");
                FillResultRecursive(result, functionDeclaration.Body, inspections);
                result.Append("}");
            }
            else
            {
                result.Append(statement);
            }

            if (inspections.TryGetValue(statement, out var statementInspections))
            {
                result.Append(JoinInspections(statementInspections));
            }

            result.AppendLine();
        }
    }

    private static string JoinInspections(IReadOnlyCollection<IInspectionDescriptor> inspections)
    {
        return "// " + string.Join(",", inspections.Select(d => d.ToString()).OrderBy(x => x));
    }

    private sealed class TestStatement : IStatement
    {
        public static TestStatement Instance { get; } = new();

        public void Accept(IStatementVisitor visitor)
        {
            throw new NotSupportedException();
        }
    }
}