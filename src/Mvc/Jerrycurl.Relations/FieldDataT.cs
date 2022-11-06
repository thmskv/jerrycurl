﻿using System;
using Jerrycurl.Relations.Internal.Compilation;

namespace Jerrycurl.Relations
{
    internal class FieldData<TValue, TParent> : IFieldData
    {
        public TParent Parent { get; }
        public TValue Value { get; set; }
        public int Index { get; }
        public object Relation { get; }
        public FieldBinder<TParent, TValue> Binder { get; }

        public FieldData(object relation, int index, TParent parent, TValue value, Delegate binder)
        {
            this.Relation = relation;
            this.Index = index;
            this.Parent = parent;
            this.Value = value;
            this.Binder = (FieldBinder<TParent, TValue>)binder;
        }

        public void Bind(TValue newValue)
        {
            this.Binder(this.Parent, this.Index, newValue);
            this.Value = newValue;
        }

        object IFieldData.Parent => this.Parent;
        object IFieldData.Value => this.Value;
    }
}
