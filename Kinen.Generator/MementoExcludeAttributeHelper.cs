namespace Kinen.Generator;

public class MementoExcludeAttributeHelper
{
    public static string AttributeName => "MementoExclude";
    public static string Namespace => "Kinen.Generator";
    public static string AttributeFullName => $"{Namespace}.{AttributeName}";
    public static string AttributeCode => @$"namespace {Namespace}
{{
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class {AttributeName} : System.Attribute
    {{
    }}
}}";
}