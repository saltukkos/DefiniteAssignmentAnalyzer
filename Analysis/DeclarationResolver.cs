using System.Collections.Immutable;
using Analysis.InspectionDescriptors;
using LanguageModel;

namespace Analysis;

public sealed class DeclarationResolver
{
    private readonly IInspectionDescriptorCollector _inspectionDescriptorCollector;

    public DeclarationResolver(IInspectionDescriptorCollector inspectionDescriptorCollector)
    {
        _inspectionDescriptorCollector = inspectionDescriptorCollector;
    }

    public IProgramDeclarations Resolve(Program rootProgram)
    {
        var programsToVisit = new Queue<ProgramDeclarations>();
        var rootDeclarations = new ProgramDeclarations(rootProgram, null);
        programsToVisit.Enqueue(rootDeclarations);

        while (programsToVisit.TryDequeue(out var currentDeclaration))
        {
            AnalyzeSubProgram(currentDeclaration, programsToVisit);
        }

        return rootDeclarations;
    }

    private void AnalyzeSubProgram(
        ProgramDeclarations currentDeclaration,
        Queue<ProgramDeclarations> programsToVisit)
    {
        var parentDeclarations = currentDeclaration.ParentDeclarations?.AllAvailableDeclarationsInternal ??
                                 ImmutableDictionary<string, IProgramDeclarations>.Empty;

        var availableDeclarationsBuilder = parentDeclarations.ToBuilder();
        foreach (var statement in currentDeclaration.Program)
        {
            if (statement is VariableDeclaration variableDeclaration)
            {
                currentDeclaration.CurrentContextVariablesInternal.Add(variableDeclaration.VariableName);
            }
            
            if (statement is not FunctionDeclaration functionDeclaration)
            {
                continue;
            }

            var functionName = functionDeclaration.FunctionName;
            if (availableDeclarationsBuilder.ContainsKey(functionName))
            {
                _inspectionDescriptorCollector.ReportInspection(
                    new ConflictingIdentifierNameDescriptor(functionDeclaration, functionDeclaration.FunctionName));

                continue;
            }

            var childDeclarations = new ProgramDeclarations(functionDeclaration.Body, currentDeclaration);
            availableDeclarationsBuilder.Add(functionName, childDeclarations);
            programsToVisit.Enqueue(childDeclarations);
        }

        currentDeclaration.AllAvailableDeclarationsInternal = availableDeclarationsBuilder.ToImmutable();
    }
    
    private sealed class ProgramDeclarations : IProgramDeclarations
    {
        public ProgramDeclarations(Program program, ProgramDeclarations? parentDeclarations)
        {
            Program = program;
            ParentDeclarations = parentDeclarations;
            AllAvailableDeclarationsInternal = ImmutableDictionary<string, IProgramDeclarations>.Empty;
        }

        public Program Program { get; }

        public ProgramDeclarations? ParentDeclarations { get; }

        internal ImmutableDictionary<string, IProgramDeclarations> AllAvailableDeclarationsInternal { get; set; }

        internal HashSet<string> CurrentContextVariablesInternal { get; } = new();

        public IReadOnlyDictionary<string, IProgramDeclarations> AllAvailableDeclarations =>
            AllAvailableDeclarationsInternal;
        
        public IReadOnlySet<string> CurrentContextVariables => CurrentContextVariablesInternal;
    }
}