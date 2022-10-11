using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Relations.Language;
using Jerrycurl.Test;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class LanguageTests
    {
        public static void Test_Select_ZeroDimensional()
        {
            var store = DatabaseHelper.Default.Store;
            var actual = store.For<RootModel>()
                              .Select(m => m.Object)
                              .Select(m => m.ReadOnly);

            var expected = store.GetSchema<RootModel>()
                                .Select("Object", "ReadOnly");

            actual.ShouldBe(expected);
        }

        public static void Test_Select_OneDimensional()
        {
            var store = DatabaseHelper.Default.Store;
            var actual = store.For<List<RootModel>>()
                                         .Join(m => m)
                                         .Select(m => m.Object)
                                         .Select(m => m.ReadOnly)
                                         .Select(m => m.Complex.Value);

            var expected = store.GetSchema<List<RootModel>>()
                                .Select("Item.Object", "Item.ReadOnly", "Item.Complex.Value");

            actual.ShouldBe(expected);
        }

        public static void Test_LambdaSelect_TwoDimensional()
        {
            var store = DatabaseHelper.Default.Store;
            var actual = store.For<List<RootModel>>()
                              .Join(m => m)
                              .Select()
                              .Select(m => m.IntValue)
                              .Select(m => m.Complex)
                              .Join(m => m.IntList)
                              .Select();

            var expected = store.GetSchema<List<RootModel>>()
                                .Select("Item", "Item.IntValue", "Item.Complex", "Item.IntList.Item");

            actual.ShouldBe(expected);
        }

        public static void Test_LambdaSelect_SelectAll()
        {
            var store = DatabaseHelper.Default.Store;
            var actual = store.For<RootModel>()
                              .SelectAll(m => m.Complex);

            var expected = store.GetSchema<RootModel>()
                                .Select("Complex.Value", "Complex.Complex");

            actual.ShouldBe(expected);
        }
    }
}
