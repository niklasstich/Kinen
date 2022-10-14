using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kinen.Generator;

[Generator]
public class OriginatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource($"{MementoAttributeHelper.AttributeName}.g.cs",
                SourceText.From(MementoAttributeHelper.AttributeCode, Encoding.UTF8));
            ctx.AddSource($"{MementoSkipAttributeHelper.AttributeName}.g.cs",
                SourceText.From(MementoSkipAttributeHelper.AttributeCode, Encoding.UTF8));
            ctx.AddSource($"{IOriginatorHelper.InterfaceName}.g.cs",
                SourceText.From(IOriginatorHelper.InterfaceCode, Encoding.UTF8));
            ctx.AddSource($"{IMementoHelper.InterfaceName}.g.cs",
                SourceText.From(IMementoHelper.InterfaceCode, Encoding.UTF8));
        });
            
            
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsTargetForGeneration,
                transform: GetSemanticTargetForGeneration)
            .Where(static v => v is not null);

        var compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());
            
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty) return;
        var distinctClasses = classes.Distinct();
            
        foreach (var classDeclaration in distinctClasses)
        {
            if (classDeclaration is null)
            {
                continue;
            }
            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                context.ReportDiagnostic(DiagnosticHelper.CreateNotPartialDiagnostic(classDeclaration));
                continue;
            }

            var parentToken =
                classDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>(token => token != classDeclaration);
            var continueOuter = false;
            while (parentToken != null)
            {
                if (!parentToken.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    context.ReportDiagnostic(DiagnosticHelper.CreateParentNotPartialDiagnostic(parentToken));
                    continueOuter = true;
                    break;
                }

                var token = parentToken;
                parentToken = parentToken.FirstAncestorOrSelf<ClassDeclarationSyntax>(node => node != token);
            }
            if (continueOuter) continue;

            BuildIOriginatorImplementation(compilation, context, classDeclaration);
        }
    }

    private static bool IsTargetForGeneration(SyntaxNode node, CancellationToken _)
    {
        var retval = node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
        return retval;
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (cancellationToken.IsCancellationRequested) return null;
                var attributeSymbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax, cancellationToken);
                var attributeContainingTypeSymbol = attributeSymbol.Symbol?.ContainingType;
                var fullName = attributeContainingTypeSymbol?.ToDisplayString();
                if (fullName != MementoAttributeHelper.AttributeFullName) continue; 
                return classDeclarationSyntax;
            }
        }

        return null;
    }

    private static void BuildIOriginatorImplementation(Compilation compilation, SourceProductionContext context,
        ClassDeclarationSyntax classDeclaration)
    {
            
        var className = classDeclaration.Identifier.Text;
        var mementoClassName = $"{className}Memento";

        var typeSymbol = compilation.GetSemanticModel(classDeclaration.SyntaxTree).GetDeclaredSymbol(classDeclaration);
        if (typeSymbol == null)
            throw new Exception("type wasn't INamedTypeSymbol");

        var (root, originatorBuilder) = GetClassBuilder(typeSymbol, compilation);
        originatorBuilder
            .AddNamespaceImport(MementoAttributeHelper.Namespace)
            .AddInterface("IOriginator");

        var originatorCreateMementoMethodBuilder = GetCreateMementoMethodBuilder(originatorBuilder);
        var originatorRestoreMementoMethodBuilder = GetRestoreMementoMethodBuilder(originatorBuilder);
        var mementoClassBuilder = GetMementoClassBuilder(originatorBuilder, mementoClassName);
        var mementoConstructorBuilder = GetMementoConstructorBuilder(mementoClassBuilder);

        //filter members
        var relevantMembers = GetRelevantMembersOfType(typeSymbol);
        if (relevantMembers.IsEmpty)
            return;

        //Memento constructor
        relevantMembers.BuildMementoConstructorParameters(mementoConstructorBuilder);
        mementoConstructorBuilder.WithBody(relevantMembers.BuildNestedConstructorBody);

        //Memento properties
        relevantMembers.AddAutoPropertiesToMemento(mementoClassBuilder);

        //Originator restore memento method
        originatorRestoreMementoMethodBuilder.WithBody(cw => relevantMembers.BuildRestoreMethodBody(cw, mementoClassName));

        //Originator create memento method
        originatorCreateMementoMethodBuilder.WithBody(cw => relevantMembers.BuildCreateMementoMethodBody(cw, mementoClassName));

        context.AddSource($"{root.Name}.g.cs", root.Build());
    }

    private static ConstructorBuilder GetMementoConstructorBuilder(ClassBuilder mementoClassBuilder)
    {
        return mementoClassBuilder
            .AddConstructor(Accessibility.Public);
    }

    private static ClassBuilder GetMementoClassBuilder(ClassBuilder originatorBuilder, string mementoClassName)
    {
        return originatorBuilder
            .AddNestedClass(mementoClassName, Accessibility.Private)
            .AddInterface("IMemento");
    }

    private static MethodBuilder GetRestoreMementoMethodBuilder(ClassBuilder originatorBuilder)
    {
        return originatorBuilder
            .AddMethod("RestoreMemento", Accessibility.Public)
            .WithReturnType("void")
            .AddParameter("IMemento", "memento");
    }

    private static MethodBuilder GetCreateMementoMethodBuilder(ClassBuilder originatorBuilder)
    {
        return originatorBuilder
            .AddMethod("CreateMemento", Accessibility.Public)
            .WithReturnType("IMemento");
    }

    private static ImmutableList<ISymbol> GetRelevantMembersOfType(INamespaceOrTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .Where(symbol => !symbol.IsImplicitlyDeclared)
            .Where(symbol => symbol is IPropertySymbol or IFieldSymbol)
            .Where(HasNoMementoSkipAttribute)
            .ToImmutableList();
    }

    private static bool HasNoMementoSkipAttribute(ISymbol symbol)
    {
        var attributes = symbol.GetAttributes();
        if (attributes.IsEmpty) return true;
        return !attributes.Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == MementoSkipAttributeHelper.AttributeFullName);
    }

    private static (ClassBuilder root, ClassBuilder) GetClassBuilder(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        if (typeSymbol == null)
            throw new Exception("type wasn't INamedTypeSymbol");
        if (typeSymbol.ContainingType != null)
        {
            var (root, parent) = GetClassBuilder(typeSymbol.ContainingType, compilation);
            var child = parent.AddNestedClass(typeSymbol.Name, true, typeSymbol.DeclaredAccessibility);
            return (root, child);
        }
        else
        {
            var root = CodeBuilder.Create(typeSymbol);
            return (root, root);
        }
    }
}