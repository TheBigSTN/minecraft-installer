using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Velopack;

namespace ModpackInstaller.Infrastructure;

public class UpdateService {
    private readonly UpdateManager _manager;

    public UpdateService() {
        _manager = new UpdateManager("https://github.com/TheBigSTN/minecraft-installer");
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
        => await _manager.CheckForUpdatesAsync();

    public async Task DownloadAndRestartAsync(UpdateInfo update, Action<int> progress)
        => await _manager.DownloadUpdatesAsync(update, progress);
}
