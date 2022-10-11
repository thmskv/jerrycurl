namespace Jerrycurl.Cqs.Sessions
{
    public interface ISessionOptions
    {
        IAsyncSession GetAsyncSession();
        ISyncSession GetSyncSession();
    }
}
