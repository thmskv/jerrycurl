﻿using System.Collections.Generic;

namespace Jerrycurl.Extensions.EntityFrameworkCore.Test.Entities
{
    public partial class Order : BaseEntity
    {
        public Order()
        {
            this.OrderLine = new HashSet<OrderLine>();
        }

        public int BillingAddressId { get; set; }
        public int? ShippingAddressId { get; set; }

        public virtual Address BillingAddress { get; set; }
        public virtual Address ShippingAddress { get; set; }
        public virtual ICollection<OrderLine> OrderLine { get; set; }
    }
}
