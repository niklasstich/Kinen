using Kinen.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Kinen.Test;


public static class TestHelper
{
    public static Task Verify(String source)
    {
        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        CSharpCompilation compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var generator = new OriginatorGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver, settings);
    }
}