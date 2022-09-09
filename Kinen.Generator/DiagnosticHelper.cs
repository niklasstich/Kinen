using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kinen.Generator;

public static class DiagnosticHelper
{
    public static Diagnostic CreateNotPartialDiagnostic(ClassDeclarationSyntax classDeclaration)
    {
        var descriptor = new DiagnosticDescriptor(
            id: "KINEN0001",
            title: "Memento non-partial class",
            messageFormat: "Classes marked with Memento must be partial to generate the Memento code.",
            category: "Memento",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        return Diagnostic.Create(descriptor, classDeclaration.Identifier.GetLocation());
    }
}