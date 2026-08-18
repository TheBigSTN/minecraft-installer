using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using ModpackInstaller.Services.Notifications;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels;

public partial class NotificationCardViewModel : ViewModelBase {
    [Reactive] private string _title = "";
    [Reactive] private string _body = "";
    [Reactive] private double _progress;
    [Reactive] private bool _autoClose;

    private IDisposable? _consoleSubscription;
    private int _consoleLine;

    public void ReportProgress(double progress) {
        Progress = progress;
    }

    public void EnableConsole() {
        if (_consoleSubscription != null)
            return;

        _consoleLine = Console.CursorTop;

        // Reserve 3 lines.
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();

        _consoleSubscription = this
                               .WhenAnyValue(
                                   x => x.Title,
                                   x => x.Body,
                                   x => x.Progress)
                               .Subscribe(_ => UpdateConsole());

        UpdateConsole();
    }

    private void UpdateConsole() {
        var oldLeft = Console.CursorLeft;
        var oldTop = Console.CursorTop;

        Console.SetCursorPosition(0, _consoleLine);
        Console.Write(new string(' ', Console.WindowWidth - 1));

        Console.SetCursorPosition(0, _consoleLine);
        Console.WriteLine(Title);

        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, _consoleLine + 1);
        Console.WriteLine(Body);

        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, _consoleLine + 2);
        Console.WriteLine(CreateProgressBar());

        Console.SetCursorPosition(oldLeft, oldTop);
    }

    private string CreateProgressBar() {
        const int width = 40;

        var filled = (int)(Progress * width);

        return $"[{new string('#', filled)}{new string('-', width - filled)}] " +
               $"{Progress:P0}";
    }

    public void DisableConsole() {
        _consoleSubscription?.Dispose();
        _consoleSubscription = null;
    }

    public async Task StartAutoCloseAsync(TimeSpan duration) {
        const int fps = 30;

        var delay = TimeSpan.FromMilliseconds(1000.0 / fps);
        var total = duration.TotalMilliseconds;
        var elapsed = 0.0;

        while (elapsed < total) {
            await Task.Delay(delay).ConfigureAwait(false);

            elapsed += delay.TotalMilliseconds;
            Progress = Math.Min(elapsed / total, 1.0);
        }

        NotificationManager.Remove(this);
    }
}