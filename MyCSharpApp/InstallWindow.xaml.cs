using System.Diagnostics;
using System.Windows;
using Github;

namespace MyCSharpApp {
    public partial class InstallModpackWindow : Window {

        public InstallModpackWindow() {
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

            await GithubHelper.DownloadModpack(modpack.Sha, modpack_name);
            this.Close();
        }
    }
}