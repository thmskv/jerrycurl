﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Jerrycurl.Relations
{
    public class One<T> : IEnumerable<T>, IEquatable<T>, IEquatable<One<T>>
    {
        private T value = default;

        public bool HasValue { get; private set; }
        public T Value
        {
            get => this.value;
            set
            {
                this.value = value;
                this.HasValue = true;
            }
        }

        public One() { }
        public One(T value)
        {
            this.Value = value;
        }

        public T ValueOrDefault => this.value;

        public void Clear()
        {
            this.value = default;
            this.HasValue = false;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (this.HasValue)
                yield return this.Value;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
        public override int GetHashCode() => this.HasValue ? (this.Value?.GetHashCode() ?? 0) : 0;
        public override bool Equals(object obj)
        {
            if (obj is T t)
                return this.Equals(t);
            else if (obj is One<T> mt)
                return this.Equals(mt);

            return false;
        }

        public bool Equals(One<T> other)
        {
            if (other == null)
                return false;
            else if (other.HasValue)
                return this.Equals(other.Value);
            else if (!this.HasValue)
                return true;

            return false;
        }

        public bool Equals(T other)
        {
            if (this.HasValue)
                return object.Equals(this.Value, other);

            return false;
        }
    }
}
