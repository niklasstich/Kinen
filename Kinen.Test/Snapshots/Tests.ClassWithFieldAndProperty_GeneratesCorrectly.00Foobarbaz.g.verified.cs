//HintName: Foobarbaz.g.cs
namespace Kinen.Test;

using Kinen.Generator;

public partial class Foobarbaz : IOriginator
{
    public IMemento CreateMemento()
    {
        return new FoobarbazMemento(this);
    }

    public void RestoreMemento(IMemento memento)
    {
        if (memento is not FoobarbazMemento concreteMemento) throw new ArgumentException("memento is not FoobarbazMemento");
		this.Foo = concreteMemento.Foo;
		this._bar = concreteMemento._bar;
    }

	private class FoobarbazMemento : IMemento 
    {
        public FoobarbazMemento(Foobarbaz originator)
        {
			this.Foo = originator.Foo;
			this._bar = originator._bar;
        }
       
		//Properties
		public string Foo { get; set; }

		//Fields
		public string _bar;
    }

}
