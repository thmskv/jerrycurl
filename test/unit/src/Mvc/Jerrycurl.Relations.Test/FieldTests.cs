using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Test;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class FieldTests
    {
        public void Test_Field_FieldTypes()
        {
            var store = DatabaseHelper.Default.Store;
            var model = new RootModel() { Complex = new RootModel.SubModel() };
            var result = store.From(model).Select("", "Complex", "Complex.Complex", "Complex.Complex.Value").Row();

            result[0].Type.ShouldBe(FieldType.Model);
            result[1].Type.ShouldBe(FieldType.Value);
            result[2].Type.ShouldBe(FieldType.Value);
            result[3].Type.ShouldBe(FieldType.Missing);
        }


        public void Test_Fields_EqualityImplementation()
        {
            var store = DatabaseHelper.Default.Store;
            var model1 = new RootModel()
            {
                ComplexList = new List<RootModel.SubModel>()
                {
                    new RootModel.SubModel() { Value = 1 },
                    new RootModel.SubModel() { Value = 2 },
                }
            };
            var model2 = new RootModel()
            {
                ComplexList = new List<RootModel.SubModel>()
                {
                    new RootModel.SubModel() { Value = 1 },
                    new RootModel.SubModel() { Value = 2 },
                }
            };

            var result1_1 = store.From(model1).Select("ComplexList.Item.Value").Column().ToArray();
            var result1_2 = store.From(model1).Select("ComplexList.Item.Value").Column().ToArray();
            var result2_1 = store.From(model2).Select("ComplexList.Item.Value").Column().ToArray();

            result1_1.ShouldBe(result1_2);
            result1_1.ShouldNotBe(result2_1);

            result1_1.Select(f => f.Identity).ShouldBe(result2_1.Select(f => f.Identity));
        }
    }
}
