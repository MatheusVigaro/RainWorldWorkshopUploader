using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace RainWorldWorkshopUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        public MainWindow()
        {
            InitializeComponent();

            SteamManager.Init();
            Manager = new RainWorldSteamManager();

            if (Manager.shutdown)
            {
                throw new Exception("Unable to initialize RainWorldSteamManager");
            }

            System.Windows.Threading.DispatcherTimer callbackRunner = new System.Windows.Threading.DispatcherTimer();
            callbackRunner.Tick += new EventHandler(CallbackRunner_Tick);
            callbackRunner.Interval = new TimeSpan(0, 0, 0, 0, 100);
            callbackRunner.Start();

            Instance = this;
        }

        private static RainWorldSteamManager Manager;

        private void CallbackRunner_Tick(object? sender, EventArgs e)
        {
            Manager.Update();

            if (Manager.isCurrentlyUploading && Manager.bytesTotal > 0UL)
            {
                SetTitle("Uploading (" + (Manager.bytesProcessed / Manager.bytesTotal * 100.0).ToString("0.00") + "%)...");
            }
        }

        public static Mod? SelectedMod = null;

        private void SelectMod_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new();

            dlg.DefaultExt = ".json";

            bool? result = dlg.ShowDialog();

            if (!result ?? true)
            {
                return;
            }

            var modInfoFile = dlg.FileName;
            var modPath = new FileInfo(modInfoFile).Directory.FullName;

            SelectedMod = Mod.LoadModFromJson(modPath);

            TitleBox.Text = SelectedMod.WorkshopData?.Title ?? SelectedMod.name;
            IDBox.Text = SelectedMod.WorkshopData?.ID ?? SelectedMod.id;
            VersionBox.Text = SelectedMod.WorkshopData?.Version ?? SelectedMod.version;
            DescriptionBox.Text = SelectedMod.WorkshopData?.Description ?? SelectedMod.description;

            RequirementsBox.Text = SelectedMod.WorkshopData?.Requirements ?? string.Join(",", SelectedMod.requirements);
            RequirementNamesBox.Text = SelectedMod.WorkshopData?.RequirementNames ?? string.Join(",", SelectedMod.requirementsNames);
            AuthorBox.Text = SelectedMod.WorkshopData?.Authors ?? SelectedMod.authors.Replace("<LINE>", Environment.NewLine);
            WorkshopIDBox.Text = SelectedMod.WorkshopData.WorkshopID > 0UL ? SelectedMod.WorkshopData.WorkshopID.ToString() : "";

            TargetVersionBox.Text = SelectedMod.WorkshopData.TargetGameVersion ?? "v1.9.06";
            if (string.IsNullOrEmpty(SelectedMod.WorkshopData.TargetGameVersion))
            {
                TargetVersionBox.Text = "v1.9.06";
            }

            VisibilityBox.Text = SelectedMod.WorkshopData?.Visibility ?? "Unlisted";

            if (SelectedMod.WorkshopData?.Tags?.Length > 0)
            {
                SetSelectedTags(SelectedMod.WorkshopData?.Tags ?? SelectedMod.tags);
            }
            else
            {
                SetSelectedTags(SelectedMod.tags);
            }


            UploadFilesOnly.IsChecked = SelectedMod.WorkshopData?.UploadFilesOnly ?? false;

            SaveWorkshopDataButton.IsEnabled = true;
            VerifyButton.IsEnabled = true;
            UploadButton.IsEnabled = true;
            UploadThumbnailButton.IsEnabled = true;
        }

        private void SetSelectedTags(string[] tags)
        {
            for (var i = 0; i < TagsPanel.Children.Count; i++)
            {
                var tag = TagsPanel.Children[i] as CheckBox;
                if (tags.Contains(tag.Content.ToString()))
                {
                    tag.IsChecked = true;
                }
                else
                {
                    tag.IsChecked = false;
                }
            }
        }

        private string[] GetSelectedTags()
        {
            var result = new List<string>();
            for (var i = 0; i < TagsPanel.Children.Count; i++)
            {
                var tag = TagsPanel.Children[i] as CheckBox;
                if (tag.IsChecked.Value)
                {
                    result.Add(tag.Content?.ToString());
                }
            }

            return result.ToArray();
        }

        private bool VerifyMod()
        {
            if (SelectedMod == null)
            {
                return false;
            }

            //-- Making sure to always reload
            SelectedMod = Mod.LoadModFromJson(SelectedMod.path);

            var problem = RainWorldSteamManager.ValidateWorkshopModForProblems(SelectedMod);
            if (!string.IsNullOrEmpty(problem))
            {
                MessageBox.Show(problem);
                return false;
            }

            return true;
        }

        public void SaveWorkshopData(bool newId = false)
        {
            if (SelectedMod != null && SelectedMod.WorkshopData != null && !string.IsNullOrEmpty(SelectedMod.path))
            {
                SelectedMod.WorkshopData.Title = TitleBox.Text;
                SelectedMod.WorkshopData.ID = IDBox.Text;
                SelectedMod.WorkshopData.Version = VersionBox.Text;
                SelectedMod.WorkshopData.Description = DescriptionBox.Text;

                SelectedMod.WorkshopData.Requirements = RequirementsBox.Text;
                SelectedMod.WorkshopData.RequirementNames = RequirementNamesBox.Text;
                SelectedMod.WorkshopData.Authors = AuthorBox.Text;
                SelectedMod.WorkshopData.TargetGameVersion = TargetVersionBox.Text;

                if (newId)
                {
                    WorkshopIDBox.Text = SelectedMod.WorkshopData.WorkshopID.ToString();
                }
                else
                {
                    if (!string.IsNullOrEmpty(WorkshopIDBox.Text))
                    {
                        SelectedMod.WorkshopData.WorkshopID = ulong.Parse(WorkshopIDBox.Text);
                    }
                }
                SelectedMod.WorkshopData.Visibility = VisibilityBox.Text;

                SelectedMod.WorkshopData.Tags = GetSelectedTags();

                SelectedMod.WorkshopData.UploadFilesOnly = UploadFilesOnly.IsChecked;

                var json = JsonSerializer.Serialize(SelectedMod.WorkshopData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SelectedMod.path + Path.DirectorySeparatorChar.ToString() + "workshopdata.json", json);
            }
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            SaveWorkshopData();
            if (VerifyMod())
            {
                MessageBox.Show("Everything is OK!");
            }
        }

        private void SaveWorkshopData_Click(object sender, RoutedEventArgs e)
        {
            SaveWorkshopData();
        }
        
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This will save the current data to workshopdata.json and upload the mod to the workshop.\r\nContinue?", "Are you sure?", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            SetTitle("Saving Workshop Data");

            IsEnabled = false;

            SaveWorkshopData();

            if (!VerifyMod()) {
                return;
            }

            Manager.FindWorkshopItemsWithKeyValue("id", SelectedMod.WorkshopData.ID);
        }

        public void SetTitle(string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Title = "Rain World Workshop Uploader - " + title;
            }
            else
            {
                Title = "Rain World Workshop Uploader";
            }
        }
        
        public void Error(string message)
        {
            MessageBox.Show(message, "Error");
            IsEnabled = true;
            ThumbnailOnly = false;
        }

        public static bool ThumbnailOnly;
        private void UploadThumbnail_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This will save the current data to workshopdata.json and upload only the thumbnail to the workshop.\r\nContinue?", "Are you sure?", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            SetTitle("Saving Workshop Data");

            IsEnabled = false;

            SaveWorkshopData();

            if (!VerifyMod())
            {
                return;
            }

            ThumbnailOnly = true;
            Manager.FindWorkshopItemsWithKeyValue("id", SelectedMod.WorkshopData.ID);
        }
    }
}
