namespace Jerrycurl.Cqs.Test.Models.Custom
{
    public class NoConstructModel
    {
        public int Int { get; set; }
        public string String { get; set; }

        public NoConstructModel(string s)
        {
            this.String = s;
        }
    }
}
