using System;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Service;
using Coding4Fun.Phone.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Collections.Generic;

namespace GPS_Tracker
{
    public partial class MainPage : PhoneApplicationPage
    {
        private BackgroundWorker Worker = new BackgroundWorker();
        private ImageSource[] TowerImages = new ImageSource[]{
            (ImageSource)new ImageSourceConverter().ConvertFromString("/Images/tower0.png"),
            (ImageSource)new ImageSourceConverter().ConvertFromString("/Images/tower1.png"),
            (ImageSource)new ImageSourceConverter().ConvertFromString("/Images/tower2.png")};
        private int TowerImageIndex = 0;


        private FileService fileService;
        protected FileService FileService { get { return fileService ?? (fileService = new FileService()); } }
        protected readonly GeoCoordinateWatcher Watcher = new GeoCoordinateWatcher();

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
            else if (e.Item == TrackPivotItem)
            {
                appBarName = "ApplicationBar_Track";
                LoadPoints();
            }


            var appBar = Resources[appBarName] as ApplicationBar;
            if (appBar == null) return;
            ApplicationBar = appBar;
        }

        private bool HasSetLocation = false;
        private void LoadPoints()
        {
            TrackMap.Children.Clear();
            if (ScheduledService.IsRunning)
            {
                var points = FileService.GetPoints();
                foreach (var p in points)
                {
                    if (double.IsNaN(p.Longitude) || double.IsNaN(p.Latitude)) continue;
                    TrackMap.Children.Add(new Pushpin
                    {
                        Name = Guid.NewGuid().ToString(),
                        Location = new GeoCoordinate(p.Latitude, p.Longitude),
                        Content = p.Time.ToLocalTime().ToString("HH:mm")
                    });
                }
            }

            if (HasSetLocation) return;
            if (TrackMap.Children.Count == 0)
            {
                TrackMap.SetView(Watcher.Position.Location, TrackMap.ZoomLevel);
            }
            else
            {
                var last = TrackMap.Children.Last() as Pushpin;
                TrackMap.SetView(last.Location, TrackMap.ZoomLevel);
            }
            HasSetLocation = true;
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitLocationNotice();
            InitButton();
            InitWorker();
            InitWatcher();
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
            if (e.Error != null)
            {
                Toast("Download Error!", "Pleace check your network or try it again!");
                return;
            }
            EmailComposeTask mail = new EmailComposeTask();
            mail.Subject = string.Concat(DateTime.Now.ToString("[yyyy-MM-dd hh:mm]"), "Download the GPX File!");
            mail.Body = e.Result;
            mail.Show();
        }
        private void InitButton()
        {
            var button = ApplicationBar_Track.Buttons[0] as ApplicationBarIconButton;
            if (ScheduledService.IsRunning)
            {
                InitStopButton(button);
            }
            else
            {
                InitStartButton(button);
            }
        }
        private void InitWorker()
        {
            Worker.WorkerReportsProgress = true;
            Worker.DoWork += (sender, e) =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    var s = sender as BackgroundWorker;
                    s.ReportProgress(0);
                }
            };
            Worker.ProgressChanged += (sender, e) =>
            {
                Image_Tower.Source = TowerImages[TowerImageIndex];
                TowerImageIndex++;
                if (TowerImageIndex > 2) TowerImageIndex = 0;
            };
        }
        private void InitWatcher()
        {
            Watcher.Start();
            Watcher.PositionChanged += (sender, e) =>
            {
                if ((DateTime.Now - e.Position.Timestamp.DateTime) > new TimeSpan(0, 10, 0) ||
                    double.IsNaN(e.Position.Location.Longitude) ||
                    double.IsNaN(e.Position.Location.Latitude))
                {
                    if (Worker.IsBusy) return;
                    Worker.RunWorkerAsync();
                }
                else
                {
                    if (Worker.IsBusy)
                    {
                        Worker.CancelAsync();
                    }
                    Image_Tower.Source = TowerImages[2];
                }
            };
        }
        private void InitLocationNotice()
        {
            var key = "LocationNotice";
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key)) return;

            var result = MessageBox.Show("The software will record your location information.In addition, when you choose to download, the software will uploaded  your location information to server and available for download.However, these data are anonymous, and no other information, and will be automatically deleted after a certain event.", "", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                IsolatedStorageSettings.ApplicationSettings.Add(key, true);
            }
        }
        private void TrackButton_Click(object sender, EventArgs e)
        {
            var button = sender as ApplicationBarIconButton;
            if (button.Text == "Start")
            {
                InitStopButton(button);
                var msg = new MessagePrompt
                {
                    Title = "",
                    Message = "Do you want to continue with last track or create new one?",
                };

                var createButton = new Button { Content = "Create" };
                createButton.Click += (s, args) =>
                {
                    msg.Hide();
                    CreateOrContinueResult(MessageBoxResult.OK);
                };
                var continueButton = new Button { Content = "Continue" };
                continueButton.Click += (s, args) =>
                {
                    msg.Hide();
                    CreateOrContinueResult(MessageBoxResult.Cancel);
                };

                msg.ActionPopUpButtons = new List<Button> { createButton, continueButton };
                msg.Show();

                //var result = MessageBox.Show("OK:  yes\nCancel:  continue with last one", "Create new？", MessageBoxButton.OKCancel);
                //CreateOrContinueResult(result);
            }
            else
            {
                InitStartButton(button);
                ScheduledService.StopPeriodicTask();
            }
        }

        private void CreateOrContinueResult(MessageBoxResult result)
        {
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

            var task = ScheduledService.StartPeriodicTask();
            ScheduledActionService.Add(task);
            ScheduledActionService.LaunchForTest(ScheduledService.PERIODICTASK_NAME, TimeSpan.FromSeconds(1));
            LoadPoints();
        }
        private void InitStartButton(ApplicationBarIconButton button)
        {
            button.Text = "Start";
            button.IconUri = new Uri("/Images/appbar.play.png", UriKind.Relative);
            (ApplicationBar_Track.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;
        }
        private void InitStopButton(ApplicationBarIconButton button)
        {
            button.Text = "Stop";
            button.IconUri = new Uri("/Images/appbar.pause.png", UriKind.Relative);
            (ApplicationBar_Track.Buttons[1] as ApplicationBarIconButton).IsEnabled = true;
        }
        private void AddManually_Click(object sender, EventArgs e)
        {
            if (double.IsNaN(Watcher.Position.Location.Longitude) || double.IsNaN(Watcher.Position.Location.Latitude))
            {
                Toast("GPS not ready!");
                return;
            }

            if (IsolatedStorageSettings.ApplicationSettings.Contains("LastLocation"))
            {
                var point = IsolatedStorageSettings.ApplicationSettings["LastLocation"] as Gpx.GpxWayPoint;
                if (point != null && point.Time.ToString() == Watcher.Position.Timestamp.UtcDateTime.ToString())
                {
                    Toast("No new position!");
                    return;
                }
            }

            IsolatedStorageSettings.ApplicationSettings["IsLocating"] = true;
            IsolatedStorageSettings.ApplicationSettings.Save();

            var count = FileService.SaveFile(Watcher.Position);

            IsolatedStorageSettings.ApplicationSettings["IsLocating"] = false;
            IsolatedStorageSettings.ApplicationSettings.Save();

            ShellTileService.Start(count);
            LoadPoints();

            Toast("Success!");
        }

        protected void Toast(string title, string message = null)
        {
            ToastPrompt toast = new ToastPrompt();
            toast.Title = title;
            if (!string.IsNullOrEmpty(message)) toast.Message = message;
            toast.Show();
        }
    }
}