using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Models;

public class ModpackManifest {
    public List<InstalledModInfo> InstalledMods { get; set; } = [];
}