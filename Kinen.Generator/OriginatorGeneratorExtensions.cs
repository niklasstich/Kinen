using System;
using System.Collections.Generic;
using System.Linq;
using CodeGenHelpers;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

namespace Kinen.Generator;

internal static class OriginatorGeneratorExtensions
{
    internal static void BuildNestedConstructorBody(this IEnumerable<ISymbol> symbols, ICodeWriter bodyCw) =>
        bodyCw.AppendLines(symbols, symbol => $"this.{symbol.Name} = {symbol.Name};");
    
    internal static void BuildRestoreMethodBody(this IEnumerable<ISymbol> symbols, ICodeWriter bodyCw, string mementoClassName)
    {
        bodyCw.AppendLine($"if (memento is not {mementoClassName} concreteMemento)");
        bodyCw.AppendLine(@$"    throw new ArgumentException(""memento is not {mementoClassName}"");");
        bodyCw.AppendLines(symbols, symbol => $"this.{symbol.Name} = concreteMemento.{symbol.Name};");
    }

    internal static void BuildCreateMementoMethodBody(this IEnumerable<ISymbol> symbols, ICodeWriter bodyCw,
        string mementoClassName) =>
        bodyCw.AppendLine(
            $"return new {mementoClassName}({string.Join(", ", symbols.Select(symbol => symbol.Name))});");

    internal static void AddAutoPropertiesToMemento(this IEnumerable<ISymbol> symbols, ClassBuilder mementoBuilder)
    {
        foreach (var tuple in symbols.GetNameTypeNameTuples())
        {
            mementoBuilder
                .AddProperty(tuple.Name, Accessibility.Public)
                .SetType(tuple.TypeName)
                .UseGetOnlyAutoProp();
        }
    }

    internal static void BuildMementoConstructorParameters(this IEnumerable<ISymbol> symbols,
        ConstructorBuilder mementoConstructorBuilder)
    {
        foreach (var tuple in symbols.GetNameTypeNameTuples())
        {
            mementoConstructorBuilder.AddParameter(tuple.TypeName, tuple.Name);
        }
    }

    private static IEnumerable<(string Name, string TypeName)> GetNameTypeNameTuples(this IEnumerable<ISymbol> symbols)
    {
        return symbols.Select(symbol => (symbol.Name, GetTypeOfSymbol(symbol).GetTypeName()));
    }

    private static ITypeSymbol GetTypeOfSymbol(ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol ps => ps.Type,
            IFieldSymbol fs => fs.Type,
            _ => throw new ArgumentException("invalid symbol", nameof(symbol))
        };
    }
    
}