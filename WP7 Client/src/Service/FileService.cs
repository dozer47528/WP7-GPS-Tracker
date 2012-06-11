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


        public int SaveFile(GeoCoordinateWatcher watcher)
        {
            var lastTime = (DateTime)IsolatedStorageSettings.ApplicationSettings["LastTime"];
            var index = IsolatedStorageSettings.ApplicationSettings["Index"];
            var fileName = string.Concat(lastTime.ToString("yyyy-MM-dd "), index.ToString(), ".gpx");

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

            IsolatedStorageFileStream input = new IsolatedStorageFileStream(tempFileName, System.IO.FileMode.OpenOrCreate, FileAccess.Read, isStore);
            IsolatedStorageFileStream output = new IsolatedStorageFileStream(fileName, System.IO.FileMode.OpenOrCreate, FileAccess.Write, isStore);


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

                if (last == null || last.Time.ToString() != watcher.Position.Timestamp.UtcDateTime.ToString())
                {
                    writer.WriteWayPoint(new GpxWayPoint
                    {
                        Latitude = watcher.Position.Location.Latitude,
                        Longitude = watcher.Position.Location.Longitude,
                        Elevation = watcher.Position.Location.Altitude,
                        Time = watcher.Position.Timestamp.UtcDateTime,
                    });
                    count++;
                }
            }
            return count;
        }
    }
}
