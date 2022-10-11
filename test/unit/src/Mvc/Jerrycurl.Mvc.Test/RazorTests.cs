using Jerrycurl.Mvc.Test.Project.Models;
using Jerrycurl.Test.Project.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Models.Database;
using Jerrycurl.Mvc.Projections;
using System.Text.RegularExpressions;

namespace Jerrycurl.Mvc.Test
{
    public class RazorTests
    {
        public void Test_Razor_Vars()
        {
            var runner = new Runner();
            var data = new BlogView()
            {
                Id = 1,
                Posts = new List<BlogPost>()
                {
                    new BlogPost() { Id = 1 },
                    new BlogPost() { Id = 2, CreatedOn = DateTime.Now },
                }
            };
            var model = Runnable.Command(data, separator: ",");

            model.M(p => p.Par(m => m.Id));
            model.Sql(";");
            model.M(p => p.Var(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Var(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Var(m => m.CreatedOn));
            model.Sql(";");
            model.M(p => p.Vals(m => m.Posts).Skip(1).FirstOrDefault().Var(m => m.Id));
            model.Sql(";");
            model.M(p => p.Var(m => m.Id));

            var result = runner.Sql(model);

            result.ShouldBe("@P0;@V0;@V1;@V2;@V3;@V0");
        }

        public void Test_Razor_Result_Props()
        {
            var runner = new Runner();

            var result1 = runner.Sql<BlogView>(p => p.Prop(m => m.Title));
            var result2 = runner.Sql<BlogView>(p => p.Props());
            var result3 = runner.Sql<BlogView>(p => p.Props(m => m.Posts));
            var result4 = runner.Sql<BlogView>(p => p.Open(m => m.Posts).Props());

            result1.ShouldBe(@"""Item.Title""");
            result2.ShouldBe(@"""Item.Id"",""Item.Title"",""Item.CategoryId""");
            result3.ShouldBe(@"""Item.Posts.Item.Id"",""Item.Posts.Item.BlogId"",""Item.Posts.Item.CreatedOn"",""Item.Posts.Item.Headline"",""Item.Posts.Item.Content""");
            result4.ShouldBe(result3);
        }

        public void Test_Razor_Model_Props()
        {
            var runner = new Runner();
            var data = new BlogView()
            {
                Id = 1,
                Posts = new List<BlogPost>()
                {
                    new BlogPost() { Id = 1 },
                    new BlogPost() { Id = 2, CreatedOn = DateTime.Now },
                }
            };
            var model = Runnable.Command(data, separator: ",");

            model.M(p => p.Prop(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Prop(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Prop(m => m.CreatedOn));
            model.Sql(";");
            model.M(p => p.Vals(m => m.Posts).Skip(1).FirstOrDefault().Prop(m => m.Id));

            var result = runner.Sql(model);

            result.ShouldBe(@"""Model.Id"";""Model.Posts.Item[0].Id"";""Model.Posts.Item[0].CreatedOn"";""Model.Posts.Item[1].Id""");
        }

        public void Test_Razor_Filters()
        {
            var runner = new Runner();

            var result1 = runner.Sql<Blog>(p => p.In().ColNames());
            var result2 = runner.Sql<Blog>(p => p.Out().ColNames());
            var result3 = runner.Sql<Blog>(p => p.Key().ColNames());
            var result4 = runner.Sql<Blog>(p => p.NonKey().ColNames());
            var result5 = runner.Sql<Blog>(p => p.Id().ColName());

            result1.ShouldBe(@"""Title"",""CategoryId""");
            result2.ShouldBe(@"""Id"",""CategoryId""");
            result3.ShouldBe(@"""Id""");
            result4.ShouldBe(@"""Title""");
            result5.ShouldBe(@"""Id""");
        }

        public void Test_Razor_LitList()
        {
            var runner = new Runner();
            var data1 = new List<int>() { 1, 2, 3, 4 };
            var data2 = new List<BlogView>()
            {
                new BlogView()
                {
                    Posts = new List<BlogPost>()
                    {
                        new BlogPost() { Id = 1, },
                        new BlogPost() { Id = 2, },
                        new BlogPost() { Id = 3, },
                    }
                },
                new BlogView()
                {
                    Posts = new List<BlogPost>()
                    {
                        new BlogPost() { Id = 4, },
                        new BlogPost() { Id = 5, },
                    }
                }
            };

            var result1 = runner.Sql(model: data1, p => p.LitList());
            var result2 = runner.Sql(model: data2, p => p.Open().Open(m => m.Posts).For(m => m.Id).LitList());
            var result3 = runner.Sql(model: data2, p => p.Open().Val().Open(m => m.Posts).For(m => m.Id).LitList());

            result1.ShouldBe("1, 2, 3, 4");
            result2.ShouldBe("1, 2, 3, 4, 5");
            result3.ShouldBe("1, 2, 3");
        }
        public void Test_Razor_ParList()
        {
            var runner = new Runner();
            var data1 = new List<int>() { 1, 2, 3, 4 };
            var data2 = new List<BlogView>()
            {
                new BlogView()
                {
                    Posts = new List<BlogPost>()
                    {
                        new BlogPost() { Id = 1, },
                        new BlogPost() { Id = 2, },
                        new BlogPost() { Id = 3, },
                    }
                },
                new BlogView()
                {
                    Posts = new List<BlogPost>()
                    {
                        new BlogPost() { Id = 4, },
                        new BlogPost() { Id = 5, },
                    }
                }
            };

            var result1 = runner.Sql(model: data1, p => p.ParList());
            var result2 = runner.Sql(model: data2, p => p.Open().Open(m => m.Posts).For(m => m.Id).ParList());
            var result3 = runner.Sql(model: data2, p => p.Open().Val().Open(m => m.Posts).For(m => m.Id).ParList());

            result1.ShouldBe("@P0, @P1, @P2, @P3");
            result2.ShouldBe("@P0, @P1, @P2, @P3, @P4");
            result3.ShouldBe("@P0, @P1, @P2");
        }

        public void Test_Razor_Pars()
        {
            var runner = new Runner();
            var data = new BlogView()
            {
                Id = 1,
                Posts = new List<BlogPost>()
                {
                    new BlogPost() { Id = 1 },
                    new BlogPost() { Id = 2, CreatedOn = DateTime.Now },
                }
            };
            var model = Runnable.Command(data, separator: ",");

            model.M(p => p.Par(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Par(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Par(m => m.CreatedOn));
            model.Sql(";");
            model.M(p => p.Vals(m => m.Posts).Skip(1).FirstOrDefault().Par(m => m.Id));
            model.Sql(";");
            model.M(p => p.Par(m => m.Id));

            var result = runner.Sql(model);

            result.ShouldBe("@P0;@P1;@P2;@P3;@P0");
        }

        public void Test_Razor_Lits()
        {
            var runner = new Runner();
            var data = new BlogView()
            {
                Id = 1,
                Posts = new List<BlogPost>()
                {
                    new BlogPost() { Id = 5 },
                    new BlogPost() { Id = 12 },
                }
            };
            var model = Runnable.Command(data, separator: ",");

            model.M(p => p.Lit(m => m.Id));
            model.Sql(";");
            model.M(p => p.Open(m => m.Posts).Lit(m => m.Id));
            model.Sql(";");
            model.M(p => p.Vals(m => m.Posts).Skip(1).FirstOrDefault().Lit(m => m.Id));

            var result = runner.Sql(model);

            result.ShouldBe("1;5;12");
        }

        public void Test_Razor_Eq()
        {
            var runner = new Runner();
            var model = Runnable.Query<Blog>();

            model.R(m => m.ColNames().Eq().ColNames());

            var result = Regex.Replace(runner.Sql(model), @"\s+", "");

            result.ShouldBe(@"""Id""=""Id"",""Title""=""Title"",""CategoryId""=""CategoryId""");
        }

        public void Test_Razor_IsEq()
        {
            var runner = new Runner();
            var model = Runnable.Query<Blog>();

            model.R(m => m.ColNames().IsEq().ColNames());

            var result = Regex.Replace(runner.Sql(model), @"\s+", "");

            result.ShouldBe(@"""Id""=""Id""AND""Title""=""Title""AND""CategoryId""=""CategoryId""");
        }
        public void Test_Razor_Nulls()
        {
            var runner = new Runner();
            var model = Runnable.Query<Blog>();

            var result1 = runner.Sql<Blog>(p => p.ColName(m => m.Id).IsNull());
            var result2 = runner.Sql<Blog>(p => p.ColName(m => m.Id).IsNotNull());

            result1.ShouldBe(@"""Id"" IS NULL");
            result2.ShouldBe(@"""Id"" IS NOT NULL");
        }

        public void Test_Razor_As()
        {
            var runner = new Runner();

            var result = runner.Sql<Blog>(p => p.Attr().Append("X").As().Append("Y"));

            result.ShouldBe("X AS Y");
        }

        public void Test_Razor_JsonPath()
        {
            var runner = new Runner();

            var result1 = runner.Sql<JsonView>(p => p.JsonPath(m => m.Json.Value));
            var result2 = runner.Sql<JsonView>(p => p.JsonPath(m => m.Json.Model.Value));
            var result3 = runner.Sql<JsonView>(p => p.JsonPath(m => m.Json.List));
            var result4 = runner.Sql<JsonView>(p => p.Open(m => m.Json.List).JsonPath(m => m.Value));
            var result5 = runner.Sql<JsonView>(p => p.JsonPath());
            var result6 = runner.Sql<JsonView>(p => p.JsonPath(m => m.Json));

            result1.ShouldBe(@"'$.Value'");
            result2.ShouldBe(@"'$.Model.Value'");
            result3.ShouldBe(@"'$.List'");
            result4.ShouldBe(@"'$.Value'");
            result5.ShouldBe(@"'$'");
            result6.ShouldBe(@"'$'");
        }


        public void Test_Razor_Cols()
        {
            var runner = new Runner();
            var model = new Runnable<object, BlogView>(separator: ",");

            model.R(p => p.Col(m => m.Title));
            model.Sql(";");
            model.R(p => p.Open(m => m.Posts).Col(m => m.Headline));

            var result1 = runner.Sql<BlogView>(p => p.Col(m => m.Title));
            var result2 = runner.Sql<BlogView>(p => p.Cols());
            var result3 = runner.Sql<BlogView>(p => p.Cols(m => m.Posts));
            var result4 = runner.Sql<BlogView>(p => p.Open(m => m.Posts).Cols());
            var result5 = runner.Sql(model);

            result1.ShouldBe(@"T0.""Title""");
            result2.ShouldBe(@"T0.""Id"",T0.""Title"",T0.""CategoryId""");
            result3.ShouldBe(@"T0.""Id"",T0.""BlogId"",T0.""CreatedOn"",T0.""Headline"",T0.""Content""");
            result4.ShouldBe(result3);
            result5.ShouldBe(@"T0.""Title"";T1.""Headline""");
        }

        public void Test_Razor_ColNames()
        {
            var runner = new Runner();

            var result1 = runner.Sql<BlogView>(p => p.ColName(m => m.Title));
            var result2 = runner.Sql<BlogView>(p => p.ColNames());
            var result3 = runner.Sql<BlogView>(p => p.ColNames(m => m.Posts));
            var result4 = runner.Sql<BlogView>(p => p.Open(m => m.Posts).ColNames());

            result1.ShouldBe(@"""Title""");
            result2.ShouldBe(@"""Id"",""Title"",""CategoryId""");
            result3.ShouldBe(@"""Id"",""BlogId"",""CreatedOn"",""Headline"",""Content""");
            result4.ShouldBe(result3);

            Should.Throw<ProjectionException>(() => runner.Sql<BlogView>(p => p.TblName(m => m.NumberOfPosts)));
        }

        public void Test_Razor_Tbls()
        {
            var runner = new Runner();
            var model = new Runnable<object, BlogView>();

            model.R(p => p.Tbl());
            model.Sql(";");
            model.R(p => p.Tbl(m => m.Posts));

            var result1 = runner.Sql<BlogView>(p => p.Tbl());
            var result2 = runner.Sql<BlogView>(p => p.Tbl(m => m.Posts));
            var result3 = runner.Sql<BlogView>(p => p.Open(m => m.Posts).Tbl());
            var result4 = runner.Sql<BlogView>(p => p.Tbl(m => m.Id));
            var result5 = runner.Sql(model);

            result1.ShouldBe(@"""dbo"".""Blog"" T0");
            result2.ShouldBe(@"""dbo"".""BlogPost"" T0");
            result3.ShouldBe(@"""dbo"".""BlogPost"" T0");
            result4.ShouldBe(@"""dbo"".""Blog"" T0");
            result5.ShouldBe(@"""dbo"".""Blog"" T0;""dbo"".""BlogPost"" T1");
        }

        public void Test_Razor_TblNames()
        {
            var runner = new Runner();

            var result1 = runner.Sql<BlogView>(p => p.TblName());
            var result2 = runner.Sql<BlogView>(p => p.TblName(m => m.Posts));
            var result3 = runner.Sql<BlogView>(p => p.Open(m => m.Posts).TblName());
            var result4 = runner.Sql<BlogView>(p => p.TblName(m => m.Id));

            result1.ShouldBe(@"""dbo"".""Blog""");
            result2.ShouldBe(@"""dbo"".""BlogPost""");
            result3.ShouldBe(@"""dbo"".""BlogPost""");
            result4.ShouldBe(@"""dbo"".""Blog""");

            Should.Throw<ProjectionException>(() => runner.Sql<BlogView>(p => p.TblName(m => m.NumberOfPosts)));
        }

        public void Test_Razor_Star()
        {
            var runner = new Runner();
            var model = new Runnable<object, BlogView>(separator: ",");

            model.R(p => p.Star());
            model.Sql(";");
            model.R(p => p.Star(m => m.Posts));
            model.Sql(";");
            model.R(p => p.Cols().As().Props());

            var expected1 = @"T0.""Id"" AS ""Item.Id"",T0.""Title"" AS ""Item.Title"",T0.""CategoryId"" AS ""Item.CategoryId""";
            var expected2 = @"T1.""Id"" AS ""Item.Posts.Item.Id"",T1.""BlogId"" AS ""Item.Posts.Item.BlogId"",T1.""CreatedOn"" AS ""Item.Posts.Item.CreatedOn""," +
                @"T1.""Headline"" AS ""Item.Posts.Item.Headline"",T1.""Content"" AS ""Item.Posts.Item.Content""";

            var result = runner.Sql(model);

            result.ShouldBe($"{expected1};{expected2};{expected1}");                
        }
    }
}
