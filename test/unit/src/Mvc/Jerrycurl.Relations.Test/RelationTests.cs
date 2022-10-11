using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Test.Models;
using Jerrycurl.Relations.Language;
using Jerrycurl.Test;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Test
{
    public class RelationTests
    {
        public void Test_Update_Model_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel();
            var model = store.From(data).Lookup("");

            Should.Throw<BindingException>(() =>
            {
                model.Update(new RootModel());
                model.Commit();
            });
        }

        public void Test_Update_Missing_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel();
            var value = store.From(data).Lookup("Complex.Value");

            Should.Throw<BindingException>(() =>
            {
                value.Update(10);
                value.Commit();
            });
        }

        public void Test_Update_ReadOnlyProperty_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var value = store.From(new RootModel()).Lookup("ReadOnly");

            Should.Throw<BindingException>(() =>
            {
                value.Update(12);
                value.Commit();
            });
        }

        public void Test_Update_NonConvertibleValue_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel();
            var value = store.From(data).Lookup("Complex.Value");

            Should.Throw<BindingException>(() =>
            {
                value.Update("String");
                value.Commit();
            });
        }


        public void Test_Update_NullToValueType_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel() { Complex = new RootModel.SubModel() };
            var value = store.From(data).Lookup("Complex.Value");

            Should.Throw<BindingException>(() =>
            {
                value.Update(null);
                value.Commit();
            });
        }


        public void Test_Update_Property()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel() { Complex = new RootModel.SubModel() { Value = 6 } };
            var value = store.From(data).Lookup("Complex.Value");

            value.ShouldNotBeNull();

            value.Update(12);
            value.Snapshot.ShouldBe(12);
            value.Data.Value.ShouldBe(6);
            data.Complex.Value.ShouldBe(6);

            value.Commit();
            value.Snapshot.ShouldBe(12);
            value.Data.Value.ShouldBe(12);
            data.Complex.Value.ShouldBe(12);
        }

        public void Test_Update_NullValue()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel();
            var complex = store.From(data).Lookup("Complex");

            complex.ShouldNotBeNull();
            complex.Snapshot.ShouldBeNull();

            Should.NotThrow(() =>
            {
                complex.Update(new RootModel.SubModel() { Value = 10 });
                complex.Commit();
            });

            data.Complex.Value.ShouldBe(10);
        }

        public void Test_Update_ListIndexer()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel() { IntList = new List<int>() { 1, 2, 3, 4, 5 } };
            var rel = store.From(data).Select("IntList.Item");
            var value = rel.Column().ElementAt(2);

            Should.NotThrow(() =>
            {
                value.Update(10);
                value.Commit();
            });

            data.IntList.ShouldBe(new[] { 1, 2, 10, 4, 5 });
        }

        public void Test_Update_EnumerableIndexer_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel() { IntEnumerable = Enumerable.Range(1, 5) };
            var rel = store.From(data).Select("IntEnumerable.Item");
            var value = rel.Column().ElementAt(2);

            Should.Throw<BindingException>(() =>
            {
                value.Update(10);
                value.Commit();
            });
        }

        public void Test_Update_EnumerableListIndexer()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel() { IntEnumerable = new List<int>() { 1, 2, 3, 4, 5 } };
            var rel = store.From(data).Select("IntEnumerable.Item");
            var value = rel.Column().ElementAt(2);

            Should.NotThrow(() =>
            {
                value.Update(10);
                value.Commit();
            });

            data.IntEnumerable.ShouldBe(new[] { 1, 2, 10, 4, 5 });
        }

        public void Test_Update_Contravariant()
        {
            var store = DatabaseHelper.Default.Store;
            var model = new RootModel();
            var result = store.From(model).Lookup("Object");

            Should.NotThrow(() =>
            {
                result.Update(new RootModel());
                result.Commit();
            });
        }

        public void Test_Update_ObjectGraph()
        {
            var store = DatabaseHelper.Default.Store;
            var model = new RootModel()
            {
                Complex = new RootModel.SubModel()
                {
                    Value = 50,
                    Complex = new RootModel.SubModel2()
                    {
                        Value = "String 1",
                    },
                },
                ComplexList = new List<RootModel.SubModel>()
                {
                    new RootModel.SubModel() { Complex = new RootModel.SubModel2() { Value = "String 2" } },
                    new RootModel.SubModel() { Complex = new RootModel.SubModel2() { Value = "String 3" } },
                },
            };

            var rel1 = store.From(model).Select("Complex.Value", "Complex.Complex.Value");
            var rel2 = store.From(model).Select("ComplexList.Item.Complex.Value");

            var tuple1 = rel1.Row();
            var tuple2 = rel2.Column().ToArray();

            tuple1[0].Update(100); tuple1[0].Commit();
            tuple1[1].Update("String 3"); tuple1[1].Commit();
            tuple2[0].Update("String 4"); tuple2[0].Commit();
            tuple2[1].Update("String 5"); tuple2[1].Commit();

            model.Complex.Value.ShouldBe(100);
            model.Complex.Complex.Value.ShouldBe("String 3");
            model.ComplexList[0].Complex.Value.ShouldBe("String 4");
            model.ComplexList[1].Complex.Value.ShouldBe("String 5");
        }


        public void Test_Select_UnknownProperty_Throws()
        {
            var model = new RootModel();
            var store = DatabaseHelper.Default.Store;

            Should.Throw<MetadataException>(() => store.From(model).Select("Unknown123"));
        }

        public void Test_Select_SourceTraverse()
        {
            var store = DatabaseHelper.Default.Store;
            var model = new DeepModel()
            {
                Sub1 = new DeepModel.SubModel1()
                {
                    Sub2 = new DeepModel.SubModel2()
                    {
                        Sub3 = new List<DeepModel.SubModel3>()
                        {
                            new DeepModel.SubModel3()
                            {
                                Sub4 = new DeepModel.SubModel4()
                                {
                                    Sub5 = new List<DeepModel.SubModel5>()
                                    {
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 1 },
                                                new DeepModel.SubModel6() { Value = 2 },
                                                new DeepModel.SubModel6() { Value = 3 },
                                            },
                                        },
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 4 },
                                                new DeepModel.SubModel6() { Value = 5 },
                                            },
                                        },
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 6 },
                                            },
                                        }
                                    },
                                },
                            },
                            new DeepModel.SubModel3()
                            {
                                Sub4 = new DeepModel.SubModel4()
                                {
                                    Sub5 = new List<DeepModel.SubModel5>()
                                    {
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 7 },
                                                new DeepModel.SubModel6() { Value = 8 },
                                                null,
                                            },
                                        },
                                        new DeepModel.SubModel5()
                                        {
                                            Sub6 = new List<DeepModel.SubModel6>()
                                            {
                                                new DeepModel.SubModel6() { Value = 9 },
                                            },
                                        },
                                    },
                                },
                            },
                            new DeepModel.SubModel3()
                            {
                                Sub4 = new DeepModel.SubModel4()
                                {
                                    Sub5 = new List<DeepModel.SubModel5>(),
                                },
                            },
                        },
                    },
                },
            };

            var valueAttr = "Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item.Value";

            var rel1 = store.From(model).Select(valueAttr);
            var rel2 = rel1.Source.Lookup("Sub1").Select(valueAttr);
            var rel3 = rel2.Source.Lookup("Sub1.Sub2").Select(valueAttr);
            var rel4 = rel3.Source.Lookup("Sub1.Sub2.Sub3").Select(valueAttr);
            var rel5 = rel4.Source.Lookup("Sub1.Sub2.Sub3.Item").Select(valueAttr);
            var rel6 = rel5.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4").Select(valueAttr);
            var rel7 = rel6.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5").Select(valueAttr);
            var rel8 = rel7.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item").Select(valueAttr);
            var rel9 = rel8.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6").Select(valueAttr);
            var rel10 = rel9.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item").Select(valueAttr);
            var rel11 = rel10.Source.Lookup("Sub1.Sub2.Sub3.Item.Sub4.Sub5.Item.Sub6.Item.Value").Select(valueAttr);

            rel1.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel2.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel3.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel4.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6, 7, 8, null, 9 });
            rel5.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6 });
            rel6.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6 });
            rel7.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3, 4, 5, 6 });
            rel8.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3 });
            rel9.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1, 2, 3 });
            rel10.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1 });
            rel11.Column().Select(f => (int?)f.Snapshot).ShouldBe(new int?[] { 1 });
        }

        public void Test_Select_OneToOne_OuterJoin()
        {
            var store = DatabaseHelper.Default.Store;
            var model = new RootModel() { IntValue = 1 };
            var tuple = store.From(model).Select("IntValue", "Complex.Complex.Value").Row();

            tuple.Degree.ShouldBe(2);

            tuple[0].Snapshot.ShouldBe(1);
            tuple[1].Snapshot.ShouldBeNull();
        }

        public void Test_Select_OneToMany_InnerJoin()
        {
            var store = DatabaseHelper.Default.Store;
            var model = new List<RootModel>()
            {
                new RootModel()
                {
                    IntValue = 1,
                    ComplexList = new List<RootModel.SubModel>()
                    {
                        new RootModel.SubModel() { Value = 10 }
                    }
                },
                new RootModel()
                {
                    IntValue = 2,
                    ComplexList = new List<RootModel.SubModel>()
                    {
                        new RootModel.SubModel() { Value = 11 },
                        new RootModel.SubModel() { Value = 12 }
                    }
                },
                new RootModel() { IntValue = 3 },
                new RootModel() { IntValue = 4, ComplexList = new List<RootModel.SubModel>() },
            };

            var result = store.From(model).Select("Item.IntValue", "Item.ComplexList.Item.Value").Body.ToArray();

            result.Length.ShouldBe(3);

            result[0].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 10 });
            result[0].Select(f => f.Identity.Name).ShouldBe(new[] { "Item[0].IntValue", "Item[0].ComplexList.Item[0].Value" });

            result[1].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 11 });
            result[1].Select(f => f.Identity.Name).ShouldBe(new[] { "Item[1].IntValue", "Item[1].ComplexList.Item[0].Value" });

            result[2].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 12 });
            result[2].Select(f => f.Identity.Name).ShouldBe(new[] { "Item[1].IntValue", "Item[1].ComplexList.Item[1].Value" });
        }


        public void Test_Select_Duplicates()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new { Value = 10 };

            var result = store.From(data).Lookup("Value", "Value", "Value");

            result.ShouldNotBeNull();
            result.Degree.ShouldBe(3);
            result[0].ShouldBeSameAs(result[1]);
            result[1].ShouldBeSameAs(result[2]);
        }

        public void Test_Select_ScalarList()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel()
            {
                IntList = new List<int>() { 1, 2, 3, 4, 5 },
            };

            var result = store.From(data).Select("IntList.Item").Column();

            result.Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 2, 3, 4, 5 });
            result.Select(f => f.Identity.Name).ShouldBe(new[] { "IntList.Item[0]", "IntList.Item[1]", "IntList.Item[2]", "IntList.Item[3]", "IntList.Item[4]" });
        }

        public void Test_Select_NotReachable_Throws()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new RootModel()
            {
                IntValue = 100,
                IntList = new List<int>(),
            };
            var nonParent = store.From(data).Lookup("IntValue");
            var noReach = Should.NotThrow(() => nonParent.Select("IntList"));

            Should.Throw<RelationException>(() => noReach.Scalar());
        }

        public void Test_Select_Adjacent_CrossJoin()
        {
            var store = DatabaseHelper.Default.GetStore();
            var data = new CrossModel()
            {
                Xs = new List<int>() { 1, 2, 3, 4 },
                Ys = new List<int>() { 5, 6 },
                Zs = new List<int>() { 7, 8, 9 },
            };

            var rel1 = store.From(data).Select("Xs.Item", "Ys.Item", "Zs.Item");
            var rel2 = store.From(data).Select("Zs.Item", "Ys.Item", "Xs.Item");

            var data1 = rel1.Body.ToArray();
            var data2 = rel2.Body.ToArray();

            data1.Length.ShouldBe(4 * 2 * 3);

            data1[0].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[0]", "Ys.Item[0]", "Zs.Item[0]" });
            data1[1].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[0]", "Ys.Item[0]", "Zs.Item[1]" });
            data1[2].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[0]", "Ys.Item[0]", "Zs.Item[2]" });
            data1[3].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[0]", "Ys.Item[1]", "Zs.Item[0]" });
            data1[4].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[0]", "Ys.Item[1]", "Zs.Item[1]" });
            data1[5].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[0]", "Ys.Item[1]", "Zs.Item[2]" });

            data1[6].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[1]", "Ys.Item[0]", "Zs.Item[0]" });
            data1[7].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[1]", "Ys.Item[0]", "Zs.Item[1]" });
            data1[8].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[1]", "Ys.Item[0]", "Zs.Item[2]" });
            data1[9].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[1]", "Ys.Item[1]", "Zs.Item[0]" });
            data1[10].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[1]", "Ys.Item[1]", "Zs.Item[1]" });
            data1[11].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[1]", "Ys.Item[1]", "Zs.Item[2]" });

            data1[12].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[2]", "Ys.Item[0]", "Zs.Item[0]" });
            data1[13].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[2]", "Ys.Item[0]", "Zs.Item[1]" });
            data1[14].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[2]", "Ys.Item[0]", "Zs.Item[2]" });
            data1[15].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[2]", "Ys.Item[1]", "Zs.Item[0]" });
            data1[16].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[2]", "Ys.Item[1]", "Zs.Item[1]" });
            data1[17].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[2]", "Ys.Item[1]", "Zs.Item[2]" });

            data1[18].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[3]", "Ys.Item[0]", "Zs.Item[0]" });
            data1[19].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[3]", "Ys.Item[0]", "Zs.Item[1]" });
            data1[20].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[3]", "Ys.Item[0]", "Zs.Item[2]" });
            data1[21].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[3]", "Ys.Item[1]", "Zs.Item[0]" });
            data1[22].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[3]", "Ys.Item[1]", "Zs.Item[1]" });
            data1[23].Select(f => f.Identity.Name).ShouldBe(new[] { "Xs.Item[3]", "Ys.Item[1]", "Zs.Item[2]" });

            data1[0].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 5, 7 });
            data1[1].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 5, 8 });
            data1[2].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 5, 9 });
            data1[3].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 6, 7 });
            data1[4].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 6, 8 });
            data1[5].Select(f => (int)f.Snapshot).ShouldBe(new[] { 1, 6, 9 });

            data1[6].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 5, 7 });
            data1[7].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 5, 8 });
            data1[8].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 5, 9 });
            data1[9].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 6, 7 });
            data1[10].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 6, 8 });
            data1[11].Select(f => (int)f.Snapshot).ShouldBe(new[] { 2, 6, 9 });

            data1[12].Select(f => (int)f.Snapshot).ShouldBe(new[] { 3, 5, 7 });
            data1[13].Select(f => (int)f.Snapshot).ShouldBe(new[] { 3, 5, 8 });
            data1[14].Select(f => (int)f.Snapshot).ShouldBe(new[] { 3, 5, 9 });
            data1[15].Select(f => (int)f.Snapshot).ShouldBe(new[] { 3, 6, 7 });
            data1[16].Select(f => (int)f.Snapshot).ShouldBe(new[] { 3, 6, 8 });
            data1[17].Select(f => (int)f.Snapshot).ShouldBe(new[] { 3, 6, 9 });

            data1[18].Select(f => (int)f.Snapshot).ShouldBe(new[] { 4, 5, 7 });
            data1[19].Select(f => (int)f.Snapshot).ShouldBe(new[] { 4, 5, 8 });
            data1[20].Select(f => (int)f.Snapshot).ShouldBe(new[] { 4, 5, 9 });
            data1[21].Select(f => (int)f.Snapshot).ShouldBe(new[] { 4, 6, 7 });
            data1[22].Select(f => (int)f.Snapshot).ShouldBe(new[] { 4, 6, 8 });
            data1[23].Select(f => (int)f.Snapshot).ShouldBe(new[] { 4, 6, 9 });

            data2.Length.ShouldBe(3 * 2 * 4);

            data2[0].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[0]", "Xs.Item[0]" });
            data2[1].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[0]", "Xs.Item[1]" });
            data2[2].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[0]", "Xs.Item[2]" });
            data2[3].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[0]", "Xs.Item[3]" });

            data2[4].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[1]", "Xs.Item[0]" });
            data2[5].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[1]", "Xs.Item[1]" });
            data2[6].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[1]", "Xs.Item[2]" });
            data2[7].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[0]", "Ys.Item[1]", "Xs.Item[3]" });

            data2[8].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[0]", "Xs.Item[0]" });
            data2[9].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[0]", "Xs.Item[1]" });
            data2[10].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[0]", "Xs.Item[2]" });
            data2[11].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[0]", "Xs.Item[3]" });

            data2[12].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[1]", "Xs.Item[0]" });
            data2[13].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[1]", "Xs.Item[1]" });
            data2[14].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[1]", "Xs.Item[2]" });
            data2[15].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[1]", "Ys.Item[1]", "Xs.Item[3]" });

            data2[16].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[0]", "Xs.Item[0]" });
            data2[17].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[0]", "Xs.Item[1]" });
            data2[18].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[0]", "Xs.Item[2]" });
            data2[19].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[0]", "Xs.Item[3]" });

            data2[20].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[1]", "Xs.Item[0]" });
            data2[21].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[1]", "Xs.Item[1]" });
            data2[22].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[1]", "Xs.Item[2]" });
            data2[23].Select(f => f.Identity.Name).ShouldBe(new[] { "Zs.Item[2]", "Ys.Item[1]", "Xs.Item[3]" });

            data2[0].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 5, 1 });
            data2[1].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 5, 2 });
            data2[2].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 5, 3 });
            data2[3].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 5, 4 });

            data2[4].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 6, 1 });
            data2[5].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 6, 2 });
            data2[6].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 6, 3 });
            data2[7].Select(f => (int)f.Snapshot).ShouldBe(new[] { 7, 6, 4 });

            data2[8].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 5, 1 });
            data2[9].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 5, 2 });
            data2[10].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 5, 3 });
            data2[11].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 5, 4 });

            data2[12].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 6, 1 });
            data2[13].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 6, 2 });
            data2[14].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 6, 3 });
            data2[15].Select(f => (int)f.Snapshot).ShouldBe(new[] { 8, 6, 4 });

            data2[16].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 5, 1 });
            data2[17].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 5, 2 });
            data2[18].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 5, 3 });
            data2[19].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 5, 4 });

            data2[20].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 6, 1 });
            data2[21].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 6, 2 });
            data2[22].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 6, 3 });
            data2[23].Select(f => (int)f.Snapshot).ShouldBe(new[] { 9, 6, 4 });
        }

        public void Test_Select_Adjacent_CrossJoin_Cache()
        {
            var store = DatabaseHelper.Default.GetStore();
            var data = new CrossModel()
            {
                Xs = new List<int>() { 1, 2 },
                Ys = new List<int>() { 5, 6 },
            };

            var relation = store.From(data).Select("Xs.Item", "Ys.Item");
            var result = relation.Body.ToArray();

            result.Length.ShouldBe(2 * 2);

            result[0][0].ShouldBeSameAs(result[1][0]);
            result[2][0].ShouldBeSameAs(result[3][0]);

            result[0][1].ShouldBeSameAs(result[2][1]);
            result[1][1].ShouldBeSameAs(result[3][1]);
        }

        public void Test_Select_Recursive()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema<List<RecursiveModel>>();
            var model = new List<RecursiveModel>()
            {
                new RecursiveModel()
                {
                    Name = "1",
                    Subs = new List<RecursiveModel>()
                    {
                        new RecursiveModel()
                        {
                            Name = "1.1",
                            Subs = new List<RecursiveModel>()
                            {
                                new RecursiveModel()
                                {
                                    Name = "1.1.1",
                                    Subs = new List<RecursiveModel>()
                                    {
                                        new RecursiveModel() { Name = "1.1.1.1" },
                                        new RecursiveModel() { Name = "1.1.1.2" },
                                        new RecursiveModel()
                                        {
                                            Name = "1.1.1.3",
                                            Subs = new List<RecursiveModel>()
                                            {
                                                new RecursiveModel() { Name = "1.1.1.3.1" },
                                            }
                                        },
                                        new RecursiveModel() { Name = "1.1.1.4" },
                                    }
                                },
                                new RecursiveModel()
                                {
                                    Name = "1.1.2",
                                    Subs = new List<RecursiveModel>()
                                    {
                                        new RecursiveModel() { Name = "1.1.2.1" },
                                        new RecursiveModel() { Name = "1.1.2.2" },
                                    }
                                },
                                new RecursiveModel()
                                {
                                    Name = "1.1.3",
                                }
                            }
                        }
                    }
                },
                new RecursiveModel()
                {
                    Name = "2",
                    Subs = new List<RecursiveModel>()
                    {
                        new RecursiveModel()
                        {
                            Name = "2.1",
                            Subs = new List<RecursiveModel>()
                            {
                                new RecursiveModel() { Name = "2.1.1" },
                                new RecursiveModel() { Name = "2.1.2" },
                                new RecursiveModel()
                                {
                                    Name = "2.1.3",
                                    Subs = new List<RecursiveModel>()
                                    {
                                        new RecursiveModel() { Name = "2.1.3.1" },
                                        new RecursiveModel() { Name = "2.1.3.2" },
                                        new RecursiveModel() { Name = "2.1.3.3" },
                                    }
                                }
                            }
                        },
                        new RecursiveModel()
                        {
                            Name = "2.2",
                            Subs = new List<RecursiveModel>()
                            {
                                new RecursiveModel() { Name = "2.2.1" },
                                new RecursiveModel() { Name = "2.2.2" },
                            }
                        }
                    }
                },
                new RecursiveModel() { Name = "3" },
                new RecursiveModel() { Name = "4" },
            };

            var rel1 = store.From(model).Select("Item.Name");
            var rel2 = store.From(model).Select("Item.Subs.Item.Name");

            var names1 = rel1.Body.Select(t => t[0].Identity.Name).ToList();
            var values1 = rel1.Body.Select(t => (string)t[0].Snapshot).ToList();

            var names2 = rel2.Body.Select(t => t[0].Identity.Name).ToList();
            var values2 = rel2.Body.Select(t => (string)t[0].Snapshot).ToList();

            names1.ShouldBe(new[] { "Item[0].Name", "Item[1].Name", "Item[2].Name", "Item[3].Name" });
            values1.ShouldBe(new[] { "1", "2", "3", "4" });

            names2.ShouldBe(new[] {  "Item[0].Subs.Item[0].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[0].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[1].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[2].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[0].Subs.Item[0].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[0].Subs.Item[1].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[0].Subs.Item[2].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[0].Subs.Item[3].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[1].Subs.Item[0].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[1].Subs.Item[1].Name",
                                     "Item[0].Subs.Item[0].Subs.Item[0].Subs.Item[2].Subs.Item[0].Name",
                                     "Item[1].Subs.Item[0].Name",
                                     "Item[1].Subs.Item[1].Name",
                                     "Item[1].Subs.Item[0].Subs.Item[0].Name",
                                     "Item[1].Subs.Item[0].Subs.Item[1].Name",
                                     "Item[1].Subs.Item[0].Subs.Item[2].Name",
                                     "Item[1].Subs.Item[1].Subs.Item[0].Name",
                                     "Item[1].Subs.Item[1].Subs.Item[1].Name",
                                     "Item[1].Subs.Item[0].Subs.Item[2].Subs.Item[0].Name",
                                     "Item[1].Subs.Item[0].Subs.Item[2].Subs.Item[1].Name",
                                     "Item[1].Subs.Item[0].Subs.Item[2].Subs.Item[2].Name",
            });
            values2.ShouldBe(new[] { "1.1",
                                       "1.1.1", "1.1.2", "1.1.3",
                                         "1.1.1.1", "1.1.1.2", "1.1.1.3", "1.1.1.4", "1.1.2.1", "1.1.2.2",
                                           "1.1.1.3.1",
                                     "2.1", "2.2",
                                       "2.1.1", "2.1.2", "2.1.3", "2.2.1", "2.2.2",
                                         "2.1.3.1", "2.1.3.2", "2.1.3.3"
            });
        }
    }
}
