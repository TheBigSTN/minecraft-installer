using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Github;

namespace MyCSharpApp {
    public partial class InstallMenu : UserControl {
        public event EventHandler ModpackInstalled;
        public InstallMenu() {
            InitializeComponent();
            this.Loaded += PopulateDropDown;
        }
        private async Task<List<GitHubTreeItem>> GetTreeItems() {
            var main = await GithubHelper.GetGitHubTreeAsync(false);
            var tree = main.Tree.Where(t => t.Type.Equals("tree")).ToList();
            return tree;
        }

        private async void PopulateDropDown(object sender, RoutedEventArgs e) {
            var treeItems = await GetTreeItems();
            foreach (var t in treeItems) {
                Modpack_List.Items.Add(t.Path);
            }
            Modpack_List.Visibility = Visibility.Visible;
        }

        private void SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Install_Button.IsEnabled = true;
        }

        private async void Install(object sender, RoutedEventArgs e) {
            var treeItems = await GetTreeItems();

            string? modpack_name = Modpack_List.SelectedItem.ToString();
            var modpack = treeItems.Find(i => i.Path.Equals(modpack_name));

            if (modpack_name == null || modpack == null)
                return;

            string localPath = Path.Combine(Modpacks.modpacksPath, modpack_name); // ajustează dacă ai altă structură

            if (Directory.Exists(localPath)) {
                var result = MessageBox.Show(
                    $"Modpack-ul \"{modpack_name}\" este deja instalat. Vrei să îl rescrii?",
                    "Modpack deja instalat",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes) {
                    return; // utilizatorul nu vrea să îl rescrie
                }
            }

            await GithubHelper.DownloadModpack(modpack.Sha, modpack_name);
            MessageBox.Show("Modpack Installed if you have tlauncer open press the reload button", "Succes!", MessageBoxButton.OK, MessageBoxImage.Information);
            ModpackInstalled?.Invoke(this, EventArgs.Empty);
        }
    }
}