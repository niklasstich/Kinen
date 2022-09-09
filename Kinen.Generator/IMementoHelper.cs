namespace Kinen.Generator;

public class IMementoHelper
{
    public static string InterfaceName => "IMemento";
    public static string InterfaceNamespace => "Kinen.Generator";
    public static string InterfaceFullName => $"{InterfaceNamespace}.{InterfaceName}";

    public static string InterfaceCode => $@"namespace {InterfaceNamespace};

public interface {InterfaceName} 
{{
    
}}";
}