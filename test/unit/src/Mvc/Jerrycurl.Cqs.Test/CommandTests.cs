using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Relations;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Language;
using Shouldly;
using Jerrycurl.Test;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations.Language;
using System.Data;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Cqs.Test
{
    public class CommandTests
    {
        public void Test_Update_LanguageFeatures()
        {
            var store = DatabaseHelper.Default.Store;
            var buffer1 = new CommandBuffer(store);
            var buffer2 = new CommandBuffer();

            Should.NotThrow(() => buffer1.Update(1, ("", "foo")));
            Should.Throw<CommandException>(() => buffer2.Update(1, ("", "foo")));
        }

        public void Test_Update_InvalidDataType()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new Blog();
            var target = store.From(data).Lookup("Id");
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target, "C0"));

            buffer.Update("Text", ("", "C0"));
            buffer.Update((object)"Text", ("", "C0"));
        }

        public void Test_Update_Missing()
        {
            var store = DatabaseHelper.Default.Store;
            var target = store.From<Blog>(null).Lookup("Id");
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target, "C0"));
            buffer.Update(10, ("", "c0"));

            Should.Throw<BindingException>(() => buffer.Commit());
        }

        public void Test_Update_CaseInsensitive()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new Blog();
            var target = store.From(data).Lookup("Id");
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target, "C0"));
            buffer.Update(10, ("", "c0"));
            buffer.Commit();

            data.Id.ShouldBe(10);
        }

        public void Test_Update_Parameter_Propagation()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new Blog();
            var data2 = new Blog();
            var target1 = store.From(data1).Lookup("Id");
            var target2 = store.From(data2).Lookup("Id");
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target1, "C1"));
            buffer.Add(new Parameter("P2", target2), target2);
            buffer.Update(10, ("", "C1"));

            var parameters1 = buffer.Prepare(() => new MockParameter());

            parameters1[0].Value = 20;

            buffer.Add(new Parameter("P1", target1));
            buffer.Add(new Parameter("P2", target2));

            var parameters2 = buffer.Prepare(() => new MockParameter());

            parameters2.Count.ShouldBe(2);
            parameters2[0].Value.ShouldBe(10);
            parameters2[1].Value.ShouldBe(20);
        }

        public void Test_Update_Indexer()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new List<int>() { 1, 2, 3 };
            var data2 = new[] { 4, 5, 6 };
            var target1 = store.From(data1).Select("Item").Body.Skip(1).First()[0];
            var target2 = store.From(data2).Select("Item").Body.Skip(1).First()[0];
            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(target1, "C1"));
            buffer.Add(new ColumnBinding(target2, "C2"));
            buffer.Update(50, ("", "C1"));
            buffer.Update(100, ("", "C2"));
            buffer.Commit();

            data1.ShouldBe(new[] { 1, 50, 3 });
            data2.ShouldBe(new[] { 4, 100, 6 });
        }

        public void Test_Update_Priority()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new Blog();
            var target = store.From(data).Lookup("Id");

            var buffer = new CommandBuffer(store);

            buffer.Add(new Parameter("P0", target), target);
            buffer.Add(new ColumnBinding(target, "C0"));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters.Count.ShouldBe(1);
            parameters[0].Direction.ShouldBe(ParameterDirection.Input);
        }

        public void Test_Update_FromParameter()
        {
            var store = DatabaseHelper.Default.Store;
            var data = new Blog();
            var targets = store.From(data).Lookup("Id", "Title");

            var buffer = new CommandBuffer(store);

            buffer.Add(new Parameter("P0", targets[0]), targets[0]);
            buffer.Add(new ParameterBinding(targets[1], "P1"));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters[0].Value = 12;
            parameters[1].Value = "Blog!";

            buffer.Commit();

            data.Id.ShouldBe(12);
            data.Title.ShouldBe("Blog!");
        }

        public void Test_Update_FromColumn()
        {
            var store = DatabaseHelper.Default.Store;
            var sourceData = new Blog() { Id = 12, Title = "Blog!" };
            var targetData = new Blog();

            var targets = store.From(targetData).Lookup("Id", "Title");
            using var source = store.From(sourceData)
                                    .Select("Id", "Title")
                                    .As("C1", "C2");

            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(targets[0], "C1"));
            buffer.Add(new ColumnBinding(targets[1], "C2"));

            buffer.Update(source);

            targetData.Id.ShouldBe(default);
            targetData.Title.ShouldBe(default);

            buffer.Commit();

            targetData.Id.ShouldBe(12);
            targetData.Title.ShouldBe("Blog!");
        }

        public async Task Test_Update_FromColumn_Async()
        {
            var store = DatabaseHelper.Default.Store;
            var sourceData = new Blog() { Id = 12, Title = "Blog!" };
            var targetData = new Blog();

            var targets = store.From(targetData).Lookup("Id", "Title");
            using var source = store.From(sourceData)
                                    .Select("Id", "Title")
                                    .As("C1", "C2");

            var buffer = new CommandBuffer(store);

            buffer.Add(new ColumnBinding(targets[0], "C1"));
            buffer.Add(new ColumnBinding(targets[1], "C2"));

            await buffer.UpdateAsync(source);

            targetData.Id.ShouldBe(default);
            targetData.Title.ShouldBe(default);

            buffer.Commit();

            targetData.Id.ShouldBe(12);
            targetData.Title.ShouldBe("Blog!");
        }

        public void Test_Update_FromCascade_Cyclic()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new Blog();
            var data2 = new Blog();
            var data3 = new Blog();
            var target1 = store.Lookup(data1, "Id");
            var target2 = store.Lookup(data2, "Id");
            var target3 = store.Lookup(data3, "Id");

            var buffer = new CommandBuffer(store);

            buffer.Add(new CascadeBinding(target1, target2));
            buffer.Add(new CascadeBinding(target2, target3));
            buffer.Add(new CascadeBinding(target3, target1));

            Should.NotThrow(() => buffer.Commit());

            buffer.Add(new ColumnBinding(target2, "C0"));
            buffer.Update(12, ("", "C0"));

            Should.NotThrow(() => buffer.Commit());

            data1.Id.ShouldBe(12);
            data2.Id.ShouldBe(12);
            data3.Id.ShouldBe(12);
        }

        public void Test_Update_FromCascadingParameter()
        {
            var store = DatabaseHelper.Default.Store;
            var data1 = new Blog();
            var data2 = new Blog();
            var data3 = new Blog();
            var target1 = store.From(data1).Lookup("Id");
            var target2 = store.From(data2).Lookup("Id");
            var target3 = store.From(data3).Lookup("Id");

            var buffer = new CommandBuffer(store);

            buffer.Add(new ParameterBinding(target1, "P0"));
            buffer.Add(new CascadeBinding(target2, target1));
            buffer.Add(new CascadeBinding(target3, target2));

            var parameters = buffer.Prepare(() => new MockParameter());

            parameters[0].Value = 11;

            buffer.Commit();

            data1.Id.ShouldBe(11);
            data2.Id.ShouldBe(11);
            data3.Id.ShouldBe(11);
        }
    }
}
