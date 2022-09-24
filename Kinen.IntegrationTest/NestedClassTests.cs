using Kinen.Generator;
using Kinen.IntegrationTest.Nested.NestedAgain;
using NSubstitute;
using NUnit.Framework;

namespace Kinen.IntegrationTest;

[TestFixture]
public class NestedClassTests
{
    /*
    [Test]
    public void NestedClass_RestoreMemento_RestoresCorrectly()
    {
        var systemUnderTest = new ParentClass.NestedClass
        {
            Name = "foobar"
        };

        var memento = systemUnderTest.CreateMemento();
        
        systemUnderTest.Name = "barfoo";
        
        systemUnderTest.RestoreMemento(memento);
        
        Assert.That(systemUnderTest.Name, Is.EqualTo("foobar"));
    }

    [Test]
    public void NestedClass_RestoreMementoWithIncorrectMementoImplementation_ThrowsException()
    {
        var fakeMemento = Substitute.For<IMemento>();
        
        var systemUnderTest = new ParentClass.NestedClass();

        Assert.That(() => systemUnderTest.RestoreMemento(fakeMemento),
            Throws.ArgumentException.With.Message.EqualTo("memento is not BasicClassMemento"));
    }
    */
}