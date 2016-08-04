using NReco.VideoConverter;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RouteVideoPreviewer
{
    class RouteLapser
    {
        private string CourseFilePath { get; set; }

        private TrainingCenterDatabase_t TrainingCenterXmlData { get; set; }

        private GoogleMapsApi GoogleMapClient { get; set; }

        private FFMpegConverter FFMpegClient { get; set; }

        public RouteLapser(string courseFilePath)
        {
            CourseFilePath = courseFilePath;
            TrainingCenterXmlData = RouteLapser.OpenCourse(CourseFilePath);

            FFMpegClient = new FFMpegConverter();
            GoogleMapClient = new GoogleMapsApi();
        }

        public async Task CreateHyperlapse(string outputFilePath)
        {
            Trace.TraceInformation("Retrieving street views...");
            var track = TrainingCenterXmlData.Courses[0].Track;
            for (int i = 1; i < track.Length; i++)
            {
                var previousPoint = track[i - 1];
                var currentPoint = track[i];
                var heading = CalculateHeading(previousPoint, currentPoint);
                var streetView = await GoogleMapClient.GetStreetViewImage(currentPoint.Position.LatitudeDegrees, currentPoint.Position.LongitudeDegrees, heading);
                streetView.Save($"temp{i}.jpg");
            }

            Trace.TraceInformation("Rendering video...");
            FFMpegClient.ConvertMedia(new FFMpegInput[] { new FFMpegInput("temp%d.jpg") { CustomInputArgs = "-start_number 1 -framerate 3" } }, outputFilePath, Format.mp4, new OutputSettings() { CustomOutputArgs = "-c:v libx264 -crf 18" });
        }

        private static TrainingCenterDatabase_t OpenCourse(string path)
        {
            Trace.TraceInformation("Opening course file...");
            TrainingCenterDatabase_t tcx;
            var serializer = new XmlSerializer(typeof(TrainingCenterDatabase_t));
            using (var stream = new StreamReader(path))
            {
                tcx = (TrainingCenterDatabase_t)serializer.Deserialize(stream);
            }
            return tcx;
        }

        private static Point ToPoint(Trackpoint_t pt)
        {
            return new Point((int)(pt.Position.LongitudeDegrees * 100000), (int)(pt.Position.LatitudeDegrees * 100000));
        }

        private static int CalculateHeading(Trackpoint_t pt1, Trackpoint_t pt2)
        {
            return CalculateHeading(ToPoint(pt1), ToPoint(pt2));
        }

        private static int CalculateHeading(Point pt1, Point pt2)
        {
            var yDiff = pt2.Y - pt1.Y;
            var xDiff = pt2.X - pt1.X;
            // theta is the angle between x-axis and the point
            // theta angle is positive for counter-clockwise angles (upper half plane) and negative for clockwise angles (lower half plane)
            var theta = (int)(Math.Atan2(yDiff, xDiff) * 180 / Math.PI);

            // angle is normalized such that the angle is always positive and measured counter-clockwise from x-axis
            var angle = (yDiff >= 0) ? theta : 360 + theta;

            // heading is measured clockwise so angle is flipped and shifted so that 0deg points north
            return (360 - angle + 90) % 360;
        }
    }
}
