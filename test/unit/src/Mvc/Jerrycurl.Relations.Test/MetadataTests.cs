using Jerrycurl.Cqs.Language;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Test.Metadata;
using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Test;
using Shouldly;
using System;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class MetadataTests
    {
        public void Test_SchemaStore_CachesProperly()
        {
            var store1 = DatabaseHelper.Default.GetStore();
            var store2 = DatabaseHelper.Default.GetStore();

            var schema1_1 = store1.GetSchema(typeof(RootModel));
            var schema1_2 = store1.GetSchema(typeof(RootModel));
            var schema2_1 = store2.GetSchema(typeof(RootModel));
            var schema2_2 = store2.GetSchema(typeof(RootModel));

            schema1_1.ShouldBeSameAs(schema1_2);
            schema2_1.ShouldBeSameAs(schema2_2);
            schema1_1.ShouldNotBeSameAs(schema2_1);
        }

        public void Test_Schema_CachesProperly()
        {
            var store1 = DatabaseHelper.Default.GetStore();
            var store2 = DatabaseHelper.Default.GetStore();

            var schema1 = store1.GetSchema(typeof(RootModel));
            var schema2 = store2.GetSchema(typeof(RootModel));

            var metadata1_1 = schema1.Lookup<IRelationMetadata>(nameof(RootModel.IntValue));
            var metadata1_2 = schema1.Lookup<IRelationMetadata>(nameof(RootModel.IntValue));
            var metadata2_1 = schema2.Lookup<IRelationMetadata>(nameof(RootModel.IntValue));
            var metadata2_2 = schema2.Lookup<IRelationMetadata>(nameof(RootModel.IntValue));

            metadata1_1.ShouldBeSameAs(metadata1_2);
            metadata2_1.ShouldBeSameAs(metadata2_2);
            metadata1_1.ShouldNotBeSameAs(metadata2_1);
        }

        public void Test_SchemaStore_DisallowsRecursion()
        {
            var store = new SchemaStore(new DotNotation(StringComparer.Ordinal), new RecursiveMetadataBuilder());
            var schema = store.GetSchema(typeof(TupleModel));

            schema.ShouldNotBeNull();

            Should.Throw<MetadataBuilderException>(() => schema.Lookup<CustomMetadata>("Value"));
        }

        public void Test_Notation_StringComparison()
        {
            var sensitive = new SchemaStore(new DotNotation(StringComparer.Ordinal));
            var insensitive = new SchemaStore(new DotNotation());

            var sensitive1 = sensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("List.Item.Name");
            var sensitive2 = sensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("list.item.name");

            var insensitive1 = insensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("List.Item.Name");
            var insensitive2 = insensitive.GetSchema(typeof(TupleModel)).Lookup<IRelationMetadata>("list.item.name");

            sensitive1.ShouldNotBeNull();
            sensitive2.ShouldBeNull();

            insensitive1.ShouldNotBeNull();
            insensitive2.ShouldNotBeNull();
        }

        public void Test_Metadata_Custom_Contract()
        {
            var store = new SchemaStore(new DotNotation());

            store.Use(new CustomContractResolver());

            var schema1 = DatabaseHelper.Default.Store.GetSchema(typeof(CustomModel));
            var schema2 = store.GetSchema(typeof(CustomModel));

            var notFound = schema1.Lookup<IRelationMetadata>("Values.Item");
            var found = schema2.Lookup<IRelationMetadata>("Values.Item");

            notFound.ShouldBeNull();
            found.ShouldNotBeNull();
            found.Type.ShouldBe(typeof(int));
            found.Annotations.OfType<CustomAttribute>().FirstOrDefault().ShouldNotBeNull();
        }

        public void Test_Metadata_Invalid_Constract()
        {
            var customStore = new SchemaStore(new DotNotation());
            customStore.Use(new InvalidContractResolver());

            var schema = customStore.GetSchema(typeof(CustomModel));

            Should.Throw<MetadataBuilderException>(() => schema.Lookup<IRelationMetadata>("List1"));
        }

        public void Test_OneType_Equality()
        {
            var empty = new One<int>();
            var zero = new One<int>(0);
            var one = new One<int>(1);

            var empty2 = new One<int>();
            var zero2 = new One<int>(0);
            var one2 = new One<int>(1);

            empty.Equals(empty2).ShouldBeTrue();
            empty.Equals(zero).ShouldBeFalse();
            empty.Equals(0).ShouldBeFalse();
            empty.Equals(one).ShouldBeFalse();
            empty.Equals(1).ShouldBeFalse();

            zero.Equals(empty).ShouldBeFalse();
            zero.Equals(zero2).ShouldBeTrue();
            zero.Equals(0).ShouldBeTrue();
            zero.Equals(one).ShouldBeFalse();
            zero.Equals(1).ShouldBeFalse();

            one.Equals(empty).ShouldBeFalse();
            one.Equals(zero).ShouldBeFalse();
            one.Equals(0).ShouldBeFalse();
            one.Equals(one2).ShouldBeTrue();
            one.Equals(1).ShouldBeTrue();
        }
    }
}
