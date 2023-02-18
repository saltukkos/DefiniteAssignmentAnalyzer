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

    public IDeclarationScope Resolve(Program rootProgram)
    {
        var programsToVisit = new Queue<DeclarationScope>();
        var rootDeclarations = new DeclarationScope(rootProgram, null);
        programsToVisit.Enqueue(rootDeclarations);

        while (programsToVisit.TryDequeue(out var currentScope))
        {
            AnalyzeSubProgram(currentScope, programsToVisit);
        }

        return rootDeclarations;
    }

    private void AnalyzeSubProgram(DeclarationScope currentScope, Queue<DeclarationScope> programsToVisit)
    {
        var parentDeclarations = currentScope.ParentScope?.AllAvailableFunctionDeclarationsInternal ??
                                 ImmutableDictionary<string, IDeclarationScope>.Empty;

        var availableDeclarationsBuilder = parentDeclarations.ToBuilder();
        foreach (var statement in currentScope.Program)
        {
            if (statement is VariableDeclaration variableDeclaration)
            {
                currentScope.CurrentContextVariablesInternal.Add(variableDeclaration.VariableName);
                continue;
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

            var childDeclarations = new DeclarationScope(functionDeclaration.Body, currentScope);
            availableDeclarationsBuilder.Add(functionName, childDeclarations);
            programsToVisit.Enqueue(childDeclarations);
        }

        currentScope.AllAvailableFunctionDeclarationsInternal = availableDeclarationsBuilder.ToImmutable();
    }
    
    private sealed class DeclarationScope : IDeclarationScope
    {
        public DeclarationScope(Program program, DeclarationScope? parentScope)
        {
            Program = program;
            ParentScope = parentScope;
            AllAvailableFunctionDeclarationsInternal = ImmutableDictionary<string, IDeclarationScope>.Empty;
        }

        public Program Program { get; }

        public DeclarationScope? ParentScope { get; }

        internal ImmutableDictionary<string, IDeclarationScope> AllAvailableFunctionDeclarationsInternal { get; set; }

        internal HashSet<string> CurrentContextVariablesInternal { get; } = new();

        public IReadOnlyDictionary<string, IDeclarationScope> AllAvailableFunctionDeclarations =>
            AllAvailableFunctionDeclarationsInternal;
        
        public IReadOnlySet<string> CurrentContextVariables => CurrentContextVariablesInternal;
    }
}