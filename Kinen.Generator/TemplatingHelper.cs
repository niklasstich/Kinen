using Microsoft.CodeAnalysis;

namespace Kinen.Generator;

public static class TemplatingHelper
{
    public static string Indent => "\t";
    public static string DoubleIndent => Indent + Indent;
    public static string TripleIndent => DoubleIndent + Indent;
    
    public static string GetOriginatorClassTemplate(string nameSpace, string parentClassName, string mementoClassName, string restoreImpl, string innerClass) =>
$@"namespace {nameSpace};

using Kinen.Generator;

public partial class {parentClassName} : {IOriginatorHelper.InterfaceName}
{{
    public IMemento CreateMemento()
    {{
        return new {mementoClassName}(this);
    }}

    public void RestoreMemento(IMemento memento)
    {{
        if (memento is not {mementoClassName} concreteMemento) throw new ArgumentException(""memento is not {mementoClassName}"");
{restoreImpl}
    }}

{innerClass}
}}
";

    public static string GetMementoClassTemplate(string parentClassName, string mementoClassName,
        string constructorImpl, string propertiesImpl, string fieldsImpl)
    {
        if (!string.IsNullOrWhiteSpace(propertiesImpl))
            propertiesImpl = $"{DoubleIndent}//Properties\n{propertiesImpl}";
        if (!string.IsNullOrWhiteSpace(fieldsImpl))
            fieldsImpl = $"{DoubleIndent}//Fields\n{propertiesImpl}";
        return $@"{Indent}private class {mementoClassName} : {IMementoHelper.InterfaceName} 
    {{
        public {mementoClassName}({parentClassName} originator)
        {{
{constructorImpl}
        }}
       
{propertiesImpl}

{fieldsImpl}
    }}
";
        
    }

    public static string GeneratePropertyString(IPropertySymbol arg) =>
        $"{DoubleIndent}public {arg.Type.ToDisplayString()} {arg.Name} {{ get; set; }}";

    public static string GenerateFieldString(IFieldSymbol arg) =>
        $"{DoubleIndent}public {arg.Type.ToDisplayString()} {arg.Name};";

    public static string GenerateConstructorMemberSet(ISymbol arg) => 
        $"{TripleIndent}this.{arg.Name} = originator.{arg.Name};";

    public static string GenerateRestoreMemberGet(ISymbol arg) =>
        $"{DoubleIndent}this.{arg.Name} = concreteMemento.{arg.Name};";
}