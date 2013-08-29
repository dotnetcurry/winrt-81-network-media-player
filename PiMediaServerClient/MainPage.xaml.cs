using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Search;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PiMediaServerClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IReadOnlyList<StorageFolder> MediaServers { get; set; }
        IReadOnlyList<StorageFolder> MediaFolders { get; set; }
        IReadOnlyList<StorageFile> MediaFiles { get; set; }
        StorageFile CurrentMediaFile { get; set; }

        Stack<StorageFolder> PreviousFolders { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            PreviousFolders = new Stack<StorageFolder>();
            this.InitilizeMediaServers();
        }

        async private void InitilizeMediaServers()
        {
            try
            {
                AvailableMediaDevices.Items.Clear();
                MediaServers = await KnownFolders.MediaServerDevices.GetFoldersAsync();

                if (MediaServers.Count == 0)
                {
                    MediaTitle.Text = "No MediaServers found";
                }
                else
                {
                    foreach (StorageFolder server in MediaServers)
                    {
                        AvailableMediaDevices.Items.Add(server.DisplayName);
                    }
                    MediaTitle.Text = "Media Servers refreshed";
                }
            }
            catch (Exception ex)
            {
                MediaTitle.Text = "Error querying Media Servers :" + ex.Message;
            }
        }

        private void AvailableMediaDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Player.Stop();
            MediaList.Items.Clear();

            if (AvailableMediaDevices.SelectedIndex != -1)
            {
                MediaTitle.Text = "Retrieving media files ...";
                Back.Content = "<< " + MediaServers[AvailableMediaDevices.SelectedIndex].DisplayName;
                PreviousFolders.Push(MediaServers[AvailableMediaDevices.SelectedIndex]);
                LoadMediaFiles(MediaServers[AvailableMediaDevices.SelectedIndex]);
            }
        }

        async private void LoadMediaFiles(StorageFolder mediaServerFolder)
        {
            try
            {
                MediaFolders = await mediaServerFolder.GetFoldersAsync();
                MediaList.Items.Clear();
                if (MediaFolders.Count > 0)
                {
                    MediaList.Items.Clear();
                    foreach (StorageFolder folder in MediaFolders)
                    {
                        MediaList.Items.Add(" + " + folder.DisplayName);
                    }
                    MediaTitle.Text = "Media folders retrieved";
                }
                var queryOptions = new QueryOptions();

                var queryFolder = mediaServerFolder.CreateFileQueryWithOptions(queryOptions);
                MediaFiles = await queryFolder.GetFilesAsync();
                if (MediaFiles.Count > 0)
                {
                    foreach (StorageFile file in MediaFiles)
                    {
                        MediaList.Items.Add(file.DisplayName);
                    }
                    MediaTitle.Text = "Media files retrieved";
                }
            }
            catch (Exception ex)
            {
                MediaTitle.Text = "Error locating media files " + ex.Message;
            }
        }

        private async void MediaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Player.Stop();

                if (MediaList.SelectedIndex != -1 && MediaList.SelectedIndex < MediaFolders.Count && MediaFolders.Count != 0)
                {
                    MediaTitle.Text = "Retrieving media files ...";
                    Back.Content = "<< " + MediaFolders[MediaList.SelectedIndex].DisplayName;
                    PreviousFolders.Push(MediaFolders[MediaList.SelectedIndex]);
                    LoadMediaFiles(MediaFolders[MediaList.SelectedIndex]);
                }
                else if (MediaList.SelectedIndex != -1 && (MediaList.SelectedIndex >= MediaFolders.Count &&
                    MediaList.SelectedIndex < (MediaFolders.Count + MediaFiles.Count)))
                {
                    CurrentMediaFile = MediaFiles[MediaList.SelectedIndex - MediaFolders.Count];
                    var stream = await CurrentMediaFile.OpenAsync(FileAccessMode.Read);
                    Player.SetSource(stream, CurrentMediaFile.ContentType);
                    Player.Play();
                    MediaTitle.Text = "Playing: " + CurrentMediaFile.DisplayName;
                }
            }
            catch (Exception ecp)
            {
                MediaTitle.Text = "Error during file selection :" + ecp.Message;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (PreviousFolders.Count > 1)
            {
                PreviousFolders.Pop();
                LoadMediaFiles(PreviousFolders.Peek());
            }
        }
    }
}
