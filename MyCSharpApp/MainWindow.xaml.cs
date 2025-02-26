using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyCSharpApp {
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
            var modpacks = Modpacks.GetModpacks();
            AddModpackButtons(modpacks);

            if (modpacks.Count == 0) {
                Sidebar.Visibility = Visibility.Collapsed;
                StartInfo.Visibility = Visibility.Visible;
            }
        }

        private void AddModpackButtons(List<Modpacks.Modpack> modpacks) {
            // Clear any existing buttons in the StackPanel
            SidebarButtonPanel.Children.Clear();

            // Loop through each modpack and create a button
            foreach (var modpack in modpacks) {
                Button modpackButton = new Button {
                    Content = modpack.MineLoader.AdditionalName,
                };

                // Apply the XAML-defined style
                modpackButton.Style = (Style)FindResource("ModpackButtonStyle");

                SidebarButtonPanel.Children.Add(modpackButton);
            }
        }

        private void Install_Modpack(object sender, RoutedEventArgs e) {
            InstallModpackWindow window = new();
            window.Owner = this;
            window.ShowDialog();
            var modpacks = Modpacks.GetModpacks();

            AddModpackButtons(modpacks);

            Sidebar.Visibility = Visibility.Visible;
            StartInfo.Visibility = Visibility.Hidden;
        }
    }
}