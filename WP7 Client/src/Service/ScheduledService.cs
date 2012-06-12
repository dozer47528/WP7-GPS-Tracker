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
using Service;

namespace Service
{
    public static class ScheduledService
    {
        public const string PERIODICTASK_NAME = "GPS Tracker";
        public static PeriodicTask StartPeriodicTask()
        {
            if (ScheduledActionService.Find(PERIODICTASK_NAME) != null)
            {
                ScheduledActionService.Remove(PERIODICTASK_NAME);
            }
            var tskPeriodic = new PeriodicTask(PERIODICTASK_NAME);
            tskPeriodic.Description = "GPS Tracker";
            return tskPeriodic;
        }
        public static void StopPeriodicTask()
        {
            ScheduledActionService.Remove(PERIODICTASK_NAME);
            ShellTileService.Stop();
        }

        public static bool IsRunning
        {
            get
            {
                PeriodicTask tskPeriodic;
                ScheduledAction tTask = ScheduledActionService.Find(PERIODICTASK_NAME);
                if (tTask == null) return false;

                tskPeriodic = tTask as PeriodicTask;
                if (tskPeriodic == null) return false;

                return tskPeriodic.IsScheduled;
            }
        }
    }
}