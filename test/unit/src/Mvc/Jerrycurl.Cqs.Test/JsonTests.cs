using Shouldly;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Test;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Mvc;
using System.Text.Json;
using Jerrycurl.Relations.Language;
using Microsoft.Data.Sqlite;
using Jerrycurl.Extensions.Json.Metadata;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Test.Models.Database;
using Jerrycurl.Cqs.Test.Models.Views;

namespace Jerrycurl.Cqs.Test
{
    public class JsonTests
    {
        public void Test_Update_Json()
        {
            var store = DatabaseHelper.Default.GetStore();

            store.Use(new JsonBindingContractResolver(new JsonSerializerOptions()));

            var json = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var data1 = new BlogJsonView();
            var data2 = new BlogJsonView();
            var target1 = store.From(data1).Lookup("Blog");
            var target2 = store.From(data2).Lookup("Blog");
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target1, "B0"));
            buffer.Add(new ParameterBinding(target2, "P0"));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters[0].Value = json;

            buffer.Update(json, ("", "B0"));

            data1.Blog.ShouldBeNull();
            data2.Blog.ShouldBeNull();

            buffer.Commit();

            data1.Blog.ShouldNotBeNull();
            data1.Blog.Id.ShouldBe(12);
            data1.Blog.Title.ShouldBe("Hello World!");

            data2.Blog.ShouldNotBeNull();
            data2.Blog.Id.ShouldBe(12);
            data2.Blog.Title.ShouldBe("Hello World!");
        }

        public void Test_Insert_Json()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetStore();
            store.Use(new JsonBindingContractResolver(options));

            var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var schema = store.GetSchema(typeof(BlogJsonView));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("", "Blog")
            );

            var result = buffer.Commit<BlogJsonView>();

            result.ShouldNotBeNull();
            result.Blog.ShouldNotBeNull();
            result.Blog.Id.ShouldBe(12);
            result.Blog.Title.ShouldBe("Hello World!");
        }

        public void Test_Insert_Json_NoContract()
        {
            var store = DatabaseHelper.Default.Store;
            var data = "{ \"Id\": 12 }";

            var schema = store.GetSchema(typeof(BlogJsonView));
            var buffer = new QueryBuffer(schema, QueryType.List);

            Should.Throw<BindingException>(() =>
            {
                buffer.Insert(data,
                    ("", "Blog")
                );
            });
        }

        public void Test_Select_Json_Parameter()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
            };
            var store = DatabaseHelper.Default.GetStore();

            store.Use(new JsonBindingContractResolver(options));

            var data = new BlogJsonView() { Blog = new Blog() { Id = 12 } };
            var model = store.From(data).Lookup("Blog");
            var parameter = new Parameter("P0", model);
            var sqlParameter = new SqliteParameter();
            var expected = JsonSerializer.Serialize(data.Blog, options);

            parameter.Build(sqlParameter);

            sqlParameter.Value.ShouldBe(expected);
        }

        public void Test_Select_JsonDocument_Parameter()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
            };
            var store = DatabaseHelper.Default.GetStore();

            store.Use(new JsonBindingContractResolver(options));

            var json = "{\"Hello\":\"World!\"}";
            var data1 = JsonDocument.Parse(json);
            var data2 = data1.RootElement;
            var data3 = (JsonElement?)data2;
            var parameter1 = new Parameter("P0", store.From(data1));
            var parameter2 = new Parameter("P1", store.From(data2));
            var parameter3 = new Parameter("P2", store.From(data3));
            var sqlParameter1 = new SqliteParameter();
            var sqlParameter2 = new SqliteParameter();
            var sqlParameter3 = new SqliteParameter();

            parameter1.Build(sqlParameter1);
            parameter2.Build(sqlParameter2);
            parameter3.Build(sqlParameter3);

            sqlParameter1.Value.ShouldBe(json);
            sqlParameter2.Value.ShouldBe(json);
            sqlParameter3.Value.ShouldBe(json);
        }

        public void Test_Insert_JsonElement()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetStore();
            store.Use(new JsonBindingContractResolver(options));

            var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var schema = store.GetSchema(typeof(JsonElement));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("", "")
            );

            var result = buffer.Commit<JsonElement>();

            Should.NotThrow(() =>
            {
                var id = result.GetProperty("Id");
                var title = result.GetProperty("Title");

                id.GetInt32().ShouldBe(12);
                title.GetString().ShouldBe("Hello World!");
            });
        }

        public void Test_Insert_JsonDocument()
        {
            var options = new JsonSerializerOptions();
            var store = DatabaseHelper.Default.GetStore();
            store.Use(new JsonBindingContractResolver(options));

            var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
            var schema = store.GetSchema(typeof(JsonDocument));
            var buffer = new QueryBuffer(schema, QueryType.List);

            buffer.Insert(data,
                ("", "")
            );

            var result = buffer.Commit<JsonDocument>();

            result.ShouldNotBeNull();

            Should.NotThrow(() =>
            {
                var id = result.RootElement.GetProperty("Id");
                var title = result.RootElement.GetProperty("Title");

                id.GetInt32().ShouldBe(12);
                title.GetString().ShouldBe("Hello World!");
            });
        }
    }
}
