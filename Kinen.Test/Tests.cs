namespace Kinen.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        VerifySourceGenerators.Enable();
    }
    [Test]
    public Task EmptyClass_ShouldGenerateNoFile()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Test;

[Memento]
public partial class Foobarbaz
{
}
";
        return TestHelper.Verify(source);
    }

    [Test]
    public Task ClassWithProperty_ShouldGenerateClassWithProperty()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Test;

[Memento]
public partial class Foobarbaz
{
    public string Bar { get; set; }
}
";
        return TestHelper.Verify(source);
    }

}