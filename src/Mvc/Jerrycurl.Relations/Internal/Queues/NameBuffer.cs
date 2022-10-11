using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal class NameBuffer
    {
        private readonly StringBuilder buffer;
        private readonly DotNotation notation;
        private int bufferStart;
        private int bufferEnd;

        public int Index { get; private set; } = -1;
        public string NamePart { get; }

        public NameBuffer(string namePart, DotNotation notation)
        {
            this.buffer = new StringBuilder(namePart);
            this.bufferStart = this.buffer.Length;
            this.notation = notation;
            this.NamePart = namePart;
        }

        public void Reset()
        {
            if (this.Index > -1)
            {
                this.buffer.Length = this.bufferStart - 1;
                this.Index = -1;
            }
        }

        public void Increment()
        {
            if (this.Index == -1)
            {
                this.buffer.Length = this.bufferStart++;
                this.buffer.Append(this.notation.IndexBefore);
            }

            this.Index++;
            this.buffer.Length = this.bufferStart;
            this.buffer.Append(this.Index);
            this.buffer.Append(this.notation.IndexAfter);

            this.bufferEnd = this.buffer.Length;
        }

        public string CombineWith(string namePart)
        {
            this.buffer.Length = this.bufferEnd;

            if (namePart.Length > 0)
            {
                this.buffer.Append(this.notation.Dot);
                this.buffer.Append(namePart);
            }

            return this.buffer.ToString();
        }
    }
}
