using System.Threading.Tasks;

namespace ModpackInstaller.ViewModels;

public abstract class DialogViewModel<TResult> : ViewModelBase {
    private readonly TaskCompletionSource<TResult> _tcs = new();

    public Task<TResult> WaitAsync() => _tcs.Task;

    protected void Close(TResult result) {
        _tcs.TrySetResult(result);
    }
}