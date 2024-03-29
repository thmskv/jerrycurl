﻿using Jerrycurl.Mvc;
using Jerrycurl.Test;

namespace Jerrycurl.Extensions.EntityFrameworkCore.Test
{
    public class EntityConvention : DatabaseConvention
    {
        public override void Configure(DomainOptions options)
        {
            options.UseSqlite("DATA SOURCE=ef.db");
            options.UseEntityFrameworkCore<EntityContext>();
        }
    }
}
