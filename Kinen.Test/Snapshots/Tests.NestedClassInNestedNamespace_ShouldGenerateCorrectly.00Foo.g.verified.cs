﻿//HintName: Foo.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Kinen.Generator;

namespace Kinen.Test.Nested.NestedAgain
{
    public partial class Foo
    {
        private partial class Bar : IOriginator
        {
            public IMemento CreateMemento()
            {
                return new BarMemento(PublicProperty);
            }

            public void RestoreMemento(IMemento memento)
            {
                if (memento is not BarMemento concreteMemento)
                    throw new ArgumentException("memento is not BarMemento");
                this.PublicProperty = concreteMemento.PublicProperty;
            }

            private class BarMemento : IMemento
            {
                public BarMemento(string PublicProperty)
                {
                    this.PublicProperty = PublicProperty;
                }

                public string PublicProperty { get; }
            }
        }
    }
}
