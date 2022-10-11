using Shouldly;
using Jerrycurl.Test;
using Jerrycurl.Relations.Language;
using Jerrycurl.Cqs.Metadata;
using System.Linq;
using Jerrycurl.Cqs.Test.Models.Custom;
using Jerrycurl.Cqs.Test.Models.Views;

namespace Jerrycurl.Cqs.Test
{
    public class MetadataTests
    {
        public void Test_ReferenceMetadata_Keys()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema<BlogView>();
            var schema2 = store.GetSchema<SecondaryModel>();

            var parentKeys1 = schema1.Require<IReferenceMetadata>().Keys;
            var childKeys1 = schema1.Require<IReferenceMetadata>("Posts.Item").Keys;
            var parentKeys2 = schema2.Require<IReferenceMetadata>().Keys;
            var childKeys2 = schema2.Require<IReferenceMetadata>("Many.Item").Keys;

            var parentPk1 = parentKeys1.First(k => k.Name == "PK_Blog");
            var parentCk2 = parentKeys2.First(k => k.Name == "PK_Secondary");

            var childFk1 = childKeys1.First(k => k.Other == "PK_Blog");
            var childFk2 = childKeys2.First(k => k.Other == "PK_Secondary");

            parentPk1.Flags.ShouldBe(ReferenceKeyFlags.Primary);
            parentCk2.Flags.ShouldBe(ReferenceKeyFlags.Candidate);

            childFk1.Flags.ShouldBe(ReferenceKeyFlags.Foreign);
            childFk2.Flags.ShouldBe(ReferenceKeyFlags.Foreign);
        }

        public void Test_ReferenceMetadata_References()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema<BlogView>();
            var schema2 = store.GetSchema<SecondaryModel>();

            var parentRefs1 = schema1.Require<IReferenceMetadata>().References;
            var childRefs1 = schema1.Require<IReferenceMetadata>("Posts.Item").References;
            var parentRefs2 = schema2.Require<IReferenceMetadata>().References;
            var childRefs2 = schema2.Require<IReferenceMetadata>("Many.Item").References;

            var parentPr1 = parentRefs1.First(r => r.Key.Name == "PK_Blog");
            var parentCr2 = parentRefs2.First(r => r.Key.Name == "PK_Secondary");

            var childFr1 = childRefs1.First(r => r.Key.Other == "PK_Blog");
            var childFr2 = childRefs2.First(r => r.Key.Other == "PK_Secondary");

            parentPr1.HasFlag(ReferenceFlags.Primary).ShouldBeTrue();
            parentCr2.HasFlag(ReferenceFlags.Candidate).ShouldBeTrue();
            parentCr2.HasFlag(ReferenceFlags.Primary).ShouldBeFalse();

            childFr1.HasFlag(ReferenceFlags.Foreign).ShouldBeTrue();
            childFr2.HasFlag(ReferenceFlags.Foreign).ShouldBeTrue();

            parentPr1.Other.ShouldBe(childFr1);
            parentCr2.Other.ShouldBe(childFr2);
        }
    }
}
