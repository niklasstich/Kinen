using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kinen.Generator
{
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
                    return classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) ? classDeclarationSyntax : null;
                }
            }

            return null;
        }

        private static void BuildIOriginatorImplementation(Compilation compilation, SourceProductionContext context,
            ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.Text;
            var childClassName = className + "Memento";
            var nameSpace = GetContainingNamespace(classDeclaration);
            
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not { } typeSymbol)
                throw new Exception("type wasn't INamedTypeSymbol");
            
            //filter members
            var relevantMembers = typeSymbol.GetMembers().Where(symbol =>
            {
                if (symbol is not (IPropertySymbol or IFieldSymbol)) return false;
                if (symbol.IsImplicitlyDeclared) return false;
                
                var attributes = symbol.GetAttributes();
                if (attributes.IsEmpty) return true;
                return !attributes.Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == MementoSkipAttributeHelper.AttributeFullName);
            }).ToImmutableList();

            if (relevantMembers.IsEmpty)
                return;

            var propertiesImpl =
                string.Join(Environment.NewLine,
                    relevantMembers
                        .OfType<IPropertySymbol>()
                        .Select(TemplatingHelper.GeneratePropertyString)
                    );

            var fieldsImpl =
                string.Join(Environment.NewLine,
                    relevantMembers
                        .OfType<IFieldSymbol>()
                        .Select(TemplatingHelper.GenerateFieldString)
                    );
            
            
            var constructorImpl =
                string.Join(Environment.NewLine, relevantMembers.Select(TemplatingHelper.GenerateConstructorMemberSet));

            var restoreImpl =
                string.Join(Environment.NewLine, relevantMembers.Select(TemplatingHelper.GenerateRestoreMemberGet));

            var mementoImpl = TemplatingHelper.GetMementoClassTemplate(className, childClassName, constructorImpl,
                propertiesImpl, fieldsImpl);
            var classImpl =
                TemplatingHelper.GetOriginatorClassTemplate(nameSpace, className, childClassName, restoreImpl, mementoImpl);
            
            context.AddSource($"{className}.g.cs", classImpl);
        }


        private static string GetContainingNamespace(BaseTypeDeclarationSyntax syntax)
        {
            //iterate through parents until we run find namespace syntax element
            var potentialNamespaceParent = syntax.Parent;
            while (potentialNamespaceParent != null
                   && potentialNamespaceParent is not NamespaceDeclarationSyntax
                   && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            if (potentialNamespaceParent is not BaseNamespaceDeclarationSyntax namespaceParent) return string.Empty;
            
            var nameSpace = namespaceParent.Name.ToString();

            //handle nested namespaces
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    break;
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }

            return nameSpace;
        }
    }
}