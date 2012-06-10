// ==========================================================================
// Copyright (c) 2011, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.IO;
using System.Xml;
using System.Globalization;

namespace Gpx
{
    public class GpxWriter : IDisposable
    {
        private static readonly string GPX_NAMESPACE = "http://www.topografix.com/GPX/1/1";
        private static readonly string GPX_VERSION = "1.1";
        private static readonly string GPX_CREATOR = "http://dlg.krakow.pl/gpx";
        private static readonly string GARMIN_EXTENSIONS_NAMESPACE = "http://www.garmin.com/xmlschemas/GpxExtensions/v3";
        private static readonly string GARMIN_EXTENSIONS_PREFIX = "gpxx";

        private XmlWriter Writer_;

        public GpxWriter(Stream stream)
        {
            Writer_ = XmlWriter.Create(stream, new XmlWriterSettings { CloseOutput = true, Indent = true });
            Writer_.WriteStartDocument(false);
            Writer_.WriteStartElement("gpx", GPX_NAMESPACE);
            Writer_.WriteAttributeString("version", GPX_VERSION);
            Writer_.WriteAttributeString("creator", GPX_CREATOR);
            Writer_.WriteAttributeString("xmlns", GARMIN_EXTENSIONS_PREFIX, null, GARMIN_EXTENSIONS_NAMESPACE);
        }

        public void WriteMetadata(GpxMetadata metadata)
        {
            Writer_.WriteStartElement("metadata");

            if (!string.IsNullOrWhiteSpace(metadata.Name)) Writer_.WriteElementString("name", metadata.Name);
            if (!string.IsNullOrWhiteSpace(metadata.Description)) Writer_.WriteElementString("desc", metadata.Description);
            if (metadata.Author != null) WritePerson("author", metadata.Author);
            if (metadata.Copyright != null) WriteCopyright("copyright", metadata.Copyright);
            if (metadata.Link != null) WriteLink("link", metadata.Link);
            if (metadata.Time != default(DateTime)) Writer_.WriteElementString("time", ToGpxDateString(metadata.Time));
            if (!string.IsNullOrWhiteSpace(metadata.Keywords)) Writer_.WriteElementString("keywords", metadata.Keywords);
            if (metadata.Bounds != null) WriteBounds("bounds", metadata.Bounds);

            Writer_.WriteEndElement();
        }

        public void WriteRoute(GpxRoute route)
        {
            Writer_.WriteStartElement("rte");

            WriteTrackOrRoute(route);

            foreach (GpxRoutePoint routePoint in route.RoutePoints)
            {
                WriteRoutePoint("rtept", routePoint);
            }

            Writer_.WriteEndElement();
        }

        public void WriteTrack(GpxTrack track)
        {
            Writer_.WriteStartElement("trk");

            WriteTrackOrRoute(track);

            foreach (GpxTrackSegment segment in track.Segments)
            {
                WriteTrackSegment("trkseg", segment);
            }

            Writer_.WriteEndElement();
        }

        private void WriteTrackOrRoute(GpxTrackOrRoute trackOrRoute)
        {
            if (!string.IsNullOrEmpty(trackOrRoute.Name)) Writer_.WriteElementString("name", trackOrRoute.Name);
            if (!string.IsNullOrEmpty(trackOrRoute.Comment)) Writer_.WriteElementString("cmt", trackOrRoute.Comment);
            if (!string.IsNullOrEmpty(trackOrRoute.Description)) Writer_.WriteElementString("desc", trackOrRoute.Description);
            if (!string.IsNullOrEmpty(trackOrRoute.Source)) Writer_.WriteElementString("src", trackOrRoute.Source);

            foreach (GpxLink link in trackOrRoute.Links)
            {
                WriteLink("link", link);
            }

            if (trackOrRoute.Number != default(int)) Writer_.WriteElementString("number", trackOrRoute.Number.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(trackOrRoute.Type)) Writer_.WriteElementString("type", trackOrRoute.Type);

            if (trackOrRoute.HasExtensions)
            {
                Writer_.WriteStartElement("extensions");
                Writer_.WriteStartElement("RouteExtension", GARMIN_EXTENSIONS_NAMESPACE);

                Writer_.WriteElementString("DisplayColor", GARMIN_EXTENSIONS_NAMESPACE, trackOrRoute.DisplayColor.ToString());

                Writer_.WriteEndElement();
                Writer_.WriteEndElement();
            }
        }

        private void WriteTrackSegment(string elementName, GpxTrackSegment segment)
        {
            Writer_.WriteStartElement(elementName);

            foreach (GpxTrackPoint trackPoint in segment.TrackPoints)
            {
                WriteTrackPoint("trkpt", trackPoint);
            }

            Writer_.WriteEndElement();
        }

        public void WriteWayPoint(GpxWayPoint wayPoint)
        {
            Writer_.WriteStartElement("wpt");

            WriteWayOrRoutePoint(wayPoint);

            if (wayPoint.HasWayPointExtensions)
            {
                Writer_.WriteStartElement("extensions");
                Writer_.WriteStartElement("WaypointExtension", GARMIN_EXTENSIONS_NAMESPACE);

                if (wayPoint.Address != null) WriteAddress("Address", wayPoint.Address);

                foreach (GpxPhone phone in wayPoint.Phones)
                {
                    WritePhone("PhoneNumber", phone);
                }

                Writer_.WriteEndElement();
                Writer_.WriteEndElement();
            }

            Writer_.WriteEndElement();
        }

        private void WriteRoutePoint(string elementName, GpxRoutePoint routePoint)
        {
            Writer_.WriteStartElement(elementName);
            
            WriteWayOrRoutePoint(routePoint);

            if (routePoint.HasWayPointExtensions || routePoint.HasRoutePointExtensions)
            {
                Writer_.WriteStartElement("extensions");

                if (routePoint.HasWayPointExtensions)
                {
                    Writer_.WriteStartElement("WaypointExtension", GARMIN_EXTENSIONS_NAMESPACE);

                    if (routePoint.Address != null) WriteAddress("Address", routePoint.Address);

                    foreach (GpxPhone phone in routePoint.Phones)
                    {
                        WritePhone("PhoneNumber", phone);
                    }

                    Writer_.WriteEndElement();
                }

                if (routePoint.HasRoutePointExtensions)
                {
                    Writer_.WriteStartElement("RoutePointExtension", GARMIN_EXTENSIONS_NAMESPACE);

                    foreach (GpxPoint point in routePoint.RoutePoints)
                    {
                        WriteSubPoint("rpt", point);
                    }

                    Writer_.WriteEndElement();
                }

                Writer_.WriteEndElement();
            }

            Writer_.WriteEndElement();
        }

        private void WriteWayOrRoutePoint(GpxWayPoint wayPoint)
        {
            Writer_.WriteAttributeString("lat", wayPoint.Latitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("lon", wayPoint.Longitude.ToString(CultureInfo.InvariantCulture));
            if (wayPoint.Elevation != default(double)) Writer_.WriteElementString("ele", wayPoint.Elevation.ToString(CultureInfo.InvariantCulture));
            if (wayPoint.Time != default(DateTime)) Writer_.WriteElementString("time", ToGpxDateString(wayPoint.Time));
            if (!string.IsNullOrWhiteSpace(wayPoint.Name)) Writer_.WriteElementString("name", wayPoint.Name);
            if (!string.IsNullOrWhiteSpace(wayPoint.Comment)) Writer_.WriteElementString("cmt", wayPoint.Comment);
            if (!string.IsNullOrWhiteSpace(wayPoint.Description)) Writer_.WriteElementString("desc", wayPoint.Description);
            if (!string.IsNullOrWhiteSpace(wayPoint.Source)) Writer_.WriteElementString("src", wayPoint.Source);

            foreach (GpxLink link in wayPoint.Links)
            {
                WriteLink("link", link);
            }

            if (!string.IsNullOrWhiteSpace(wayPoint.Symbol)) Writer_.WriteElementString("sym", wayPoint.Symbol);
            if (!string.IsNullOrWhiteSpace(wayPoint.Type)) Writer_.WriteElementString("type", wayPoint.Type);

        }

        private void WriteTrackPoint(string elementName, GpxTrackPoint trackPoint)
        {
            Writer_.WriteStartElement(elementName);

            Writer_.WriteAttributeString("lat", trackPoint.Latitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("lon", trackPoint.Longitude.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.Elevation != default(double)) Writer_.WriteElementString("ele", trackPoint.Elevation.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.Time != default(DateTime)) Writer_.WriteElementString("time", ToGpxDateString(trackPoint.Time));
            if (trackPoint.MagneticVar != default(double)) Writer_.WriteElementString("magvar", trackPoint.MagneticVar.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.GeoidHeight != default(double)) Writer_.WriteElementString("geoidheight", trackPoint.GeoidHeight.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(trackPoint.FixType)) Writer_.WriteElementString("fix", trackPoint.FixType);
            if (trackPoint.Satelites != default(int)) Writer_.WriteElementString("sat", trackPoint.Satelites.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.Hdop != default(double)) Writer_.WriteElementString("hdop", trackPoint.Hdop.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.Vdop != default(double)) Writer_.WriteElementString("vdop", trackPoint.Vdop.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.Pdop != default(double)) Writer_.WriteElementString("pdop", trackPoint.Pdop.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.AgeOfData != default(double)) Writer_.WriteElementString("ageofdgpsdata", trackPoint.AgeOfData.ToString(CultureInfo.InvariantCulture));
            if (trackPoint.DgpsId != default(int)) Writer_.WriteElementString("dgpsid", trackPoint.DgpsId.ToString(CultureInfo.InvariantCulture));

            Writer_.WriteEndElement();
        }

        private void WriteSubPoint(string elementName, GpxPoint point)
        {
            Writer_.WriteStartElement(elementName, GARMIN_EXTENSIONS_NAMESPACE);

            Writer_.WriteAttributeString("lat", point.Latitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("lon", point.Longitude.ToString(CultureInfo.InvariantCulture));

            Writer_.WriteEndElement();
        }

        private void WritePerson(string elementName, GpxPerson person)
        {
            Writer_.WriteStartElement(elementName);

            if (!string.IsNullOrWhiteSpace(person.Name)) Writer_.WriteElementString("name", person.Name);
            if (person.Email != null) WriteEmail("email", person.Email);

            Writer_.WriteEndElement();
        }

        private void WriteEmail(string elementName, GpxEmail email)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("id", email.Id);
            Writer_.WriteAttributeString("domain", email.Domain);
            Writer_.WriteEndElement();
        }

        private void WriteLink(string elementName, GpxLink link)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("href", link.Href.ToString());
            if (!string.IsNullOrWhiteSpace(link.Text)) Writer_.WriteElementString("text", link.Text);
            if (!string.IsNullOrWhiteSpace(link.MimeType)) Writer_.WriteElementString("type", link.MimeType);
            Writer_.WriteEndElement();
        }

        private void WriteCopyright(string elementName, GpxCopyright copyright)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("author", copyright.Author);
            if (copyright.Year != default(int)) Writer_.WriteElementString("year", copyright.Year.ToString());
            if (copyright.Licence != null) Writer_.WriteElementString("licence", copyright.Licence.ToString());
            Writer_.WriteEndElement();
        }

        private void WriteAddress(string elementName, GpxAddress address)
        {
            Writer_.WriteStartElement(elementName, GARMIN_EXTENSIONS_NAMESPACE);
            if (!string.IsNullOrEmpty(address.StreetAddress)) Writer_.WriteElementString("StreetAddress", GARMIN_EXTENSIONS_NAMESPACE, address.StreetAddress);
            if (!string.IsNullOrEmpty(address.City)) Writer_.WriteElementString("City", GARMIN_EXTENSIONS_NAMESPACE, address.City);
            if (!string.IsNullOrEmpty(address.State)) Writer_.WriteElementString("State", GARMIN_EXTENSIONS_NAMESPACE, address.State);
            if (!string.IsNullOrEmpty(address.Country)) Writer_.WriteElementString("Country", GARMIN_EXTENSIONS_NAMESPACE, address.Country);
            if (!string.IsNullOrEmpty(address.PostalCode)) Writer_.WriteElementString("PostalCode", GARMIN_EXTENSIONS_NAMESPACE, address.PostalCode);
            Writer_.WriteEndElement();
        }

        private void WritePhone(string elementName, GpxPhone phone)
        {
            Writer_.WriteStartElement(elementName, GARMIN_EXTENSIONS_NAMESPACE);
            if (!string.IsNullOrEmpty(phone.Category)) Writer_.WriteAttributeString("Category", phone.Category);
            Writer_.WriteString(phone.Number);
            Writer_.WriteEndElement();
        }

        private void WriteBounds(string elementName, GpxBounds bounds)
        {
            Writer_.WriteStartElement(elementName);
            Writer_.WriteAttributeString("minlat", bounds.MinLatitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("minlon", bounds.MinLongitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("maxlat", bounds.MaxLatitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteAttributeString("maxlon", bounds.MaxLongitude.ToString(CultureInfo.InvariantCulture));
            Writer_.WriteEndElement();
        }

        private static string ToGpxDateString(DateTime date)
        {
            return string.Format("{0:D4}-{1:D2}-{2:D2}T{3:D2}:{4:D2}:{5:D2}", date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        }

        public void Dispose()
        {
            Writer_.WriteEndElement();
            Writer_.Close();
        }
    }
}