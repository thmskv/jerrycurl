﻿using System;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Commands
{
    public class ColumnBinding : IUpdateBinding
    {
        public string ColumnName { get; }
        public IField Target { get; }

        public ColumnBinding(IField target, string columnName)
        {
            this.ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public ColumnBinding(IField target)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.ColumnName = this.Target.Identity.Name;
        }

        public override string ToString() => $"ColumnBinding: {this.ColumnName} -> {this.Target.Identity}";
    }
}
