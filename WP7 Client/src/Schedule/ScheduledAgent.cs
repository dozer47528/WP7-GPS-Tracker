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

namespace Schedule
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;
        public const string PERIODICTASK_NAME = "GPS Tracker";

        private FileService fileService;
        protected FileService FileService { get { return fileService ?? (fileService = new FileService()); } }

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
            if (IsolatedStorageSettings.ApplicationSettings.Contains("IsLocating"))
            {
                var isLocating = (bool)IsolatedStorageSettings.ApplicationSettings["IsLocating"];
                if (isLocating) return;
            }
            if (task.Name == PERIODICTASK_NAME)
            {
                var count = FileService.SaveFile();
                ShellTileService.Start(count);
            }
            ScheduledActionService.LaunchForTest(PERIODICTASK_NAME, TimeSpan.FromSeconds(300));
            NotifyComplete();
        }
    }
}