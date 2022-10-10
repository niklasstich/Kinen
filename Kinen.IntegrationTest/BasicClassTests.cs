using Kinen.Generator;
using NSubstitute;
using NUnit.Framework;

namespace Kinen.IntegrationTest;

public class BasicClassTests
{
    [Test]
    public void BasicClass_RestoreMemento_RestoresCorrectly()
    {
        var obj = new object();
        var guid = Guid.NewGuid();
        var systemUnderTest = new BasicClass
        {
            Number = 42,
            Text = "Hello World",
            _object = obj,
            _guid = guid 
        };

        var memento = systemUnderTest.CreateMemento();
        
        systemUnderTest.Number = 0;
        systemUnderTest.Text = "Goodbye World";
        systemUnderTest._object = null!;
        systemUnderTest._guid = Guid.Empty;
        
        systemUnderTest.RestoreMemento(memento);
        
        Assert.Multiple(() =>
        {
            Assert.That(systemUnderTest.Number, Is.EqualTo(42));
            Assert.That(systemUnderTest.Text, Is.EqualTo("Hello World"));
            Assert.That(systemUnderTest._object, Is.EqualTo(obj));
            Assert.That(systemUnderTest._guid, Is.EqualTo(guid));
        });
    }

    [Test]
    public void BasicClass_RestoreMementoWithIncorrectMementoImplementation_ThrowsException()
    {
        var fakeMemento = Substitute.For<IMemento>();
        
        var systemUnderTest = new BasicClass();

        Assert.That(() => systemUnderTest.RestoreMemento(fakeMemento),
            Throws.ArgumentException.With.Message.EqualTo("memento is not BasicClassMemento"));
    }
}