using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using WindowsPhonePostClient;
using Microsoft.Phone.Tasks;
using Schedule;
using Service;

namespace GPS_Tracker
{
    public partial class MainPage : PhoneApplicationPage
    {
        private FileService fileService;
        protected FileService FileService { get { return fileService ?? (fileService = new FileService()); } }
        protected ApplicationBar ApplicationBar_Track
        {
            get
            {
                return Resources["ApplicationBar_Track"] as ApplicationBar;
            }
        }
        public MainPage()
        {
            InitializeComponent();
        }
        private void Pivot_LoadedPivotItem(object sender, PivotItemEventArgs e)
        {
            var pivot = sender as Pivot;
            if (pivot == null) return;
            var pivotItem = pivot.SelectedItem as PivotItem;
            if (pivotItem == null) return;

            var appBarName = "ApplicationBar_Track";



            if (e.Item == HistoryPivotItem)
            {
                appBarName = "ApplicationBar_History";
                LoadHistory();
            }


            var appBar = Resources[appBarName] as ApplicationBar;
            if (appBar == null) return;
            ApplicationBar = appBar;
        }
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitButton();
        }
        private void LoadHistory()
        {
            HistoryListBox.Items.Clear();
            foreach (var fileName in FileService.GetFileList())
            {
                var item = new ListBoxItem { Content = fileName };
                item.Hold += (object sender, System.Windows.Input.GestureEventArgs e) =>
                {
                    var name = (sender as ListBoxItem).Content.ToString();
                    ContextMenu menu = new ContextMenu();

                    MenuItem downloadButton = new MenuItem();
                    downloadButton.Header = "Download";
                    downloadButton.Click += (object s, RoutedEventArgs args) =>
                    {
                        ProgressBar_Downloading.Visibility = Visibility.Visible;
                        FileService.DownloadFile(name, DownloadStringCompleted);
                    };
                    menu.Items.Add(downloadButton);

                    MenuItem deleteButton = new MenuItem();
                    deleteButton.Header = "Delete";
                    deleteButton.Click += (object s, RoutedEventArgs args) =>
                    {
                        if (!FileService.DeleteFile(name))
                        {
                            MessageBox.Show("Delete failed!");
                        }
                        LoadHistory();
                    };
                    menu.Items.Add(deleteButton);
                    ContextMenuService.SetContextMenu(sender as DependencyObject, menu);
                };

                HistoryListBox.Items.Add(item);
            }
        }
        private void DownloadStringCompleted(object sender, WindowsPhonePostClient.DownloadStringCompletedEventArgs e)
        {
            ProgressBar_Downloading.Visibility = Visibility.Collapsed;
            EmailComposeTask mail = new EmailComposeTask();
            mail.Subject = string.Concat(DateTime.Now.ToString("[yyyy-MM-dd hh:mm]"), "Download the GPX File!");
            mail.Body = e.Result;
            mail.Show();
        }
        private void InitButton()
        {
            var button = ApplicationBar_Track.Buttons[0] as ApplicationBarIconButton;
            if (ScheduledAgent.CheckTask())
            {
                InitStopButton(button);
            }
            else
            {
                InitStartButton(button);
            }
        }
        private void TrackButton_Click(object sender, EventArgs e)
        {
            var button = sender as ApplicationBarIconButton;
            if (button.Text == "Start")
            {
                InitStopButton(button);

                var result = MessageBox.Show("OK:  yes\nCancel:  continue with last one", "Create new？", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK || !IsolatedStorageSettings.ApplicationSettings.Contains("LastTime"))
                {
                    IsolatedStorageSettings.ApplicationSettings["LastTime"] = DateTime.Now;
                }
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("Index"))
                {
                    IsolatedStorageSettings.ApplicationSettings["Index"] = 0;
                }
                if (result == MessageBoxResult.OK)
                {
                    var index = (int)IsolatedStorageSettings.ApplicationSettings["Index"] + 1;
                    IsolatedStorageSettings.ApplicationSettings["Index"] = index;
                }
                IsolatedStorageSettings.ApplicationSettings.Save();
                ScheduledAgent.StartPeriodicTask();
            }
            else
            {
                InitStartButton(button);
                ScheduledAgent.StopPeriodicTask();
            }
        }
        private void InitStartButton(ApplicationBarIconButton button)
        {
            button.Text = "Start";
            button.IconUri = new Uri("/Images/appbar.play.png", UriKind.Relative);
        }
        private void InitStopButton(ApplicationBarIconButton button)
        {
            button.Text = "Stop";
            button.IconUri = new Uri("/Images/appbar.pause.png", UriKind.Relative);
        }
    }
}