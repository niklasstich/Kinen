namespace Kinen.Generator;

public class IOriginatorHelper
{
    public static string InterfaceName => "IOriginator";
    public static string InterfaceNamespace => "Kinen.Generator";
    public static string InterfaceFullName => $"{InterfaceNamespace}.{InterfaceName}";
    public static string InterfaceCode => $@"namespace {InterfaceNamespace};

public interface {InterfaceName}
{{
    IMemento CreateMemento();
    void RestoreMemento(IMemento memento);
}}";
}