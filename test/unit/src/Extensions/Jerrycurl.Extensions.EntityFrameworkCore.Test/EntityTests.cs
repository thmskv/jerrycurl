using System.Collections.Generic;
using Jerrycurl.Extensions.EntityFrameworkCore.Test.Entities;
using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Shouldly;
using Jerrycurl.Relations.Language;
using Jerrycurl.Cqs.Metadata;
using System.Linq;
using Jerrycurl.Extensions.EntityFrameworkCore.Metadata;
using Jerrycurl.Collections;

namespace Jerrycurl.Extensions.EntityFrameworkCore.Test
{
    public class EntityTests
    {
        public void Test_EFCore_TableMetadata()
        {
            var store = DatabaseHelper.Default.GetStore(contracts: new[] { new EntityFrameworkCoreContractResolver(new EntityContext()) });
            var address = store.GetSchema<Address>().Lookup<ITableMetadata>();
            var addressView = store.GetSchema<AddressView>().Lookup<ITableMetadata>();

            address.ColumnName.ShouldBeNull();
            address.TableName.ShouldBe(new[] { "Address" });
            address.Properties.Select(m => m.ColumnName).NotNull().ShouldBe(new[] { "Street", "Id" });

            addressView.ColumnName.ShouldBeNull();
            addressView.TableName.ShouldBe(new[] { "Address" });
            addressView.Properties.Select(m => m.ColumnName).NotNull().ShouldBe(new[] { "Street", "Id" });
        }

        public void Test_EfCore_ReferenceMetadata()
        {
            var store = DatabaseHelper.Default.GetStore(contracts: new[] { new EntityFrameworkCoreContractResolver(new EntityContext()) });
            var schema = store.GetSchema<List<Order>>();

            var orderRefs = schema.Require<IReferenceMetadata>("Item").References;
            var lineRefs = schema.Require<IReferenceMetadata>("Item.OrderLine.Item").References;
            var addressRefs = schema.Require<IReferenceMetadata>("Item.BillingAddress").References;

            var pk1 = orderRefs.FirstOrDefault(r => r.HasFlag(ReferenceFlags.Parent) && r.Key.Properties[0].Identity.Name == "Item.Id");
            var fk1 = lineRefs.FirstOrDefault(r => r.HasFlag(ReferenceFlags.Child) && r.Key.Properties[0].Identity.Name == "Item.OrderLine.Item.OrderId");

            var pk2 = addressRefs.FirstOrDefault(r => r.HasFlag(ReferenceFlags.Child) && r.Key.Properties[0].Identity.Name == "Item.BillingAddress.Id");
            var fk2 = orderRefs.FirstOrDefault(r => r.HasFlag(ReferenceFlags.Parent) && r.Key.Properties[0].Identity.Name == "Item.BillingAddressId");

            pk1.ShouldNotBeNull();
            pk1.HasFlag(ReferenceFlags.Primary | ReferenceFlags.One).ShouldBeTrue();
            fk1.ShouldNotBeNull();
            fk1.HasFlag(ReferenceFlags.Foreign | ReferenceFlags.Many).ShouldBeTrue();

            pk2.ShouldNotBeNull();
            pk2.HasFlag(ReferenceFlags.Primary | ReferenceFlags.One).ShouldBeTrue();
            fk2.ShouldNotBeNull();
            fk2.HasFlag(ReferenceFlags.Foreign | ReferenceFlags.One).ShouldBeTrue();
        }

        public void Test_EfCore_Crud()
        {
            Runnable<object, AddressView> table = new Runnable<object, AddressView>();

            table.Sql("CREATE TABLE IF NOT EXISTS ");
            table.R(p => p.TblName());
            table.Sql("( ");
            table.R(p => p.ColName(m => m.Id));
            table.Sql(" );");
            table.Sql("DELETE FROM ");
            table.R(p => p.TblName());
            table.Sql(";");
            table.Sql("INSERT INTO ");
            table.R(p => p.TblName());
            table.Sql(" VALUES (12);");
            table.Sql("SELECT ");
            table.R(p => p.Col(m => m.Id));
            table.Sql(" AS ");
            table.R(p => p.Prop(m => m.Id));
            table.Sql(" FROM ");
            table.R(p => p.Tbl());
            table.Sql(";");

            IList<AddressView> addresses = Runner.Query(table);

            addresses.ShouldNotBeNull();
            addresses.Count.ShouldBe(1);
            addresses[0].Id.ShouldBe(12);
        }

        public void Test_EfCore_Query_OneToMany()
        {
            Runnable<object, Order> table = new Runnable<object, Order>();

            table.Sql("SELECT ");
            table.Sql("1 AS "); table.R(p => p.Prop(m => m.Id));
            table.Sql(",1 AS "); table.R(p => p.Prop(m => m.BillingAddress.Id));
            table.Sql(",1 AS "); table.R(p => p.Prop(m => m.ShippingAddress.Id));
            table.Sql(" UNION ALL SELECT ");
            table.Sql("2 AS "); table.R(p => p.Prop(m => m.Id));
            table.Sql(",2 AS "); table.R(p => p.Prop(m => m.BillingAddress.Id));
            table.Sql(",NULL AS "); table.R(p => p.Prop(m => m.ShippingAddress.Id));

            table.Sql(";SELECT ");
            table.Sql("1 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Id));
            table.Sql(",1 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.OrderId));
            table.Sql(",'Product 1' AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Product));
            table.Sql(" UNION ALL SELECT ");
            table.Sql("2 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Id));
            table.Sql(",1 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.OrderId));
            table.Sql(",'Product 2' AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Product));
            table.Sql(" UNION ALL SELECT ");
            table.Sql("3 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Id));
            table.Sql(",1 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.OrderId));
            table.Sql(",'Product 3' AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Product));
            table.Sql(" UNION ALL SELECT ");
            table.Sql("4 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Id));
            table.Sql(",2 AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.OrderId));
            table.Sql(",'Product 1' AS "); table.R(p => p.Open(m => m.OrderLine).Prop(m => m.Product));

            IList<Order> orders = Runner.Query(table);

            orders.Count.ShouldBe(2);
            orders[0].BillingAddress.ShouldNotBeNull();
            orders[0].ShippingAddress.ShouldNotBeNull();
            orders[0].OrderLine.ShouldNotBeNull();
            orders[0].OrderLine.Count.ShouldBe(3);

            orders[1].BillingAddress.ShouldNotBeNull();
            orders[1].ShippingAddress.ShouldBeNull();
            orders[1].OrderLine.ShouldNotBeNull();
            orders[1].OrderLine.Count.ShouldBe(1);
        }
    }
}
