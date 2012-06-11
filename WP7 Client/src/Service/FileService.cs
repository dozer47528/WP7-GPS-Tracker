using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.IO;
using System.Text;
using WindowsPhonePostClient;
using System.Device.Location;
using Gpx;

namespace Service
{
    public class FileService
    {
        private IsolatedStorageFile file;
        protected IsolatedStorageFile File
        {
            get
            {
                return file ?? (file = IsolatedStorageFile.GetUserStoreForApplication());
            }
        }
        public string[] GetFileList()
        {
            return File.GetFileNames("*.gpx").Select(file => file.Substring(0, file.LastIndexOf('.'))).ToArray();
        }

        public void DownloadFile(string fileName, Action<object, WindowsPhonePostClient.DownloadStringCompletedEventArgs> onCompleted)
        {
            IsolatedStorageFileStream input = new IsolatedStorageFileStream(fileName + ".gpx", System.IO.FileMode.OpenOrCreate, FileAccess.Read, File);
            StreamReader sm = new StreamReader(input);
            var content = sm.ReadToEnd();
            if (string.IsNullOrEmpty(content)) return;

            var postString = "content=" + Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(content)));
            PostClient client = new PostClient(postString);
            client.DownloadStringCompleted += new PostClient.DownloadStringCompletedHandler(onCompleted);
            client.DownloadStringAsync(new Uri("http://1010c.v2.ipc.la/"));
        }

        public bool DeleteFile(string fileName)
        {
            fileName += ".gpx";
            try
            {
                File.DeleteFile(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int SaveFile(GeoPosition<GeoCoordinate> Position = null)
        {
            if (Position == null)
            {
                GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                watcher.Start();
                Position = watcher.Position;
            }

            var fileName = CurrentFileName();

            var tempFileName = "record.gpx.temp";
            var count = 0;
            bool firstRun = true;
            IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication();
            if (isStore.FileExists(tempFileName))
            {
                isStore.DeleteFile(tempFileName);
            }
            if (isStore.FileExists(fileName))
            {
                firstRun = false;
                isStore.MoveFile(fileName, tempFileName);
            }

            using (IsolatedStorageFileStream input = new IsolatedStorageFileStream(tempFileName, System.IO.FileMode.OpenOrCreate, FileAccess.Read, isStore))
            using (IsolatedStorageFileStream output = new IsolatedStorageFileStream(fileName, System.IO.FileMode.OpenOrCreate, FileAccess.Write, isStore))
            using (GpxWriter writer = new GpxWriter(output))
            {
                GpxWayPoint last = null;
                if (!firstRun)
                {
                    using (GpxReader reader = new GpxReader(input))
                    {
                        while (reader.Read())
                        {
                            switch (reader.ObjectType)
                            {
                                case GpxObjectType.WayPoint:
                                    count++;
                                    writer.WriteWayPoint(reader.WayPoint);
                                    last = reader.WayPoint;
                                    break;
                            }
                        }
                    }
                }

                IsolatedStorageSettings.ApplicationSettings["LastLocation"] = last;
                IsolatedStorageSettings.ApplicationSettings.Save();

                if (double.IsNaN(Position.Location.Latitude) || double.IsNaN(Position.Location.Longitude))
                {
                    return count;
                }
                if (last == null || last.Time.ToString() != Position.Timestamp.UtcDateTime.ToString())
                {
                    writer.WriteWayPoint(new GpxWayPoint
                    {
                        Latitude = Position.Location.Latitude,
                        Longitude = Position.Location.Longitude,
                        Elevation = Position.Location.Altitude,
                        Time = Position.Timestamp.UtcDateTime,
                    });
                    count++;
                }
            }
            return count;
        }

        private string CurrentFileName()
        {
            var lastTime = (DateTime)IsolatedStorageSettings.ApplicationSettings["LastTime"];
            var index = IsolatedStorageSettings.ApplicationSettings["Index"];
            var fileName = string.Format("[{0}]{1}.gpx", index.ToString(), lastTime.ToString("yyyy-MM-dd"));
            return fileName;
        }

        public List<GpxWayPoint> GetPoints()
        {
            var result = new List<GpxWayPoint>();
            var fileName = CurrentFileName();
            var tempFileName = "points.gpx.temp";

            IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication();
            if (isStore.FileExists(tempFileName))
            {
                isStore.DeleteFile(tempFileName);
            }

            if (isStore.FileExists(fileName))
            {
                isStore.CopyFile(fileName, tempFileName);
            }
            else
            {
                return result;
            }

            using (IsolatedStorageFileStream input = new IsolatedStorageFileStream(tempFileName, System.IO.FileMode.Open, FileAccess.Read, isStore))
            {
                using (GpxReader reader = new GpxReader(input))
                {
                    while (reader.Read())
                    {
                        switch (reader.ObjectType)
                        {
                            case GpxObjectType.WayPoint:
                                result.Add(reader.WayPoint);
                                break;
                        }
                    }
                }
            }
            return result;
        }
    }
}
