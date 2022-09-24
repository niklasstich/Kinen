//HintName: IOriginator.g.cs
namespace Kinen.Generator;

public interface IOriginator
{
    IMemento CreateMemento();
    void RestoreMemento(IMemento memento);
}