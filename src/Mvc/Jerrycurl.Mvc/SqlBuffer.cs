using Jerrycurl.Collections;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jerrycurl.Mvc
{
    public class SqlBuffer : ISqlBuffer
    {
        private readonly List<IndexedBuffer> innerBuffers;
        private readonly Stack<int> bufferStack = new Stack<int>();

        private IndexedBuffer currentBuffer;

        protected List<IParameter> Parameters => this.currentBuffer.Parameters;
        protected List<IUpdateBinding> Bindings => this.currentBuffer.Bindings;
        protected StringBuilder Text => this.currentBuffer.Text;
        protected List<SqlOffset> Offsets => this.currentBuffer.Offsets;

        public SqlBuffer()
        {
            this.currentBuffer = new IndexedBuffer();
            this.innerBuffers = new List<IndexedBuffer>() { this.currentBuffer };
        }

        public void Push(int batchIndex)
        {
            this.bufferStack.Push(batchIndex);

            if (batchIndex >= 0)
            {
                this.ReserveBuffer(batchIndex);
                this.currentBuffer = this.innerBuffers[batchIndex] ??= new IndexedBuffer();
            }
        }

        public void Pop()
        {
            if (this.bufferStack.Count > 0)
            {
                int batchIndex = this.bufferStack.Pop();

                if (batchIndex >= 0)
                {
                    if (this.bufferStack.Count > 0)
                        this.currentBuffer = this.innerBuffers[this.bufferStack.Peek()];
                    else
                        this.currentBuffer = this.innerBuffers[0];
                }
            }
        }

        public void Mark()
        {
            SqlOffset current = this.GetCurrentOffset();

            this.Offsets.Add(current);
        }

        private void ReserveBuffer(int batchIndex)
        {
            if (batchIndex >= this.innerBuffers.Count)
            {
                IEnumerable<IndexedBuffer> defaults = Enumerable.Range(0, batchIndex + 1 - this.innerBuffers.Count).Select(_ => (IndexedBuffer)null);

                this.innerBuffers.AddRange(defaults);
            }
        }

        public void Append(IEnumerable<IParameter> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            this.Parameters.AddRange(parameters);
        }

        public void Append(IEnumerable<IUpdateBinding> bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));

            this.Bindings.AddRange(bindings);
        }

        public void Append(ISqlContent content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            this.Bindings.AddRange(content.Bindings ?? Array.Empty<IUpdateBinding>());
            this.Parameters.AddRange(content.Parameters ?? Array.Empty<IParameter>());
            this.Text.Append(content.Text);
        }

        public void Append(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            this.Text.Append(text);
        }

        private SqlOffset GetCurrentOffset()
        {
            return new SqlOffset()
            {
                NumberOfParams = this.Parameters.Count,
                NumberOfBindings = this.Bindings.Count,
                Text = this.Text.Length,
            };
        }

        public ISqlContent ReadToEnd()
        {
            List<IParameter> parameters = new List<IParameter>();
            List<IUpdateBinding> bindings = new List<IUpdateBinding>();
            StringBuilder text = new StringBuilder();

            foreach (IndexedBuffer buffer in this.innerBuffers.NotNull())
            {
                parameters.AddRange(buffer.Parameters);
                bindings.AddRange(buffer.Bindings);
                text = text.Append(buffer.Text.ToString());
            }

            return new SqlContent()
            {
                Bindings = bindings,
                Parameters = parameters,
                Text = text.ToString(),
            };
        }

        public IEnumerable<ISqlContent> Read(ISqlOptions options)
        {
            foreach (IndexedBuffer buffer in this.innerBuffers.NotNull())
                foreach (ISqlContent batch in this.Read(buffer, options))
                    yield return batch;
        }

        private IEnumerable<ISqlContent> Read(IndexedBuffer buffer, ISqlOptions options)
        {
            if (options == null || (options.MaxParameters <= 0 && options.MaxSql <= 0) || (options.MaxParameters >= this.Parameters.Count && options.MaxSql >= this.Text.Length))
            {
                yield return this.ReadToEnd();
                yield break;
            }

            int yieldedParams = 0;
            int yieldedBindings = 0;
            int yieldedText = 0;

            int maxSql = options.MaxSql <= 0 ? int.MaxValue : options.MaxSql;
            int maxParams = options.MaxParameters <= 0 ? int.MaxValue : options.MaxParameters;

            SqlOffset[] offsets = buffer.Offsets.Concat(new[] { this.GetCurrentOffset() }).ToArray();

            for (int i = 0; i < offsets.Length - 1; i++)
            {
                SqlOffset offset = offsets[i];
                SqlOffset nextOffset = offsets[i + 1];

                if (nextOffset.NumberOfParams - yieldedParams > maxParams || nextOffset.Text - yieldedText > maxSql)
                {
                    yield return new SqlContent()
                    {
                        Bindings = buffer.Bindings.Skip(yieldedBindings).Take(offset.NumberOfBindings - yieldedBindings),
                        Parameters = buffer.Parameters.Skip(yieldedParams).Take(offset.NumberOfParams - yieldedParams),
                        Text = buffer.Text.ToString(yieldedText, offset.Text - yieldedText),
                    };

                    yieldedParams += offset.NumberOfParams - yieldedParams;
                    yieldedText += offset.Text - yieldedText;
                    yieldedBindings += offset.NumberOfBindings - yieldedBindings;
                }
            }

            if (yieldedParams < buffer.Parameters.Count || yieldedText < buffer.Text.Length || yieldedBindings < buffer.Bindings.Count)
            {
                string newText = buffer.Text.ToString(yieldedText, buffer.Text.Length - yieldedText);

                if (!string.IsNullOrWhiteSpace(newText))
                {
                    yield return new SqlContent()
                    {
                        Bindings = buffer.Bindings.Skip(yieldedBindings),
                        Parameters = buffer.Parameters.Skip(yieldedParams),
                        Text = newText,
                    };
                }
            }
        }

        private class IndexedBuffer
        {
            public List<IParameter> Parameters { get; } = new List<IParameter>();
            public List<IUpdateBinding> Bindings { get; } = new List<IUpdateBinding>();
            public StringBuilder Text { get; } = new StringBuilder();
            public List<SqlOffset> Offsets { get; } = new List<SqlOffset>();
        }
    }
}
