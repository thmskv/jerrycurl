namespace Jerrycurl.Extensions.EntityFrameworkCore.Test.Entities
{
    public partial class OrderLine : BaseEntity
    {
        public int OrderId { get; set; }
        public string Product { get; set; }

        public virtual Order Order { get; set; }
    }
}
