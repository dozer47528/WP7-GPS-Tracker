// ==========================================================================
// Copyright (c) 2011, dlg.krakow.pl
// All Rights Reserved
//
// NOTICE: dlg.krakow.pl permits you to use, modify, and distribute this file
// in accordance with the terms of the license agreement accompanying it.
// ==========================================================================

using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Gpx
{
    public enum GpxObjectType { None, Attributes, Metadata, WayPoint, Route, Track };

    public class GpxReader : IDisposable
    {
        private static readonly string GARMIN_EXTENSIONS = "http://www.garmin.com/xmlschemas/GpxExtensions/v3";

        private XmlReader Reader_;

        public GpxObjectType ObjectType { get; private set; }
        public GpxAttributes Attributes { get; private set; }
        public GpxMetadata Metadata { get; private set; }
        public GpxWayPoint WayPoint { get; private set; }
        public GpxRoute Route { get; private set; }
        public GpxTrack Track { get; private set; }

        public GpxReader(Stream stream)
        {
            Reader_ = XmlReader.Create(stream);

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:
                        if (Reader_.Name != "gpx") throw new FormatException(Reader_.Name);
                        Attributes = ReadGpxAttribures();
                        ObjectType = GpxObjectType.Attributes;
                        return;
                }
            }

            throw new FormatException();
        }

        public bool Read()
        {
            if (ObjectType == GpxObjectType.None) return false;

            while (Reader_.Read())
            {
                switch (Reader_.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (Reader_.Name)
                        {
                            case "metadata":
                                Metadata = ReadGpxMetadata(Reader_);
                                ObjectType = GpxObjectType.Metadata;
                                return true;
                            case "wpt":
                                WayPoint = ReadGpxWayPoint(Reader_);
                                ObjectType = GpxObjectType.WayPoint;
                                return true;
                            case "rte":
                                Route = ReadGpxRoute(Reader_);
                                ObjectType = GpxObjectType.Route;
                                return true;
                            case "trk":
                                Track = ReadGpxTrack(Reader_);
                                ObjectType = GpxObjectType.Track;
                                return true;
                            case "extensions":
                                ReadGpxExtensions(Reader_);
                                break;
                            default:
                                throw new FormatException(Reader_.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (Reader_.Name != "gpx") throw new FormatException(Reader_.Name);
                        ObjectType = GpxObjectType.None;
                        return false;
                }
            }

            ObjectType = GpxObjectType.None;
            return false;
        }

        private GpxAttributes ReadGpxAttribures()
        {
            GpxAttributes attributes = new GpxAttributes();
            
            while (Reader_.MoveToNextAttribute())
            {
                switch (Reader_.Name)
                {
                    case "version":
                        attributes.Version = Reader_.Value;
                        break;
                    case "creator":
                        attributes.Creator = Reader_.Value;
                        break;
                }
            }

            return attributes;
        }

        private GpxMetadata ReadGpxMetadata(XmlReader reader)
        {
            GpxMetadata metadata = new GpxMetadata();
            if (reader.IsEmptyElement) return metadata;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "name":
                                metadata.Name = ReadContentAsString(reader);
                                break;
                            case "desc":
                                metadata.Description = ReadContentAsString(reader);
                                break;
                            case "author":
                                metadata.Author = ReadGpxPerson(reader);
                                break;
                            case "copyright":
                                metadata.Copyright = ReadGpxCopyright(reader);
                                break;
                            case "link":
                                metadata.Link = ReadGpxLink(reader);
                                break;
                            case "time":
                                metadata.Time = ReadContentAsDateTime(reader);
                                break;
                            case "keywords":
                                metadata.Keywords = ReadContentAsString(reader);
                                break;
                            case "bounds":
                                ReadGpxBounds(reader);
                                break;
                            case "extensions":
                                ReadMetadataExtensions(reader, metadata);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return metadata;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxWayPoint ReadGpxWayPoint(XmlReader reader)
        {
            GpxWayPoint wayPoint = new GpxWayPoint();

            string elementName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "lat":
                        wayPoint.Latitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "lon":
                        wayPoint.Longitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }

            if (isEmptyElement) return wayPoint;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "ele":
                                wayPoint.Elevation = ReadContentAsDouble(reader);
                                break;
                            case "time":
                                wayPoint.Time = ReadContentAsDateTime(reader);
                                break;
                            case "name":
                                wayPoint.Name = ReadContentAsString(reader);
                                break;
                            case "cmt":
                                wayPoint.Comment = ReadContentAsString(reader);
                                break;
                            case "desc":
                                wayPoint.Description = ReadContentAsString(reader);
                                break;
                            case "src":
                                wayPoint.Source = ReadContentAsString(reader);
                                break;
                            case "link":
                                wayPoint.Links.Add(ReadGpxLink(reader));
                                break;
                            case "sym":
                                wayPoint.Symbol = ReadContentAsString(reader);
                                break;
                            case "type":
                                wayPoint.Type = ReadContentAsString(reader);
                                break;
                            case "extensions":
                                ReadWayPointExtensions(reader, wayPoint);
                                break;
                            case "magvar":
                            case "geoidheight":
                            case "fix":
                            case "sat":
                            case "hdop":
                            case "vdop":
                            case "pdop":
                            case "ageofdgpsdata":
                            case "dgpsid":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return wayPoint;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxRoute ReadGpxRoute(XmlReader reader)
        {
            GpxRoute route = new GpxRoute();
            if (reader.IsEmptyElement) return route;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "name":
                                route.Name = ReadContentAsString(reader);
                                break;
                            case "cmt":
                                route.Comment = ReadContentAsString(reader);
                                break;
                            case "desc":
                                route.Description = ReadContentAsString(reader);
                                break;
                            case "src":
                                route.Source = ReadContentAsString(reader);
                                break;
                            case "link":
                                route.Links.Add(ReadGpxLink(reader));
                                break;
                            case "number":
                                route.Number = int.Parse(ReadContentAsString(reader));
                                break;
                            case "type":
                                route.Type = ReadContentAsString(reader);
                                break;
                            case "extensions":
                                ReadRouteExtensions(reader, route);
                                break;
                            case "rtept":
                                route.RoutePoints.Add(ReadGpxRoutePoint(reader));
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return route;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxRoutePoint ReadGpxRoutePoint(XmlReader reader)
        {
            GpxRoutePoint wayPoint = new GpxRoutePoint();

            string elementName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "lat":
                        wayPoint.Latitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "lon":
                        wayPoint.Longitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }

            if (isEmptyElement) return wayPoint;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "ele":
                                wayPoint.Elevation = ReadContentAsDouble(reader);
                                break;
                            case "time":
                                wayPoint.Time = ReadContentAsDateTime(reader);
                                break;
                            case "name":
                                wayPoint.Name = ReadContentAsString(reader);
                                break;
                            case "cmt":
                                wayPoint.Comment = ReadContentAsString(reader);
                                break;
                            case "desc":
                                wayPoint.Description = ReadContentAsString(reader);
                                break;
                            case "src":
                                wayPoint.Source = ReadContentAsString(reader);
                                break;
                            case "link":
                                wayPoint.Links.Add(ReadGpxLink(reader));
                                break;
                            case "sym":
                                wayPoint.Symbol = ReadContentAsString(reader);
                                break;
                            case "type":
                                wayPoint.Type = ReadContentAsString(reader);
                                break;
                            case "extensions":
                                ReadRoutePointExtensions(reader, wayPoint);
                                break;
                            case "magvar":
                            case "geoidheight":
                            case "fix":
                            case "sat":
                            case "hdop":
                            case "vdop":
                            case "pdop":
                            case "ageofdgpsdata":
                            case "dgpsid":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return wayPoint;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxTrack ReadGpxTrack(XmlReader reader)
        {
            GpxTrack track = new GpxTrack();
            if (reader.IsEmptyElement) return track;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "name":
                                track.Name = ReadContentAsString(reader);
                                break;
                            case "cmt":
                                track.Comment = ReadContentAsString(reader);
                                break;
                            case "desc":
                                track.Description = ReadContentAsString(reader);
                                break;
                            case "src":
                                track.Source = ReadContentAsString(reader);
                                break;
                            case "link":
                                track.Links.Add(ReadGpxLink(reader));
                                break;
                            case "number":
                                track.Number = int.Parse(ReadContentAsString(reader));
                                break;
                            case "type":
                                track.Type = ReadContentAsString(reader);
                                break;
                            case "extensions":
                                ReadTrackExtensions(reader, track);
                                break;
                            case "trkseg":
                                track.Segments.Add(ReadGpxTrackSegment(reader));
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return track;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxTrackSegment ReadGpxTrackSegment(XmlReader reader)
        {
            GpxTrackSegment segment = new GpxTrackSegment();
            if (reader.IsEmptyElement) return segment;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "trkpt":
                                segment.TrackPoints.Add(ReadGpxTrackPoint(reader));
                                break;
                            case "extensions":
                                ReadTrackSegmentExtensions(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return segment;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxTrackPoint ReadGpxTrackPoint(XmlReader reader)
        {
            GpxTrackPoint point = new GpxTrackPoint();

            string elementName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "lat":
                        point.Latitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "lon":
                        point.Longitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }

            if (isEmptyElement) return point;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "ele":
                                point.Elevation = ReadContentAsDouble(reader);
                                break;
                            case "time":
                                point.Time = ReadContentAsDateTime(reader);
                                break;
                            case "magvar":
                                point.MagneticVar = ReadContentAsDouble(reader);
                                break;
                            case "geoidheight":
                                point.GeoidHeight = ReadContentAsDouble(reader);
                                break;
                            case "fix":
                                point.FixType = ReadContentAsString(reader);
                                break;
                            case "sat":
                                point.Satelites = ReadContentAsInt(reader);
                                break;
                            case "hdop":
                                point.Hdop = ReadContentAsDouble(reader);
                                break;
                            case "vdop":
                                point.Vdop = ReadContentAsDouble(reader);
                                break;
                            case "pdop":
                                point.Pdop = ReadContentAsDouble(reader);
                                break;
                            case "ageofdgpsdata":
                                point.AgeOfData = ReadContentAsDouble(reader);
                                break;
                            case "dgpsid":
                                point.DgpsId = ReadContentAsInt(reader);
                                break;
                            case "extensions":
                                ReadTrackPointExtensions(reader);
                                break;
                            case "name":
                            case "cmt":
                            case "desc":
                            case "src":
                            case "link":
                            case "sym":
                            case "type":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return point;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxPerson ReadGpxPerson(XmlReader reader)
        {
            GpxPerson person = new GpxPerson();
            if (reader.IsEmptyElement) return person;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "name":
                                person.Name = ReadContentAsString(reader);
                                break;
                            case "email":
                                person.Email = ReadGpxEmail(reader);
                                break;
                            case "link":
                                person.Link = ReadGpxLink(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return person;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxEmail ReadGpxEmail(XmlReader reader)
        {
            GpxEmail email = new GpxEmail();
            if (reader.IsEmptyElement) return email;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "id":
                                email.Id = ReadContentAsString(reader);
                                break;
                            case "domain":
                                email.Domain = ReadContentAsString(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return email;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxLink ReadGpxLink(XmlReader reader)
        {
            GpxLink link = new GpxLink();

            string elementName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "href":
                        link.Href = new Uri(reader.Value);
                        break;
                }
            }

            if (isEmptyElement) return link;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "text":
                                link.Text = ReadContentAsString(reader);
                                break;
                            case "type":
                                link.MimeType = ReadContentAsString(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return link;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxCopyright ReadGpxCopyright(XmlReader reader)
        {
            GpxCopyright copyright = new GpxCopyright();

            string elementName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "author":
                        copyright.Author = reader.Value;
                        break;
                }
            }

            if (isEmptyElement) return copyright;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (reader.Name)
                        {
                            case "year":
                                copyright.Year = int.Parse(ReadContentAsString(reader));
                                break;
                            case "license":
                                copyright.Licence = new Uri(ReadContentAsString(reader));
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return copyright;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxBounds ReadGpxBounds(XmlReader reader)
        {
            if (!reader.IsEmptyElement) throw new FormatException(reader.Name);

            GpxBounds bounds = new GpxBounds();

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "minlat":
                        bounds.MinLatitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "maxlat":
                        bounds.MaxLatitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "minlon":
                        bounds.MinLongitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "maxlon":
                        bounds.MaxLongitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }

            return bounds;
        }

        private void SkipElement(XmlReader reader)
        {
            if (reader.IsEmptyElement) return;
            string elementName = reader.Name;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == elementName) return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGpxExtensions(XmlReader reader)
        {
            SkipElement(reader);
        }

        private void ReadMetadataExtensions(XmlReader reader, GpxMetadata metadata)
        {
            SkipElement(reader);
        }

        private void ReadWayPointExtensions(XmlReader reader, GpxWayPoint wayPoint)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        if (reader.NamespaceURI == GARMIN_EXTENSIONS)
                        {
                            switch (reader.LocalName)
                            {
                                case "WaypointExtension":
                                    ReadGarminWayPointExtensions(reader, wayPoint);
                                    break;
                                default:
                                    throw new FormatException(reader.Name);
                            }

                            break;
                        }

                        SkipElement(reader);
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadRoutePointExtensions(XmlReader reader, GpxRoutePoint routePoint)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        if (reader.NamespaceURI == GARMIN_EXTENSIONS)
                        {
                            switch (reader.LocalName)
                            {
                                case "WaypointExtension":
                                    ReadGarminWayPointExtensions(reader, routePoint);
                                    break;
                                case "RoutePointExtension":
                                    ReadGarminRoutePointExtensions(reader, routePoint);
                                    break;
                                default:
                                    throw new FormatException(reader.Name);
                            }

                            break;
                        }

                        SkipElement(reader);
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadRouteExtensions(XmlReader reader, GpxRoute route)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.LocalName == "RouteExtension" && reader.NamespaceURI == GARMIN_EXTENSIONS)
                        {
                            ReadGarminTrackOrRouteExtensions(reader, route);
                            break;
                        }

                        SkipElement(reader);
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadTrackExtensions(XmlReader reader, GpxTrack track)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.LocalName == "TrackExtension" && reader.NamespaceURI == GARMIN_EXTENSIONS)
                        {
                            ReadGarminTrackOrRouteExtensions(reader, track);
                            break;
                        }

                        SkipElement(reader);
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadTrackSegmentExtensions(XmlReader reader)
        {
            SkipElement(reader);
        }

        private void ReadTrackPointExtensions(XmlReader reader)
        {
            SkipElement(reader);
        }

        private void ReadGarminWayPointExtensions(XmlReader reader, GpxWayPoint wayPoint)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "Address":
                                wayPoint.Address = ReadGarminGpxAddress(reader);
                                break;
                            case "PhoneNumber":
                                wayPoint.Phones.Add(ReadGarminGpxPhone(reader));
                                break;
                            case "Categories":
                            case "Depth":
                            case "DisplayMode":
                            case "Proximity":
                            case "Temperature":
                            case "Extensions":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminRoutePointExtensions(XmlReader reader, GpxRoutePoint routePoint)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "rpt":
                                routePoint.RoutePoints.Add(ReadGarminAutoRoutePoint(reader));
                                break;
                            case "Subclass":
                            case "Extensions":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private void ReadGarminTrackOrRouteExtensions(XmlReader reader, GpxTrackOrRoute trackOrRoute)
        {
            if (reader.IsEmptyElement) return;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "DisplayColor":
                                trackOrRoute.DisplayColor = (GpxColor)Enum.Parse(typeof(GpxColor), ReadContentAsString(reader), false);
                                break;
                            case "IsAutoNamed":
                            case "Extensions":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxPoint ReadGarminAutoRoutePoint(XmlReader reader)
        {
            GpxPoint point = new GpxPoint();

            string elementName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "lat":
                        point.Latitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;

                    case "lon":
                        point.Longitude = double.Parse(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                }
            }

            if (isEmptyElement) return point;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        SkipElement(reader);
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return point;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxAddress ReadGarminGpxAddress(XmlReader reader)
        {
            GpxAddress address = new GpxAddress();
            if (reader.IsEmptyElement) return address;

            string elementName = reader.Name;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "StreetAddress":

                                if (string.IsNullOrEmpty(address.StreetAddress))
                                {
                                    address.StreetAddress = ReadContentAsString(reader);
                                    break;
                                }

                                address.StreetAddress += " " + ReadContentAsString(reader);
                                break;

                            case "City":
                                address.City = ReadContentAsString(reader);
                                break;
                            case "State":
                                address.State = ReadContentAsString(reader);
                                break;
                            case "Country":
                                address.Country = ReadContentAsString(reader);
                                break;
                            case "PostalCode":
                                address.PostalCode = ReadContentAsString(reader);
                                break;
                            case "Extensions":
                                SkipElement(reader);
                                break;
                            default:
                                throw new FormatException(reader.Name);
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != elementName) throw new FormatException(reader.Name);
                        return address;
                }
            }

            throw new FormatException(elementName);
        }

        private GpxPhone ReadGarminGpxPhone(XmlReader reader)
        {
            return new GpxPhone
            {
                Category = reader.GetAttribute("Category", GARMIN_EXTENSIONS),
                Number = ReadContentAsString(reader)
            };
        }

        private string ReadContentAsString(XmlReader reader)
        {
            if (reader.IsEmptyElement) throw new FormatException(reader.Name);

            string elementName = reader.Name;
            string result = string.Empty;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Text:
                        result = reader.Value;
                        break;

                    case XmlNodeType.EndElement:
                        return result;

                    case XmlNodeType.Element:
                        throw new FormatException(elementName);
                }
            }

            throw new FormatException(elementName);
        }

        private int ReadContentAsInt(XmlReader reader)
        {
            string value = ReadContentAsString(reader);
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        private double ReadContentAsDouble(XmlReader reader)
        {
            string value = ReadContentAsString(reader);
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private DateTime ReadContentAsDateTime(XmlReader reader)
        {
            string value = ReadContentAsString(reader);
            return DateTime.Parse(value);
        }

        public void Dispose()
        {
            Reader_.Close();
        }
    }       
}