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
            
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (typeSymbol == null)
                throw new Exception("type wasn't INamedTypeSymbol");

            var (root, parentBuilder) = GetClassBuilder(typeSymbol, compilation);
            parentBuilder.AddNamespaceImport(MementoAttributeHelper.Namespace);
            parentBuilder.AddInterface("IOriginator");

            var parentCreateMementoMethod = parentBuilder.AddMethod("CreateMemento", Accessibility.Public);
            parentCreateMementoMethod.WithReturnType("IMemento");
            var parentCreateMementoConstructorArguments = "";
            
            var parentRestoreMethod = parentBuilder.AddMethod("RestoreMemento", Accessibility.Public);
            parentRestoreMethod.WithReturnType("void");
            parentRestoreMethod.AddParameter("IMemento", "memento");
            var parentRestoreMethodActions = new List<Action<ICodeWriter>> 
            {
                w =>
                    w.AppendLine(
                        $"if(memento is not {className}Memento concreteMemento) throw new ArgumentException(\"memento is not {className}Memento\");")
            };
            var nestedBuilder = parentBuilder.AddNestedClass($"{className}Memento", Accessibility.Private);
            nestedBuilder.AddInterface("IMemento");
            var nestedConstructorBuilder = nestedBuilder
                .AddConstructor(Accessibility.Public);

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

            var propertySymbols = relevantMembers.OfType<IPropertySymbol>();
            var fieldSymbols = relevantMembers.OfType<IFieldSymbol>();

            var nestedConstructorActions = new List<Action<ICodeWriter>>();
            
            foreach (var property in propertySymbols )
            {
                parentCreateMementoConstructorArguments = HandlePropertyOrField(nestedBuilder,
                    property.Type.ToDisplayString(), property.Name, nestedConstructorBuilder, nestedConstructorActions,
                    parentRestoreMethodActions, parentCreateMementoConstructorArguments);
            }
            
            foreach (var field in fieldSymbols)
            {
                parentCreateMementoConstructorArguments = HandlePropertyOrField(nestedBuilder,
                    field.Type.ToDisplayString(), field.Name, nestedConstructorBuilder, nestedConstructorActions,
                    parentRestoreMethodActions, parentCreateMementoConstructorArguments);
            }
            
            var constructorImpl =
                nestedConstructorActions.Aggregate<Action<ICodeWriter>, Action<ICodeWriter>>(null!,
                    (current, a) => current + a);
            nestedConstructorBuilder.WithBody(constructorImpl);
            var parentRestoreImpl =
                parentRestoreMethodActions.Aggregate<Action<ICodeWriter>, Action<ICodeWriter>>(null!,
                    (current, a) => current + a);
            parentRestoreMethod.WithBody(parentRestoreImpl);
            Action<ICodeWriter> parentCreateMementoImpl = w =>
                w.AppendLine($"return new {className}Memento({parentCreateMementoConstructorArguments});");
            parentCreateMementoMethod.WithBody(parentCreateMementoImpl);
            
            context.AddSource($"{root.Name}.g.cs", root.Build());
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

        private static string HandlePropertyOrField(ClassBuilder nestedBuilder,
            string typeDisplayString, string fieldOrPropertyName,
            ConstructorBuilder nestedConstructorBuilder, ICollection<Action<ICodeWriter>> nestedConstructorActions,
            ICollection<Action<ICodeWriter>> parentRestoreMethodActions,
            string parentCreateMementoConstructorArguments)
        {
            nestedBuilder
                .AddProperty(fieldOrPropertyName, Accessibility.Public)
                .SetType(typeDisplayString)
                .UseGetOnlyAutoProp();
            nestedConstructorBuilder
                .AddParameter(typeDisplayString, fieldOrPropertyName);
            nestedConstructorActions.Add(w => w.AppendLine($"this.{fieldOrPropertyName} = {fieldOrPropertyName};"));
            parentRestoreMethodActions.Add(w => w.AppendLine($"this.{fieldOrPropertyName} = concreteMemento.{fieldOrPropertyName};"));
            if (parentCreateMementoConstructorArguments == "")
            {
                parentCreateMementoConstructorArguments += $"{fieldOrPropertyName}";
            }
            else
            {
                parentCreateMementoConstructorArguments += $", {fieldOrPropertyName}";
            }

            return parentCreateMementoConstructorArguments;
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