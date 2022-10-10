using Kinen.Generator;

namespace Kinen.IntegrationTest;

[Memento]
public partial class BasicClass
{
    public int Number { get; set; }
    public string Text { get; set; }
    public object _object;
    public Guid _guid;
}