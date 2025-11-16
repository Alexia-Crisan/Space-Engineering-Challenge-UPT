using System.Collections.Concurrent;
using System.Threading.Tasks;

public class AsyncCommandQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Add(T cmd)
    {
        _queue.Enqueue(cmd);
        _signal.Release();
    }

    public async Task<T> TakeAsync(CancellationToken token)
    {
        await _signal.WaitAsync(token);
        _queue.TryDequeue(out var cmd);
        return cmd;
    }
}
