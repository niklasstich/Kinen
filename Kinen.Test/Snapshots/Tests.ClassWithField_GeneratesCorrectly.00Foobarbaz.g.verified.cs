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
		this._bar = concreteMemento._bar;
    }

	private class FoobarbazMemento : IMemento 
    {
        public FoobarbazMemento(Foobarbaz originator)
        {
			this._bar = originator._bar;
        }
       


		//Fields
		public string _bar;
    }

}
