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
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace Jerrycurl.Cqs.Test;

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
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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

    public void Test_Select_JsonNode_Parameter()
    {
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var store = DatabaseHelper.Default.GetStore();

        store.Use(new JsonBindingContractResolver(options));

        var json = "{\"Hello\":\"World!\"}";
        var data1 = JsonNode.Parse(json);
        var parameter1 = new Parameter("P0", store.From(data1));
        var sqlParameter1 = new SqliteParameter();

        parameter1.Build(sqlParameter1);

        sqlParameter1.Value.ShouldBe(json);
    }

    public void Test_Insert_JsonNode()
    {
        var options = new JsonSerializerOptions();
        var store = DatabaseHelper.Default.GetStore();
        store.Use(new JsonBindingContractResolver(options));

        var data = "{ \"Id\": 12, \"Title\": \"Hello World!\" }";
        var schema = store.GetSchema(typeof(JsonNode));
        var buffer = new QueryBuffer(schema, QueryType.List);

        buffer.Insert(data,
            ("", "")
        );

        var result = buffer.Commit<JsonNode>();

        Should.NotThrow(() =>
        {
            var id = (int)result["Id"];
            var title = (string)result["Title"];

            id.ShouldBe(12);
            title.ShouldBe("Hello World!");
        });
    }
}
