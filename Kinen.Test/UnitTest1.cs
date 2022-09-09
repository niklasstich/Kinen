using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Kinen.Test;

public class Tests
{
    [Test]
    public void Test1()
    {
        const string source = @"
using Kinen.Generator;

namespace Kinen.Consumer;

[Memento]
public partial class Foobarbaz
{
    public string Name {get;set;}
}
";
        RunTest(source);
    }

    private static void RunTest(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "Tests",
            new[] { syntaxTree },
            references);

        var generator = new Kinen.Generator.OriginatorGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
    }
}

public class TestHelper
{
    public static void Verify(string source)
    {
    }
}