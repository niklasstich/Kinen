﻿//HintName: Foobarbaz.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Kinen.Generator;

namespace Kinen.Test
{
    public partial class Foobarbaz : IOriginator
    {
        public IMemento CreateMemento()
        {
            return new FoobarbazMemento(_bar);
        }

        public void RestoreMemento(IMemento memento)
        {
            if(memento is not FoobarbazMemento concreteMemento) throw new ArgumentException("memento is not FoobarbazMemento");
            this._bar = concreteMemento._bar;
        }

        private class FoobarbazMemento : IMemento
        {
            public FoobarbazMemento(string _bar)
            {
                this._bar = _bar;
            }

            public string _bar { get; }
        }
    }
}
