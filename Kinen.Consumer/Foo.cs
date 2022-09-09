using Kinen.Generator;

namespace Kinen.Consumer;

[Memento]
public partial class Foo
{
    [MementoSkip]
    private string _name;

    public string Name
    {
        get => _name;
        set => _name = value;
    }
}