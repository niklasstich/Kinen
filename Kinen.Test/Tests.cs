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
    public Task ClassWithProperty_GeneratesCorrectly()
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

    [Test]
    public Task ClassWithField_GeneratesCorrectly()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Test;

[Memento]
public partial class Foobarbaz
{
    public string _bar;
}
";
        return TestHelper.Verify(source);
        
    }
    
    [Test]
    public Task ClassWithFieldAndProperty_GeneratesCorrectly()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Test;

[Memento]
public partial class Foobarbaz
{
    public string Foo { get; set; }
    public string _bar;
}
";
        return TestHelper.Verify(source);
        
    }

    [Test]
    public Task FieldsAndPropertiesWithMementoSkip_IgnoredInGeneration()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Test;

[Memento]
public partial class Foobarbaz
{
    [MementoSkip]
    public string IgnoreMe { get; set; }
    [MementoSkip]
    public string _ignoreMe;
    public string Foo { get; set; }
    public string _bar;
}
";
        return TestHelper.Verify(source);
        
    }

    [Test]
    public Task NonPartialClass_ShouldGenerateDiagnosticError()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Test;

[Memento]
public class Foobarbaz
{
    public string Bar { get; set; }
}
";
        return TestHelper.Verify(source);
    }

}