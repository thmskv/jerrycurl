using System.Threading.Tasks;
using Shouldly;
using Jerrycurl.Test;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Relations.Language;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata;
using System.Linq;
using System;
using Jerrycurl.Relations.Metadata;
using System.Drawing;
using Jerrycurl.Cqs.Test.Metadata;
using Jerrycurl.Test.Extensions;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Test.Models.Views;
using Jerrycurl.Cqs.Test.Models.Custom;
using Jerrycurl.Test.Models.Database;
using Jerrycurl.Cqs.Test.Models;

namespace Jerrycurl.Cqs.Test
{
    public class QueryTests
    {
        public void Test_Insert_ManyToMany()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<BlogTagView>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            var data1 = new[] { 1, 2, 3, 4 };
            var data2 = new[] { 11, 12, 13, 14 };
            var data3 = new (int, int)[]
            {
                (1, 11),
                (2, 11),
                (3, 13),
            };

            buffer.Insert(data1, ("Item", "Item.Post.Item.Id"));
            buffer.Insert(data2, ("Item", "Item.Tag.Item.Id"));
            buffer.Insert(data3,
                ("Item.Item1", "Item.BlogPostId"),
                ("Item.Item2", "Item.BlogTagId")
            );

            var result = buffer.Commit<IList<BlogTagView>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(3);

            result[0].ShouldNotBeNull();
            result[0].BlogPostId.ShouldBe(1);
            result[0].BlogTagId.ShouldBe(11);
            result[0].Post.ShouldNotBeNull();
            result[0].Post.HasValue.ShouldBeTrue();
            result[0].Post.Value.Id.ShouldBe(1);
            result[0].Tag.ShouldNotBeNull();
            result[0].Tag.HasValue.ShouldBeTrue();
            result[0].Tag.Value.Id.ShouldBe(11);

            result[1].ShouldNotBeNull();
            result[1].BlogPostId.ShouldBe(2);
            result[1].BlogTagId.ShouldBe(11);
            result[1].Post.ShouldNotBeNull();
            result[1].Post.HasValue.ShouldBeTrue();
            result[1].Post.Value.Id.ShouldBe(2);
            result[1].Tag.ShouldNotBeNull();
            result[1].Tag.HasValue.ShouldBeTrue();
            result[1].Tag.Value.Id.ShouldBe(11);
            result[1].Tag.ShouldBeSameAs(result[0].Tag);

            result[2].ShouldNotBeNull();
            result[2].BlogPostId.ShouldBe(3);
            result[2].BlogTagId.ShouldBe(13);
            result[2].Post.ShouldNotBeNull();
            result[2].Post.HasValue.ShouldBeTrue();
            result[2].Post.Value.Id.ShouldBe(3);
            result[2].Tag.ShouldNotBeNull();
            result[2].Tag.HasValue.ShouldBeTrue();
            result[2].Tag.Value.Id.ShouldBe(13);
        }

        public void Test_Empty_ObjectAndList()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(IList<Blog>));

            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);

            var result1 = buffer1.Commit<Blog>();
            var result2 = buffer2.Commit<IList<Blog>>();

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        public void Test_Insert_NullableKeys()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<KeysModel>));
            var buffer1 = new QueryBuffer(schema, QueryType.List);
            var buffer2 = new QueryBuffer(schema, QueryType.List);

            // primary
            buffer1.Insert(1, ("", "Item.Id1"));
            buffer1.Insert((int?)null, ("", "Item.Id1"));

            buffer1.Insert(1, ("", "Item.Many1.Item.KeyId"));
            buffer1.Insert((int?)null, ("", "Item.Many1.Item.KeyId"));

            // non-primary
            buffer2.Insert(2, ("", "Item.Id2"));
            buffer2.Insert((int?)null, ("", "Item.Id2"));

            buffer2.Insert(2, ("", "Item.Many2.Item.KeyId"));
            buffer2.Insert((int?)null, ("", "Item.Many2.Item.KeyId"));

            var result1 = buffer1.Commit<IList<KeysModel>>();
            var result2 = buffer2.Commit<IList<KeysModel>>();

            result1.Count.ShouldBe(1);
            result1[0].Id1.ShouldBe(1);
            result1[0].Many1.ShouldNotBeNull();
            result1[0].Many1.Count.ShouldBe(1);
            result1[0].Many1[0].KeyId.ShouldBe(1);

            result2.Count.ShouldBe(2);
            result2[0].Id2.ShouldBe(2);
            result2[0].Many2.ShouldNotBeNull();
            result2[0].Many2.Count.ShouldBe(1);
            result2[0].Many2[0].KeyId.ShouldBe(2);
            result2[1].Id2.ShouldBeNull();
            result2[1].Many2.ShouldNotBeNull();
            result2[1].Many2.Count.ShouldBe(0);
        }

        public void Test_Read_NullableSet()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new int?[] { 1, 2, null };

            using var dataReader = store.From(data).Select("Item").As("Item");

            var reader = new QueryReader(store, dataReader);
            var result = reader.Read<int?>();

            result.ShouldBe(new int?[] { 1, 2, null });
        }

        public void Test_Read_IntegerSet()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new[] { 1, 2, 3, 4, 5, 6 };

            using var dataReader = store.From(data).Select("Item").As("Item");

            var reader = new QueryReader(store, dataReader);
            var result = reader.Read<int>();

            result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
        }

        public void Test_Insert_ManyToOne_List()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new (int, int)[]
            {
                // BlogPost(Id, BlogId)
                (1, 1),
                (2, 1),
                (3, 2),
                (4, 4),
            };
            var data2 = new[] { 1, 2, 3 }; // Blog(Id)

            var schema = store.GetSchema(typeof(List<BlogPostView>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.BlogId")
            );

            buffer.Insert(data2,
                ("Item", "Item.Blog2.Item.Id")
            );

            var result = buffer.Commit<List<BlogPostView>>();

            result.Count.ShouldBe(4);

            result[0].Blog2.ShouldNotBeNull();
            result[0].Blog2.HasValue.ShouldBeTrue();
            result[0].Blog2.Value.Id.ShouldBe(1);
            result[0].Blog2.ShouldBeSameAs(result[1].Blog2);

            result[2].Blog2.ShouldNotBeNull();
            result[2].Blog2.HasValue.ShouldBeTrue();
            result[2].Blog2.Value.Id.ShouldBe(2);

            result[3].Blog2.ShouldNotBeNull();
            result[3].Blog2.HasValue.ShouldBeFalse();
        }

        public void Test_Insert_ManyToOne_Object()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new[] { 2 }; // Blog(Id)
            var data2 = new (int, int)[]
            {
                // BlogPost(Id, BlogId)
                (2, 1),
                (1, 2),
            };
            var data3 = new[] { 1 }; // Blog(Id)

            var schema = store.GetSchema(typeof(List<BlogPostView>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data1,
                ("Item", "Item.Blog1.Id")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.BlogId")
            );

            buffer.Insert(data3,
                ("Item", "Item.Blog1.Id")
            );

            var result = buffer.Commit<List<BlogPostView>>();

            result.Count.ShouldBe(2);

            result[0].Blog1.ShouldBeNull();

            result[1].Blog1.ShouldNotBeNull();
            result[1].Blog1.Id.ShouldBe(2);
        }


        public void Test_Aggregate_ListTypes()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(ListTypeModel));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert(42, ("", "IList.Item"));
            buffer.Insert(43, ("", "List.Item"));
            buffer.Insert(44, ("", "IReadOnlyList.Item"));
            buffer.Insert(45, ("", "IEnumerable.Item"));
            buffer.Insert(46, ("", "One.Item"));
            buffer.Insert(47, ("", "IReadOnlyCollection.Item"));

            var result = buffer.Commit<ListTypeModel>();

            result.ShouldNotBeNull();

            result.IList.ShouldNotBeNull();
            result.IList.ShouldBeOfType<List<int>>();
            result.IList.Count.ShouldBe(1);
            result.IList[0].ShouldBe(42);

            result.List.ShouldNotBeNull();
            result.List.ShouldBeOfType<List<int>>();
            result.List.Count.ShouldBe(1);
            result.List[0].ShouldBe(43);

            result.IReadOnlyList.ShouldNotBeNull();
            result.IReadOnlyList.ShouldBeOfType<List<int>>();
            result.IReadOnlyList.Count.ShouldBe(1);
            result.IReadOnlyList[0].ShouldBe(44);

            result.IEnumerable.ShouldNotBeNull();
            result.IEnumerable.ShouldBeOfType<List<int>>();
            result.IEnumerable.Count().ShouldBe(1);
            result.IEnumerable.First().ShouldBe(45);

            result.One.ShouldNotBeNull();
            result.One.HasValue.ShouldBeTrue();
            result.One.Value.ShouldBe(46);

            result.IReadOnlyCollection.ShouldNotBeNull();
            result.IReadOnlyCollection.ShouldBeOfType<List<int>>();
            result.IReadOnlyCollection.Count.ShouldBe(1);
            result.IReadOnlyCollection.First().ShouldBe(47);
        }
        public void Test_InsertAndAggregate_AllTypes()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(List<BlogDatabaseView>));
            var schema2 = store.GetSchema(typeof(BlogDatabaseModel));
            var data = new BlogDatabaseModel()
            {
                Categories = new List<BlogCategory>()
                {
                    new BlogCategory()
                    {
                        Id = 1,
                        Name = "Plants",
                    },
                    new BlogCategory()
                    {
                        Id = 2,
                        Name = "Birds",
                    }
                },
                Blogs = new List<BlogDatabaseModel.BlogView>()
                {
                    new BlogDatabaseModel.BlogView()
                    {
                        Id = 1,
                        Title = "Blog #1",
                        CategoryId = 2,
                    },
                    new BlogDatabaseModel.BlogView()
                    {
                        Id = 2,
                        Title = "Blog #2",
                        CategoryId = 2,
                        Author = new BlogAuthor()
                        {
                            BlogId = 2,
                            Name = "Blogger #2",
                        }
                    },
                    new BlogDatabaseModel.BlogView()
                    {
                        Id = 3,
                        Title = "Blog #3",
                        CategoryId = 1,
                    }
                },
                Posts = new List<BlogPost>()
                {
                    new BlogPost()
                    {
                        Id = 1,
                        BlogId = 1,
                        CreatedOn = new DateTime(2000, 1, 1),
                        Headline = "Blog post #1.1",
                        Content = "So...",
                    },
                    new BlogPost()
                    {
                        Id = 2,
                        BlogId = 1,
                        CreatedOn = new DateTime(2000, 1, 1),
                        Headline = "Blog post #1.2",
                        Content = "...and then"
                    },
                    new BlogPost()
                    {
                        Id = 3,
                        BlogId = 3,
                        CreatedOn = new DateTime(2000, 1, 1),
                        Headline = "Blog post #3.1",
                        Content = "Well...",
                    },
                },
            };

            
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.Aggregate);

            var categories = store.For<BlogDatabaseModel>()
                .Join(m => m.Categories)
                .SelectAll()
                .From(data);

            var blogs = store.For<BlogDatabaseModel>()
                .Join(m => m.Blogs)
                .Select(m => m.Id)
                .Select(m => m.Title)
                .Select(m => m.CategoryId)
                .SelectAll(m => m.Author)
                .From(data);

            var posts = store.For<BlogDatabaseModel>()
                .Join(m => m.Posts)
                .SelectAll()
                .From(data);

            buffer1.Insert(categories, "Item.Category.Item.Id",
                                       "Item.Category.Item.ParentId",
                                       "Item.Category.Item.Name");

            buffer1.Insert(blogs, "Item.Id",
                                  "Item.Title",
                                  "Item.CategoryId",
                                  "Item.Author.BlogId",
                                  "Item.Author.Name",
                                  "Item.Author.TwitterUrl");

            buffer1.Insert(posts, "Item.Posts.Item.Id",
                                  "Item.Posts.Item.BlogId",
                                  "Item.Posts.Item.CreatedOn",
                                  "Item.Posts.Item.Headline",
                                  "Item.Posts.Item.Content");

            buffer2.Insert(categories);
            buffer2.Insert(blogs);
            buffer2.Insert(posts);

            var result1 = buffer1.Commit<List<BlogDatabaseView>>();
            var result2 = buffer2.Commit<BlogDatabaseModel>();

            var blogsLeft = result1.Cast<Blog>();
            var blogsRight = data.Blogs.Cast<Blog>();

            var authorsLeft = result1.Select(b => b.Author);
            var authorsRight = data.Blogs.Select(b => b.Author);

            var postsLeft = result1.SelectMany(b => b.Posts);
            var postsRight = data.Posts;

            var categoriesLeft = result1.Where(b => b.Category.HasValue).Select(b => b.Category.Value).OrderBy(c => c.Id).Distinct();
            var categoriesRight = data.Categories;

            blogsLeft.ShouldBeSameAsJson(blogsRight);
            authorsLeft.ShouldBeSameAsJson(authorsRight);
            postsLeft.ShouldBeSameAsJson(postsRight);
            categoriesLeft.ShouldBeSameAsJson(categoriesRight);
        }

        public void Test_Insert_DualRecursiveTree()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<BlogCategoryView>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            // BlogCategory(Id, ParentId)
            var data1 = new (int, int?)[] // Parents
            {
                (1, null), (2, 0), (3, 1), (4, 1),
            };

            var data2 = new (int, int?)[]
            {
                (5, 2), (6, 3)
            };

            var data3 = new (int, int?)[] // Children
            {
                (7, 6), (8, 6), (9, 8), (10, 9),
            };

            buffer.Insert(data1,
                ("Item.Item1", "Item.Parent.Item.Id"),
                ("Item.Item2", "Item.Parent.Item.ParentId")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.ParentId")
            );

            buffer.Insert(data3,
                ("Item.Item1", "Item.Children.Item.Id"),
                ("Item.Item2", "Item.Children.Item.ParentId")
            );

            var result = buffer.Commit<IList<BlogCategoryView>>();

            result.Count.ShouldBe(2);
            result[0].Id.ShouldBe(5);
            result[0].ParentId.ShouldBe(2);
            result[0].Parent.ShouldNotBeNull();
            result[0].Parent.HasValue.ShouldBeTrue();

            result[0].Parent.HasValue.ShouldBeTrue();
            result[0].Parent.Value.Id.ShouldBe(2);
            result[0].Parent.Value.ParentId.ShouldBe(0);
            result[0].Parent.Value.Parent.ShouldNotBeNull();
            result[0].Parent.Value.Parent.HasValue.ShouldBeFalse();

            result[1].Id.ShouldBe(6);
            result[1].ParentId.ShouldBe(3);

            result[1].Parent.HasValue.ShouldBeTrue();
            result[1].Parent.Value.Id.ShouldBe(3);
            result[1].Parent.Value.ParentId.ShouldBe(1);

            result[1].Parent.Value.Parent.HasValue.ShouldBeTrue();
            result[1].Parent.Value.Parent.Value.Id.ShouldBe(1);
            result[1].Parent.Value.Parent.Value.ParentId.ShouldBeNull();
            result[1].Parent.Value.Parent.Value.Parent.ShouldNotBeNull();
            result[1].Parent.Value.Parent.Value.Parent.HasValue.ShouldBeFalse();

            result[1].Children.ShouldNotBeNull();
            result[1].Children.Count.ShouldBe(2);
            result[1].Children.Select(c => c.Id).ShouldBe(new[] { 7, 8 });

            result[1].Children[0].Children.ShouldNotBeNull();
            result[1].Children[0].Children.Count.ShouldBe(0);

            result[1].Children[1].Children.ShouldNotBeNull();
            result[1].Children[1].Children.Count.ShouldBe(1);
            result[1].Children[1].Children[0].Id.ShouldBe(9);
            result[1].Children[1].Children[0].ParentId.ShouldBe(8);
            result[1].Children[1].Children[0].Children.ShouldNotBeNull();
            result[1].Children[1].Children[0].Children.Count.ShouldBe(1);
            result[1].Children[1].Children[0].Children[0].Id.ShouldBe(10);
            result[1].Children[1].Children[0].Children[0].ParentId.ShouldBe(9);
            result[1].Children[1].Children[0].Children[0].Children.ShouldNotBeNull();
            result[1].Children[1].Children[0].Children[0].Children.Count.ShouldBe(0);
        }

        public void Test_Aggregate_PrimaryKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert<object>(null, ("", "Id"));

            var result = buffer.Commit();

            result.ShouldBeNull();
        }

        public void Test_Aggregate_NonPrimaryKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(SecondaryModel));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert<object>(null, ("", "Id"));

            var result = buffer.Commit<SecondaryModel>();

            result.ShouldNotBeNull();
            result.Id.ShouldBe(0);
        }

        public void Test_Insert_One()
        {
            var store = DatabaseHelper.Default.Store;
            
            var data = new (int, string)[]
            {
                (1, "Hello World!"),
                (2, "Hello Universe!"),
            };

            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Id"),
                ("Item.Item2", "Title")
            );

            var result = buffer.Commit<Blog>();

            result.ShouldNotBeNull();
            result.Id.ShouldBe(2);
            result.Title.ShouldBe("Hello Universe!");
        }


        public void Test_Insert_NonMatching()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(List<Blog>));
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);

            buffer1.Insert(50, ("", "Foo"));
            buffer2.Insert(50, ("", "Item.Bar"));

            var result1 = buffer1.Commit();
            var result2 = buffer1.Commit();

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        public void Test_Aggregate_NonMatching()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(Blog));
            var schema2 = store.GetSchema(typeof(List<Blog>));
            var buffer1 = new QueryBuffer(schema1, QueryType.Aggregate);
            var buffer2 = new QueryBuffer(schema2, QueryType.Aggregate);

            buffer1.Insert(50, ("", "Foo"));
            buffer2.Insert(50, ("", "Item.Bar"));

            var result1 = buffer1.Commit();
            var result2 = buffer2.Commit();

            result1.ShouldBeNull();
            result2.ShouldBeNull();
        }

        public void Test_Insert_EmptySet()
        {
            var store = DatabaseHelper.Default.Store;
            var schema1 = store.GetSchema(typeof(SecondaryModel));
            var schema2 = store.GetSchema(typeof(List<SecondaryModel>));
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);
            var buffer3 = new QueryBuffer(schema1, QueryType.Aggregate);
            var buffer4 = new QueryBuffer(schema2, QueryType.Aggregate);

            var data = (50, new List<int>());
            var empty = store.From(data).Select("Item1", "Item2.Item");

            buffer1.Insert(empty, "Id", "Foo");
            buffer2.Insert(empty, "Item.Id", "Foo");
            buffer3.Insert(empty, "Id", "Foo");
            buffer4.Insert(empty, "Item.Id", "Foo");

            var result1 = buffer1.Commit<SecondaryModel>();
            var result2 = buffer2.Commit<List<SecondaryModel>>();
            var result3 = buffer3.Commit<SecondaryModel>();
            var result4 = buffer4.Commit<List<SecondaryModel>>();

            result1.ShouldBeNull();

            result2.ShouldNotBeNull();
            result2.Count.ShouldBe(0);

            result3.ShouldNotBeNull();
            result3.Id.ShouldBe(0);

            result4.ShouldNotBeNull();
            result4.Count.ShouldBe(1);
            result4[0].ShouldNotBeNull();
            result4[0].Id.ShouldBe(0);
        }


        public void Test_Insert_Struct()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Point));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert((10, 20),
                ("Item1", "X"),
                ("Item2", "Y")
            );

            var result = buffer.Commit<Point>();

            result.X.ShouldBe(10);
            result.Y.ShouldBe(20);
        }

        public void Test_Insert_Invalid_Constructor()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(NoConstructModel));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() =>
            {
                buffer.Insert("Hello World!", ("", "String"));
            });
        }

        public void Test_Insert_Invalid_ParentKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(InvalidModel));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.NotThrow(() =>
            {
                buffer.Insert(10, ("", "InvalidId"));
            });

            var result = buffer.Commit<InvalidModel>();

            result.ShouldNotBeNull();
            result.InvalidId.ShouldBe(10);
            result.Many.ShouldBeNull();
        }

        public void Test_Insert_OneToMany_Natural()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(List<NaturalModel>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(100, ("", "Item.NaturalId"));
            buffer.Insert(100, ("", "Item.Many.Item.NaturalId"));

            var result = buffer.Commit<List<NaturalModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].NaturalId.ShouldBe(100);
            result[0].Many.ShouldNotBeNull();
            result[0].Many.Count.ShouldBe(1);
            result[0].Many[0].NaturalId.ShouldBe(100);

        }

        public void Test_Insert_Priority_Result()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(List<int>));
            var buffer1 = new QueryBuffer(schema, QueryType.List);
            var buffer2 = new QueryBuffer(schema, QueryType.List);

            buffer1.Insert(11, ("", "Item"), ("", "Capacity"));
            buffer2.Insert(11, ("", "Capacity"), ("", "Item"));

            var result1 = buffer1.Commit<List<int>>();
            var result2 = buffer2.Commit<List<int>>();

            result1.ShouldNotBeNull();
            result1.Count.ShouldBe(1);
            result1[0].ShouldBe(11);
            result1.Capacity.ShouldNotBe(11);

            result2.ShouldNotBeNull();
            result2.Count.ShouldBe(1);
            result2[0].ShouldBe(11);
            result2.Capacity.ShouldNotBe(11);
        }

        public void Test_Insert_OneToMany_CustomList()
        {
            var store = DatabaseHelper.Default.GetStore(contracts: new[] { new CustomContractResolver() });
            var schema = store.GetSchema(typeof(List<PriorityModel>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(44, ("", "Item.Id1"));
            buffer.Insert(44, ("", "Item.Custom.Item.PriorityId1"));

            var result = buffer.Commit<List<PriorityModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Custom.ShouldNotBeNull();
            result[0].Custom.Count.ShouldBe(1);
            result[0].Custom[0].PriorityId1.ShouldBe(44);
        }

        public void Test_Insert_Priority_ManyToOne_Object()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(List<PriorityModel>));
            var buffer1 = new QueryBuffer(schema, QueryType.List);
            var buffer2 = new QueryBuffer(schema, QueryType.List);

            buffer1.Insert((34, 34), ("Item1", "Item.Id1"), ("Item2", "Item.One2.PriorityId1"));
            buffer2.Insert((34, 34), ("Item2", "Item.One2.PriorityId1"), ("Item1", "Item.Id1"));

            var result1 = buffer1.Commit<List<PriorityModel>>();
            var result2 = buffer2.Commit<List<PriorityModel>>();

            result1.ShouldNotBeNull();
            result1.Count.ShouldBe(1);
            result1[0].One2.ShouldNotBeNull();
            result1[0].One2.PriorityId1.ShouldBe(34);

            result2.ShouldNotBeNull();
            result2.Count.ShouldBe(1);
            result2[0].One2.ShouldNotBeNull();
            result2[0].One2.PriorityId1.ShouldBe(34);
        }

        public void Test_Insert_Priority_Value()
        {
            var store = DatabaseHelper.Default.GetStore(contracts: new[] { new CustomContractResolver() });
            var schema = store.GetSchema(typeof(List<PriorityModel>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert((44, 43), ("Item1", "Item.Id1"), ("Item2", "Item.Custom"));
            buffer.Insert(44, ("", "Item.Custom.Item.PriorityId1"));

            var result = buffer.Commit<List<PriorityModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Custom.ShouldNotBeNull();
            result[0].Custom.Count.ShouldBe(1);
            result[0].Custom[0].ShouldBeNull();
        }

        public void Test_Insert_Priority_Keys()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(List<PriorityModel>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert((200, 100), ("Item1", "Item.Id2"), ("Item2", "Item.Id1"));
            buffer.Insert(100, ("", "Item.Many.Item.PriorityId1")); // one-to-many, prefers PK -> FK
            buffer.Insert(200, ("", "Item.Many.Item.PriorityId2"));
            buffer.Insert(100, ("", "Item.One.Item.PriorityId1")); // many-to-one, prefers FK -> PK
            buffer.Insert(200, ("", "Item.One.Item.PriorityId2"));

            var result = buffer.Commit<List<PriorityModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Id1.ShouldBe(100);
            result[0].Id2.ShouldBe(200);
            result[0].Many.ShouldNotBeNull();
            result[0].Many.Count.ShouldBe(1);
            result[0].Many[0].PriorityId1.ShouldBe(100);
            result[0].Many[0].PriorityId2.ShouldBe(0);
            result[0].One.ShouldNotBeNull();
            result[0].One.HasValue.ShouldBeTrue();
            result[0].One.Value.PriorityId1.ShouldBe(0);
            result[0].One.Value.PriorityId2.ShouldBe(200);
        }

        public void Test_Aggregate_Result_Priority()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(List<int>));
            var buffer1 = new QueryBuffer(schema, QueryType.Aggregate);
            var buffer2 = new QueryBuffer(schema, QueryType.Aggregate);

            buffer1.Insert(11, ("", "Item"), ("", "Capacity"));
            buffer2.Insert(11, ("", "Capacity"), ("", "Item"));

            var result1 = buffer1.Commit<List<int>>();
            var result2 = buffer2.Commit<List<int>>();

            result1.ShouldNotBeNull();
            result1.Count.ShouldBe(1);
            result1[0].ShouldBe(11);
            result1.Capacity.ShouldNotBe(11);

            result2.ShouldNotBeNull();
            result2.Count.ShouldBe(1);
            result2[0].ShouldBe(11);
            result2.Capacity.ShouldNotBe(11);
        }


        public void Test_Insert_Missing_ChildKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(BlogView));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() =>
            {
                buffer.Insert("Hello World!", ("", "Posts.Item.Headline"));
            });
        }

        public void Test_Insert_Invalid_ChildKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(InvalidModel));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() =>
            {
                buffer.Insert(10, ("", "Many.Item.RefId"));
            });
        }

        public void Test_Insert_CompositeKey()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<CompositeModel>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            var data = new (string, Guid, int)[]
            {
                ("X", Guid.Parse("9a48597e-707f-49b9-ba75-b1630e7a9c8f"), 1),
                ("Y", Guid.Parse("0bfb24da-9f64-4779-a6b7-8ae474ef07cb"), 2),
            };

            var data2 = new (string, Guid, int)[]
            {
                ("X", Guid.Parse("9a48597e-707f-49b9-ba75-b1630e7a9c8f"), 1),
                ("X", Guid.Parse("9a48597e-707f-49b9-ba75-b1630e7a9c8f"), 2),
                ("Y", Guid.Parse("0bfb24da-9f64-4779-a6b7-8ae474ef07cb"), 2),
            };

            buffer.Insert(data,
                ("Item.Item1", "Item.Key1"),
                ("Item.Item2", "Item.Key2"),
                ("Item.Item3", "Item.Key3")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Refs.Item.Ref1"),
                ("Item.Item2", "Item.Refs.Item.Ref2"),
                ("Item.Item3", "Item.Refs.Item.Ref3")
            );

            var result = buffer.Commit<IList<CompositeModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result[0].ShouldNotBeNull();
            result[0].Refs.Count.ShouldBe(1);
            result[1].ShouldNotBeNull();
            result[1].Refs.Count.ShouldBe(1);
        }

        public void Test_Insert_CaseInsensitive()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(IList<Blog>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(50, ("", "ITEM.id"));

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(50);
        }

        public void Test_Insert_CaseSensitive()
        {
            var store = DatabaseHelper.Default.GetStore(new DotNotation(StringComparer.Ordinal));
            var schema = store.GetSchema(typeof(IList<Blog>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(50, ("", "ITEM.id"));
            buffer.Insert(60, ("", "Item.Id"));

            var result = buffer.Commit<IList<Blog>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(60);
        }


        public void Test_Insert_OneToMany_NonPrimary()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new (int?, string)[]
            {
                ( 1, "Blog 1" ),
                ( null, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<SecondaryModel>>(), QueryType.List);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Many.Item.Id"),
                ("Item.Item2", "Item.Many.Item.RefId"),
                ("Item.Item3", "Item.Many.Item.Headline")
            );

            var result = buffer.Commit<IList<SecondaryModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Many.ShouldNotBeNull();
            result[0].Many.Count.ShouldBe(2);
            result[0].Many[0].RefId.ShouldBe(result[0].Id);
            result[0].Many[0].Headline.ShouldBe("Post 1.1");
            result[0].Many[1].RefId.ShouldBe(result[0].Id);
            result[0].Many[1].Headline.ShouldBe("Post 1.2");

            result[1].Id.ShouldBe(0);
            result[1].Title.ShouldBe("Blog 2");

            result[1].Many.ShouldNotBeNull();
            result[1].Many.Count.ShouldBe(0);
        }

        public async Task Test_Insert_OneToMany_NonPrimary_Async()
        {
            var store = DatabaseHelper.Default.Store;

            var data1 = new (int?, string)[]
            {
                ( 1, "Blog 1" ),
                ( null, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<SecondaryModel>>(), QueryType.List);

            await buffer.InsertAsync(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            await buffer.InsertAsync(data2,
                ("Item.Item1", "Item.Many.Item.Id"),
                ("Item.Item2", "Item.Many.Item.RefId"),
                ("Item.Item3", "Item.Many.Item.Headline")
            );

            var result = buffer.Commit<IList<SecondaryModel>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Many.ShouldNotBeNull();
            result[0].Many.Count.ShouldBe(2);
            result[0].Many[0].RefId.ShouldBe(result[0].Id);
            result[0].Many[0].Headline.ShouldBe("Post 1.1");
            result[0].Many[1].RefId.ShouldBe(result[0].Id);
            result[0].Many[1].Headline.ShouldBe("Post 1.2");

            result[1].Id.ShouldBe(0);
            result[1].Title.ShouldBe("Blog 2");

            result[1].Many.ShouldNotBeNull();
            result[1].Many.Count.ShouldBe(0);
        }

        public async Task Test_Insert_OneToMany_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new (int, string)[]
            {
                ( 1, "Blog 1" ),
                ( 2, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };
            var data3 = new (int, int, string)[]
            {
                ( 1, 2, "Comment 1.2.1" ),
                ( 2, 2, "Comment 1.2.2" ),
                ( 3, 2, "Comment 1.2.3" ),
                ( 4, 3, "Comment 2.1.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<BlogView>>(), QueryType.List);

            await buffer.InsertAsync(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            await buffer.InsertAsync(data2,
                ("Item.Item1", "Item.Posts.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.BlogId"),
                ("Item.Item3", "Item.Posts.Item.Headline")
            );

            await buffer.InsertAsync(data3,
                ("Item.Item1", "Item.Posts.Item.Comments.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.Comments.Item.BlogPostId"),
                ("Item.Item3", "Item.Posts.Item.Comments.Item.Comment")
            );

            var result = buffer.Commit<IList<BlogView>>();

            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[0].Headline.ShouldBe("Post 1.1");
            result[0].Posts[1].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[1].Headline.ShouldBe("Post 1.2");

            result[0].Posts[0].Comments.Count.ShouldBe(0);

            result[0].Posts[1].Comments.Count.ShouldBe(3);
            result[0].Posts[1].Comments[0].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[0].Comment.ShouldBe("Comment 1.2.1");
            result[0].Posts[1].Comments[1].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[1].Comment.ShouldBe("Comment 1.2.2");
            result[0].Posts[1].Comments[2].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[2].Comment.ShouldBe("Comment 1.2.3");

            result[1].Posts.Count.ShouldBe(1);
            result[1].Posts[0].BlogId.ShouldBe(result[1].Id);
            result[1].Posts[0].Headline.ShouldBe("Post 2.1");

            result[1].Posts[0].Comments.Count.ShouldBe(1);
            result[1].Posts[0].Comments[0].BlogPostId.ShouldBe(result[1].Posts[0].Id);
            result[1].Posts[0].Comments[0].Comment.ShouldBe("Comment 2.1.1");
        }

        public void Test_Insert_OneToMany()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new (int, string)[]
            {
                ( 1, "Blog 1" ),
                ( 2, "Blog 2" ),
            };
            var data2 = new (int, int, string)[]
            {
                ( 1, 1, "Post 1.1" ),
                ( 2, 1, "Post 1.2" ),
                ( 3, 2, "Post 2.1" ),
            };
            var data3 = new (int, int, string)[]
            {
                ( 1, 2, "Comment 1.2.1" ),
                ( 2, 2, "Comment 1.2.2" ),
                ( 3, 2, "Comment 1.2.3" ),
                ( 4, 3, "Comment 2.1.1" ),
            };

            var buffer = new QueryBuffer(store.Describe<IList<BlogView>>(), QueryType.List);

            buffer.Insert(data1,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            buffer.Insert(data2,
                ("Item.Item1", "Item.Posts.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.BlogId"),
                ("Item.Item3", "Item.Posts.Item.Headline")
            );

            buffer.Insert(data3,
                ("Item.Item1", "Item.Posts.Item.Comments.Item.Id"),
                ("Item.Item2", "Item.Posts.Item.Comments.Item.BlogPostId"),
                ("Item.Item3", "Item.Posts.Item.Comments.Item.Comment")
            );

            var result = buffer.Commit<IList<BlogView>>();

            result.Count.ShouldBe(2);

            result[0].Id.ShouldBe(1);
            result[0].Title.ShouldBe("Blog 1");

            result[0].Posts.Count.ShouldBe(2);
            result[0].Posts[0].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[0].Headline.ShouldBe("Post 1.1");
            result[0].Posts[1].BlogId.ShouldBe(result[0].Id);
            result[0].Posts[1].Headline.ShouldBe("Post 1.2");

            result[0].Posts[0].Comments.Count.ShouldBe(0);

            result[0].Posts[1].Comments.Count.ShouldBe(3);
            result[0].Posts[1].Comments[0].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[0].Comment.ShouldBe("Comment 1.2.1");
            result[0].Posts[1].Comments[1].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[1].Comment.ShouldBe("Comment 1.2.2");
            result[0].Posts[1].Comments[2].BlogPostId.ShouldBe(result[0].Posts[1].Id);
            result[0].Posts[1].Comments[2].Comment.ShouldBe("Comment 1.2.3");

            result[1].Posts.Count.ShouldBe(1);
            result[1].Posts[0].BlogId.ShouldBe(result[1].Id);
            result[1].Posts[0].Headline.ShouldBe("Post 2.1");

            result[1].Posts[0].Comments.Count.ShouldBe(1);
            result[1].Posts[0].Comments[0].BlogPostId.ShouldBe(result[1].Posts[0].Id);
            result[1].Posts[0].Comments[0].Comment.ShouldBe("Comment 2.1.1");
        }

        public void Test_Insert_NonPrimaryKeys()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<SecondaryModel>>(), QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<SecondaryModel>>();

            result.Count.ShouldBe(2);
            result[0].Id.ShouldBe(0);
            result[0].Title.ShouldBe("Blog 1");
            result[1].Id.ShouldBe(10);
            result[1].Title.ShouldBe("Blog 2");
        }

        public async Task Test_Insert_NonPrimaryKeys_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<SecondaryModel>>(), QueryType.List);

            await buffer.InsertAsync(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<SecondaryModel>>();

            result.Count.ShouldBe(2);
            result[0].Id.ShouldBe(0);
            result[0].Title.ShouldBe("Blog 1");
            result[1].Id.ShouldBe(10);
            result[1].Title.ShouldBe("Blog 2");
        }

        public void Test_Insert_PrimaryKeys()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(10);
            result[0].Title.ShouldBe("Blog 2");
        }

        public async Task Test_Insert_PrimaryKeys_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int?, string)[]
            {
                ( null, "Blog 1" ),
                ( 10,   "Blog 2" )
            };

            var buffer = new QueryBuffer(store.Describe<IList<Blog>>(), QueryType.List);

            await buffer.InsertAsync(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Title")
            );

            var result = buffer.Commit<IList<Blog>>();

            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(10);
            result[0].Title.ShouldBe("Blog 2");
        }

        public void Test_Insert_DynamicResult()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new int?[] { 1, 2, null };

            var schema1 = store.GetSchema(typeof(object));
            var schema2 = store.GetSchema(typeof(IList<object>));
            var buffer1 = new QueryBuffer(schema1, QueryType.List);
            var buffer2 = new QueryBuffer(schema2, QueryType.List);

            buffer1.Insert(data,
                ("Item", "")
            );

            buffer2.Insert(data,
                ("Item", "Item")
            );

            var result1 = buffer1.Commit<dynamic>();
            var result2 = buffer2.Commit<IList<dynamic>>();

            ((object)result1).ShouldBeNull();
            result2.Select(d => (int?)d).ShouldBe(new int?[] { 1, 2, null });
        }

        public void Test_Aggregate_Invalid_DataType()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            Should.Throw<BindingException>(() => buffer.Insert("Text", ("", "Id"))); // compile time
            Should.Throw<BindingException>(() => buffer.Insert((object)12, ("", "Title"))); // runtime
        }

        public void Test_Insert_Invalid_DataType()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(Blog));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() => buffer.Insert("Text", ("", "Id"))); // compile time
            Should.Throw<BindingException>(() => buffer.Insert((object)12, ("", "Title"))); // runtime
        }

        public void Test_Insert_Invalid_Setter()
        {
            var store = DatabaseHelper.Default.Store;
            var schema = store.GetSchema(typeof(InvalidModel));
            var buffer = new QueryBuffer(schema, QueryType.List);

            var exception = Should.Throw<NotSupportedException>(() =>
            {
                buffer.Insert(100, ("", "GetOnly"));
            });

            exception.Message.ShouldBe("NoTryCatchHere");
        }

        public void Test_Aggregate_Dynamic()
        {
            var store = DatabaseHelper.Default.Store;
            var data = (3, "L3");

            var schema = store.GetSchema(typeof(object));
            var buffer = new QueryBuffer(schema, QueryType.Aggregate);

            buffer.Insert(data, ("Item1", "Id"));
            buffer.Insert(data, ("Item2", "Text.String"));

            var result = buffer.Commit<dynamic>();

            DynamicShould.HaveProperty(result, "Id");
            DynamicShould.HaveProperty(result, "Text");
            DynamicShould.HaveProperty(result.Text, "String");

            var id = Should.NotThrow(() => (int)result.Id);
            var text = Should.NotThrow(() => (string)result.Text.String);

            id.ShouldBe(3);
            text.ShouldBe("L3");
        }
        public void Test_Insert_DynamicGraph()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new (int, string)[]
            {
                (1, "L1"),
                (2, "L2"),
            };

            var schema = store.GetSchema(typeof(IList<object>));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("Item.Item1", "Item.Id"),
                ("Item.Item2", "Item.Text.String")
            );

            var result = buffer.Commit<IList<dynamic>>();

            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);

            DynamicShould.HaveProperty(result[0], "Id");
            DynamicShould.HaveProperty(result[0], "Text");
            DynamicShould.HaveProperty(result[0].Text, "String");

            DynamicShould.HaveProperty(result[1], "Id");
            DynamicShould.HaveProperty(result[1], "Text");
            DynamicShould.HaveProperty(result[1].Text, "String");

            var id0 = Should.NotThrow(() => (int)result[0].Id);
            var text0 = Should.NotThrow(() => (string)result[0].Text.String);
            var id1 = Should.NotThrow(() => (int)result[1].Id);
            var text1 = Should.NotThrow(() => (string)result[1].Text.String);

            id0.ShouldBe(1);
            text0.ShouldBe("L1");
            id1.ShouldBe(2);
            text1.ShouldBe("L2");
        }
    }
}
