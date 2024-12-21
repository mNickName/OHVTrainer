using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace OHVInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string downloadURL = "";
        string zipPath = "C:\\Users\\Laptop\\AppData\\Local\\Temp\\ohvtrainer.zip";
        string pathToPluginsFolder = "\\BepInEx\\plugins\\";

        int maxSteps = 3;

        public MainWindow()
        {
            InitializeComponent();
        }

        void AddStatus(string status)
        {
            if (status_text.Text == "")
            {
                status_text.Text = status;
            } else
            {
                status_text.Text += $"\n{status}";
            }
        }

        private void select_game_dir_button_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    game_dir_textbox.Text = fbd.SelectedPath;
                }
            }
        }

        private void install_button_Click(object sender, RoutedEventArgs e)
        {
            select_game_dir_button.IsEnabled = false;
            install_button.IsEnabled = false;
            AddStatus("Starting...");

            _ = GetDownloadLink();
        }

        async Task GetDownloadLink()
        {
            string url = "https://pastebin.com/raw/6jPVZesH";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    AddStatus($"[1/{maxSteps}] Getting Download URL...");
                    string htmlContent = await client.GetStringAsync(url);
                    var lines = htmlContent.Split('\n');

                    var fullDownload = lines[0].Replace(";", "").Split('=')[1];
                    var trainerDownload = lines[1].Replace(";", "").Split('=')[1];

                    downloadURL = (bool)only_update_trainer_checkbox.IsChecked ? trainerDownload : fullDownload;

                    if (downloadURL == "")
                    {
                        AddStatus("FAILED: URL to file is empty!");
                    } else
                    {
                        _ = StartDownloading();
                    }
                }
            }
            catch (Exception ex)
            {
                AddStatus("Cannot receive Download URL!");
            }
        }

        async Task StartDownloading()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                using (Stream stream = await client.GetStreamAsync(downloadURL))
                using (FileStream fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    AddStatus($"[2/{maxSteps}] Downloading...");
                    await stream.CopyToAsync(fileStream);
                }
                AddStatus($"[2/{maxSteps}] Patch downloaded!");
                Unpack();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd: " + ex.Message);
            }
        }

        void Unpack()
        {
            string extractPath = game_dir_textbox.Text; // Folder docelowy

            if ((bool)only_update_trainer_checkbox.IsChecked) extractPath += pathToPluginsFolder;

            try
            {
                AddStatus($"[3/{maxSteps}] Installing...");

                // Wypakowanie i nadpisywanie istniejących plików
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(extractPath, entry.FullName);

                        // Sprawdź, czy to nie folder
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        // Utwórz folder docelowy, jeśli nie istnieje
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        // Wypakuj plik z nadpisywaniem
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }

                try
                {
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath); // Usunięcie pliku
                        AddStatus("Pliki instalacyjne zostały usunięte");
                    }
                    else
                    {
                        AddStatus("Nie udało się usunąć plików instalacyjnych.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Wystąpił błąd podczas usuwania pliku: " + ex.Message);
                }

                if (System.Windows.Forms.MessageBox.Show("Installation Success", "Close", MessageBoxButtons.OK, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.OK)
                {
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                AddStatus($"Error while unpacking zip file: {ex.Message.ToString()}");
            }
        }
    }
}
