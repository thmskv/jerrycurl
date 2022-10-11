using System.Collections.Generic;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Queries.Internal
{
    internal class FieldArray
    {
        private readonly List<IField> innerList = new List<IField>(2);

        public IField this[int index]
        {
            get
            {
                this.EnsureIndex(index);

                return this.innerList[index];
            }
            set
            {
                this.EnsureIndex(index);

                this.innerList[index] = value;
            }
        }

        private void EnsureIndex(int index)
        {
            if (index >= this.innerList.Count)
                this.innerList.AddRange(new IField[index - this.innerList.Count + 1]);
        }
    }
}