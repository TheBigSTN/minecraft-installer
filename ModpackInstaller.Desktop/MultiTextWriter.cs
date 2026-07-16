using System.IO;
using System.Text;

namespace ModpackInstaller.Desktop;

internal sealed class MultiTextWriter(params TextWriter[] writers) : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) {
        foreach (var writer in writers)
            writer.Write(value);
    }

    public override void Write(string? value) {
        foreach (var writer in writers)
            writer.Write(value);
    }

    public override void WriteLine(string? value) {
        foreach (var writer in writers)
            writer.WriteLine(value);
    }

    public override void Flush() {
        foreach (var writer in writers)
            writer.Flush();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (var writer in writers)
                writer.Dispose();
        }

        base.Dispose(disposing);
    }
}