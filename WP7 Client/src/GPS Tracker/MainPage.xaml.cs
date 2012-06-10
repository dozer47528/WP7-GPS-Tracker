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

namespace GPS_Tracker
{
    public partial class MainPage : PhoneApplicationPage
    {
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

            var appBarName = string.Concat("ApplicationBar_", pivotItem.Name);
            var appBar = Resources[appBarName] as ApplicationBar;
            if (appBar == null) return;

            ApplicationBar = appBar;
        }
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitButton();
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
            (ApplicationBar_Track.Buttons[1] as ApplicationBarIconButton).IsEnabled = true;
            button.Text = "Start";
            button.IconUri = new Uri("/Images/appbar.play.png", UriKind.Relative);
        }
        private void InitStopButton(ApplicationBarIconButton button)
        {
            (ApplicationBar_Track.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;
            button.Text = "Stop";
            button.IconUri = new Uri("/Images/appbar.pause.png", UriKind.Relative);
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            ProgressBar_Downloading.Visibility = Visibility.Visible;
            (ApplicationBar_Track.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;

            IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream input = new IsolatedStorageFileStream("record.gpx", System.IO.FileMode.OpenOrCreate, FileAccess.Read, isStore);
            StreamReader sm = new StreamReader(input);
            var content = sm.ReadToEnd();
            if (string.IsNullOrEmpty(content)) return;

            var postString = "content=" + Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(content)));
            PostClient client = new PostClient(postString);
            client.DownloadStringCompleted += new PostClient.DownloadStringCompletedHandler(client_DownloadStringCompleted);
            client.DownloadStringAsync(new Uri("http://1010c.v2.ipc.la/"));
        }
        void client_DownloadStringCompleted(object sender, WindowsPhonePostClient.DownloadStringCompletedEventArgs e)
        {
            ProgressBar_Downloading.Visibility = Visibility.Collapsed;
            (ApplicationBar_Track.Buttons[1] as ApplicationBarIconButton).IsEnabled = true;


            EmailComposeTask mail = new EmailComposeTask();
            mail.Subject = string.Concat(DateTime.Now.ToString("[yyyy-MM-dd hh:mm]"), "Download the GPX File!");
            mail.Body = e.Result;
            mail.Show();
        }
    }
}