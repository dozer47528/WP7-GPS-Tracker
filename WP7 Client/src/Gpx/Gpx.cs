// ==========================================================================
// Copyright (c) 2011, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gpx
{
    public class GpxAttributes
    {
        public string Version { get; set; }
        public string Creator { get; set; }
    }

    public class GpxMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public GpxPerson Author { get; set; }
        public GpxCopyright Copyright { get; set; }
        public GpxLink Link { get; set; }
        public DateTime Time { get; set; }
        public string Keywords { get; set; }
        public GpxBounds Bounds { get; set; }
    }

    public class GpxPoint
    {
        private const double EARTH_RADIUS = 6371; // [km]
        private const double RADIAN = Math.PI / 180;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
        public DateTime Time { get; set; }

        public double GetDistnaceFrom(GpxPoint other)
        {
            double thisLatitude = this.Latitude;
            double otherLatitude = other.Latitude;
            double thisLongitude = this.Longitude;
            double otherLongitude = other.Longitude;

            double deltaLatitude = Math.Abs(this.Latitude - other.Latitude);
            double deltaLongitude = Math.Abs(this.Longitude - other.Longitude);

            thisLatitude *= RADIAN;
            otherLatitude *= RADIAN;
            deltaLongitude *= RADIAN;

            double cos = Math.Cos(deltaLongitude) * Math.Cos(thisLatitude) * Math.Cos(otherLatitude) +
                Math.Sin(thisLatitude) * Math.Sin(otherLatitude);

            return EARTH_RADIUS * Math.Acos(cos);
        }
    }

    public class GpxTrackPoint : GpxPoint
    {
        private class GpxGpsParameters
        {
            public double MagneticVar { get; set; }
            public double GeoidHeight { get; set; }
            public string FixType { get; set; }
            public int Satelites { get; set; }
            public double Hdop { get; set; }
            public double Vdop { get; set; }
            public double Pdop { get; set; }
            public double AgeOfData { get; set; }
            public int DgpsId { get; set; }
        }

        private GpxGpsParameters GpsParameters_ = null;

        public double MagneticVar
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.MagneticVar : default(double); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.MagneticVar = value;
            }
        }

        public double GeoidHeight
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.GeoidHeight : default(double); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.GeoidHeight = value;
            }
        }

        public string FixType
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.FixType : default(string); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.FixType = value;
            }
        }

        public int Satelites
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.Satelites : default(int); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.Satelites = value;
            }
        }

        public double Hdop
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.Hdop : default(double); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.Hdop = value;
            }
        }

        public double Vdop
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.Vdop : default(double); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.Vdop = value;
            }
        }

        public double Pdop
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.Pdop : default(double); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.Pdop = value;
            }
        }

        public double AgeOfData
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.AgeOfData : default(double); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.AgeOfData = value;
            }
        }

        public int DgpsId
        {
            get { return (GpsParameters_ != null) ? GpsParameters_.DgpsId : default(int); }
            set
            {
                if (GpsParameters_ == null) GpsParameters_ = new GpxGpsParameters();
                GpsParameters_.DgpsId = value;
            }
        }
    }

    public class GpxWayPoint : GpxPoint
    {
        private List<GpxLink> Links_ = new List<GpxLink>(0);
        private List<GpxPhone> Phones_ = new List<GpxPhone>(0);

        public string Name { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }

        public IList<GpxLink> Links
        {
            get { return Links_; }
        }

        // Extensions

        public GpxAddress Address { get; set; }

        public IList<GpxPhone> Phones
        {
            get { return Phones_; }
        }

        public bool HasWayPointExtensions
        {
            get { return Address != null || Phones.Count != 0; }
        }

        public GpxLink HttpLink
        {
            get
            {
                if (Links == null) return null;
                return Links.Where(l => l.Href.Scheme == Uri.UriSchemeHttp).FirstOrDefault();
            }
        }

        public GpxLink EmailLink
        {
            get
            {
                if (Links == null) return null;
                return Links.Where(l => l.Href.Scheme == Uri.UriSchemeMailto).FirstOrDefault();
            }
        }
    }

    public class GpxRoutePoint : GpxWayPoint
    {
        private List<GpxPoint> Points_ = new List<GpxPoint>(0);

        public IList<GpxPoint> RoutePoints
        {
            get { return Points_; }
        }

        public bool HasRoutePointExtensions
        {
            get { return RoutePoints.Count != 0; }
        }
    }

    public class GpxPointCollection<T> : IList<T> where T : GpxPoint
    {
        private List<T> Points_ = new List<T>();

        public GpxPoint AddPoint(T point)
        {
            Points_.Add(point);
            return point;
        }

        public T StartPoint
        {
            get { return (Points_.Count == 0) ? null : Points_[0]; }
        }

        public T EndPoint
        {
            get { return (Points_.Count == 0) ? null : Points_[Points_.Count - 1]; }
        }

        public double GetLength()
        {
            double result = 0;

            for (int i = 1; i < Points_.Count; i++)
            {
                double dist = Points_[i].GetDistnaceFrom(Points_[i - 1]);
                result += dist;
            }

            return result;
        }

        public double GetMinElevation()
        {
            return Points_.Select(p => p.Elevation).Min();
        }

        public double GetMaxElevation()
        {
            return Points_.Select(p => p.Elevation).Max();
        }

        public GpxPointCollection<GpxPoint> ToGpxPoints()
        {
            GpxPointCollection<GpxPoint> points = new GpxPointCollection<GpxPoint>();

            foreach (T gpxPoint in Points_)
            {
                GpxPoint point = new GpxPoint
                {
                    Longitude = gpxPoint.Longitude,
                    Latitude = gpxPoint.Latitude,
                    Elevation = gpxPoint.Elevation,
                    Time = gpxPoint.Time
                };

                points.Add(point);
            }

            return points;
        }

        public int Count
        {
            get { return Points_.Count; }
        }

        public int IndexOf(T item)
        {
            return Points_.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Points_.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Points_.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Points_[index]; }
            set { Points_[index] = value; }
        }

        public void Add(T item)
        {
            Points_.Add(item);
        }

        public void Clear()
        {
            Points_.Clear();
        }

        public bool Contains(T item)
        {
            return Points_.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Points_.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return Points_.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Points_.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public abstract class GpxTrackOrRoute
    {
        private List<GpxLink> Links_ = new List<GpxLink>(0);

        public string Name { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public int Number { get; set; }
        public string Type { get; set; }
        public GpxColor DisplayColor { get; set; }

        public IList<GpxLink> Links
        {
            get { return Links_; }
        }

        public bool HasExtensions
        {
            get { return DisplayColor != default(GpxColor); }
        }

        public abstract GpxPointCollection<GpxPoint> ToGpxPoints();
    }

    public class GpxRoute : GpxTrackOrRoute
    {
        private GpxPointCollection<GpxRoutePoint> RoutePoints_ = new GpxPointCollection<GpxRoutePoint>();

        public GpxPointCollection<GpxRoutePoint> RoutePoints
        {
            get { return RoutePoints_; }
        }

        public override GpxPointCollection<GpxPoint> ToGpxPoints()
        {
            GpxPointCollection<GpxPoint> points = new GpxPointCollection<GpxPoint>();

            foreach (GpxRoutePoint routePoint in RoutePoints_)
            {
                points.Add(routePoint);

                foreach (GpxPoint gpxPoint in routePoint.RoutePoints)
                {
                    points.Add(gpxPoint);
                }
            }

            return points;
        }
    }

    public class GpxTrack : GpxTrackOrRoute
    {
        private List<GpxTrackSegment> Segments_ = new List<GpxTrackSegment>(1);

        public IList<GpxTrackSegment> Segments
        {
            get { return Segments_; }
        }

        public override GpxPointCollection<GpxPoint> ToGpxPoints()
        {
            GpxPointCollection<GpxPoint> points = new GpxPointCollection<GpxPoint>();

            foreach (GpxTrackSegment segment in Segments_)
            {
                GpxPointCollection<GpxPoint> segmentPoints = segment.TrackPoints.ToGpxPoints();

                foreach (GpxPoint point in segmentPoints)
                {
                    points.Add(point);
                }
            }

            return points;
        }
    }

    public class GpxTrackSegment
    {
        GpxPointCollection<GpxTrackPoint> TrackPoints_ = new GpxPointCollection<GpxTrackPoint>();

        public GpxPointCollection<GpxTrackPoint> TrackPoints
        {
            get { return TrackPoints_; }
        }
    }

    public class GpxLink
    {
        public Uri Href { get; set; }
        public string Text { get; set; }
        public string MimeType { get; set; }
    }

    public class GpxEmail
    {
        public string Id { get; set; }
        public string Domain { get; set; }
    }

    public class GpxAddress
    {
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
    }

    public class GpxPhone
    {
        public string Number { get; set; }
        public string Category { get; set; }
    }

    public class GpxPerson
    {
        public string Name { get; set; }
        public GpxEmail Email { get; set; }
        public GpxLink Link { get; set; }
    }

    public class GpxCopyright
    {
        public string Author { get; set; }
        public int Year { get; set; }
        public Uri Licence { get; set; }
    }

    public class GpxBounds
    {
        public double MinLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MaxLongitude { get; set; }
    }

    public enum GpxColor : uint
    {
        Black = 0xff000000,
        DarkRed = 0xff8b0000,
        DarkGreen = 0xff008b00,
        DarkYellow = 0x8b8b0000,
        DarkBlue = 0Xff00008b,
        DarkMagenta = 0xff8b008b,
        DarkCyan = 0xff008b8b,
        LightGray = 0xffd3d3d3,
        DarkGray = 0xffa9a9a9,
        Red = 0xffff0000,
        Green = 0xff00b000,
        Yellow = 0xffffff00,
        Blue = 0xff0000ff,
        Magenta = 0xffff00ff,
        Cyan = 0xff00ffff,
        White = 0xffffffff,
        Transparent = 0x00ffffff
    }
}