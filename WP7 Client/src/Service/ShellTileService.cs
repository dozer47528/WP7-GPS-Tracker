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
using Microsoft.Phone.Shell;
using System.Linq;

namespace Service
{
    public static class ShellTileService
    {
        public static void Start(int number)
        {
            ShellTile firstTile = ShellTile.ActiveTiles.First();
            var newData = new StandardTileData()
            {
                Title = "Running...",
                BackgroundImage = new Uri("background.png", UriKind.Relative),
                Count = number,
            };
            firstTile.Update(newData);
        }

        public static void Stop()
        {
            ShellTile firstTile = ShellTile.ActiveTiles.First();
            var newData = new StandardTileData()
            {
                Title = "GPS Tracker",
                BackgroundImage = new Uri("background.png", UriKind.Relative),
                Count = 0
            };
            firstTile.Update(newData);
        }
    }
}
