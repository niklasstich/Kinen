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
		this.Bar = concreteMemento.Bar;
    }

	private class FoobarbazMemento : IMemento 
    {
        public FoobarbazMemento(Foobarbaz originator)
        {
			this.Bar = originator.Bar;
        }
       
		//Properties
		public string Bar { get; set; }


    }

}
