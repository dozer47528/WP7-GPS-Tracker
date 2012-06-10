using System.Windows;
using Microsoft.Phone.Scheduler;
using System.Device.Location;
using Microsoft.Phone.Shell;
using System;
using System.Linq;
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using Gpx;

namespace Schedule
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent 构造函数，初始化 UnhandledException 处理程序
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // 订阅托管的异常处理程序
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// 出现未处理的异常时执行的代码
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // 出现未处理的异常；强行进入调试器
                System.Diagnostics.Debugger.Break();
            }
        }

        public static string PERIODICTASKNAME = "GPS Tracker";
        /// <summary>
        /// 运行计划任务的代理
        /// </summary>
        /// <param name="task">
        /// 调用的任务
        /// </param>
        /// <remarks>
        /// 调用定期或资源密集型任务时调用此方法
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            if (task.Name == PERIODICTASKNAME)
            {

                GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                watcher.Start();

                var count = 0;
                bool firstRun = true;
                IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication();
                if (isStore.FileExists("record.gpx.temp"))
                {
                    isStore.DeleteFile("record.gpx.temp");
                }
                if (isStore.FileExists("record.gpx"))
                {
                    firstRun = false;
                    isStore.MoveFile("record.gpx", "record.gpx.temp");
                }


                IsolatedStorageFileStream input = new IsolatedStorageFileStream("record.gpx.temp", System.IO.FileMode.OpenOrCreate, FileAccess.Read, isStore);
                IsolatedStorageFileStream output = new IsolatedStorageFileStream("record.gpx", System.IO.FileMode.OpenOrCreate, FileAccess.Write, isStore);


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




                ShellTile firstTile = ShellTile.ActiveTiles.First();
                var newData = new StandardTileData()
                {
                    Title = "Running...",
                    BackgroundImage = new Uri("background.png", UriKind.Relative),
                    Count = count,
                    BackBackgroundImage = new Uri("background.png", UriKind.Relative),
                    BackTitle = watcher.Position.Timestamp.ToString(),
                    BackContent = watcher.Position.Location.Latitude + " " + watcher.Position.Location.Longitude
                };
                firstTile.Update(newData);
            }
            ScheduledActionService.LaunchForTest(PERIODICTASKNAME, TimeSpan.FromSeconds(300));
            NotifyComplete();
        }

        public static bool CheckTask()
        {
            PeriodicTask tskPeriodic;
            ScheduledAction tTask = ScheduledActionService.Find(PERIODICTASKNAME);
            if (tTask == null) return false;

            tskPeriodic = tTask as PeriodicTask;
            if (tskPeriodic == null) return false;

            return tskPeriodic.IsScheduled;
        }
        public static void StartPeriodicTask()
        {
            PeriodicTask tskPeriodic;
            ScheduledAction tTask = ScheduledActionService.Find(PERIODICTASKNAME);
            if (tTask != null)
            {
                tskPeriodic = tTask as PeriodicTask;
            }
            else
            {
                tskPeriodic = new PeriodicTask(PERIODICTASKNAME);
                tskPeriodic.Description = "GPS Tracker";
            }

            if (!tskPeriodic.IsScheduled)
            {
                ScheduledActionService.Add(tskPeriodic);
                ScheduledActionService.LaunchForTest(PERIODICTASKNAME, TimeSpan.FromSeconds(1));
            }

            ShellTile firstTile = ShellTile.ActiveTiles.First();
            var newData = new StandardTileData()
            {
                Title = "Running...",
                BackgroundImage = new Uri("background.png", UriKind.Relative),
            };
            firstTile.Update(newData);
        }
        public static void StopPeriodicTask()
        {
            ScheduledActionService.Remove(PERIODICTASKNAME);

            ShellTile firstTile = ShellTile.ActiveTiles.First();
            var newData = new StandardTileData()
            {
                Title = "Not Running",
                BackgroundImage = new Uri("background.png", UriKind.Relative),
            };
            firstTile.Update(newData);
        }
    }
}