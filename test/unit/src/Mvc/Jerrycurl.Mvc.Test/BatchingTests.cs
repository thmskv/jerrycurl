using Jerrycurl.Cqs.Commands;
using Jerrycurl.Mvc.Test.Project.Accessors;
using Shouldly;
using System.Linq;

namespace Jerrycurl.Mvc.Test
{
    public class BatchingTests
    {
        private readonly ProcLocator locator = new ProcLocator();
        private readonly ProcEngine engine = new ProcEngine(null);

        public void Test_SqlBuffer_Batching()
        {
            var page = this.locator.FindPage("../Commands/Batching/BatchedCommand.cssql", typeof(LocatorAccessor));
            var factory = this.engine.Proc(page, new ProcArgs(typeof(object), typeof(object)));

            var result = factory(null);
            var serializer = result.Buffer as ISqlSerializer<Command>;

            var batchedBySql = serializer.Serialize(new SqlOptions() { MaxSql = 1 }).ToList();
            var batchedByParams = serializer.Serialize(new SqlOptions() { MaxParameters = 2 }).ToList();
            var notBatched = serializer.Serialize(new SqlOptions()).ToList();

            batchedBySql.ShouldNotBeNull();
            batchedByParams.ShouldNotBeNull();
            notBatched.ShouldNotBeNull();

            batchedBySql.Count.ShouldBe(20);
            batchedByParams.Count.ShouldBe(10);
            notBatched.Count.ShouldBe(1);

            var joinedSql = string.Join("", batchedBySql.Select(d => d.CommandText));
            var joinedParams = string.Join("", batchedByParams.Select(d => d.CommandText));

            notBatched.First().CommandText.ShouldBe(joinedSql);
            notBatched.First().CommandText.ShouldBe(joinedParams);

        }
    }
}
