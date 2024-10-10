﻿using System.Collections.Generic;
using System.Data;
using Jerrycurl.Cqs.Sessions;

namespace Jerrycurl.Cqs.Commands;

public class Command : IBatch
{
    public string CommandText { get; set; }

    public ICollection<IUpdateBinding> Bindings { get; set; } = [];
    public ICollection<IParameter> Parameters { get; set; } = [];

    public void Build(IDbCommand adoCommand)
    {
        CommandBuffer buffer = new CommandBuffer();

        adoCommand.CommandText = this.CommandText;

        foreach (IDbDataParameter parameter in buffer.Prepare(adoCommand))
            adoCommand.Parameters.Add(parameter);
    }
}
