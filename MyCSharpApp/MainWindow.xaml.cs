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
            if(modpacks.Count != 0)
                AddModpackButtons(modpacks);
            else {
                Sidebar.Visibility = Visibility.Collapsed;
                TopBar.Visibility = Visibility.Collapsed;
                Install_Button_Click(null, null);
            } 
        }

        private void AddModpackButtons(List<Modpacks.Modpack> modpacks) {
            // Clear any existing buttons in the StackPanel
            SidebarButtonPanel.Children.Clear();

            // Loop through each modpack and create a button
            foreach (var modpack in modpacks) {
                Button modpackButton = new Button {
                    Content = modpack.MineLoader.ModpackName,
                    Style = (Style)FindResource("ModpackButtonStyle")
                };

                modpackButton.Click += (sender, e) =>
                {
                    var detailsControl = new ModpackInfo(modpack);
                    BodyContent.Content = detailsControl;
                };

                SidebarButtonPanel.Children.Add(modpackButton);
            }
        }

        private void Install_Button_Click(object sender, RoutedEventArgs e) {
            InstallMenu installModpackWindow = new InstallMenu();

            // Te abonezi la evenimentul ModpackInstalled
            installModpackWindow.ModpackInstalled += InstallModpackWindow_ModpackInstalled;

            // Înlocuiește conținutul din BodyContent cu InstallMenu
            BodyContent.Content = installModpackWindow;
        }

        private void InstallModpackWindow_ModpackInstalled(object sender, EventArgs e) {
            // După ce instalarea modpack-ului este completă, poți reîncărca lista de modpack-uri
            var modpacks = Modpacks.GetModpacks();

            // Actualizează butoanele din sidebar pentru a reflecta modpack-urile noi
            AddModpackButtons(modpacks);

            BodyContent.Content = null;
            Sidebar.Visibility = Visibility.Visible;
            TopBar.Visibility = Visibility.Visible;
        }
    }
}