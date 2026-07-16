namespace ModpackInstaller.Models;

public class DialogResponse<TStatus, TResponse> {
    public TStatus Status { get; set; } = default!;
    public TResponse? Response { get; set; }
}