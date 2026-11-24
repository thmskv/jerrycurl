namespace Jerrycurl.Cqs.Sessions;

public interface ISessionOptions
{
    public IAsyncSession GetAsyncSession();
    public ISyncSession GetSyncSession();
}
