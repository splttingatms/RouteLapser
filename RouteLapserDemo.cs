using NReco.VideoConverter;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Xml.Serialization;

namespace RouteVideoPreviewer
{
    class RouteLapserDemo
    {
        static void Main(string[] args)
        {
            var routeFilePath = "UW_to_Gasworks.tcx";

            var tcx = OpenRoute(routeFilePath);

            Console.WriteLine("Getting street view images...");
            var track = tcx.Courses[0].Track;
            for (int i = 1; i < track.Length; i++)
            {
                var prev = track[i - 1];
                var curr = track[i];

                var heading = CalculateHeading(prev, curr);
                GetStreetView(curr.Position.LatitudeDegrees, curr.Position.LongitudeDegrees, heading, $"temp{i}.jpg");
            }

            Console.WriteLine("Rendering video...");
            var converter = new FFMpegConverter();
            converter.ConvertMedia(new FFMpegInput[] { new FFMpegInput("temp%d.jpg") { CustomInputArgs = "-start_number 1 -framerate 3" } }, "routelapse.mp4", Format.mp4, new OutputSettings() { CustomOutputArgs = "-c:v libx264 -crf 18" });

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static TrainingCenterDatabase_t OpenRoute(string path)
        {
            TrainingCenterDatabase_t tcx;
            var serializer = new XmlSerializer(typeof(TrainingCenterDatabase_t));
            using (var stream = new StreamReader(path))
            {
                tcx = (TrainingCenterDatabase_t)serializer.Deserialize(stream);
            }
            
            return tcx;
        }

        private static void GetStreetView(double lat, double lng, int heading, string filepath)
        {
            // YouTube uses 16:9 aspect ratio players
            // https://support.google.com/youtube/answer/1722171?hl=en
            var width = 640;
            var height = 400;
            //var heading = 235; // 0 and 360 north, 90 east, 180 south
            var pitch = 0; // default 0, 90 degrees indicates straight up -90 indicates straight down
            var fov = 120; // default 90, max 120, smaller number means higher level of zoom
            var key = "INSERT_KEY_HERE";

            var path = $"https://maps.googleapis.com/maps/api/streetview?location={lat},{lng}&size={width}x{height}&fov={fov}&heading={heading}&pitch={pitch}&key={key}";
            using (var client = new HttpClient())
            using (var response = client.GetAsync(path).Result)
            {
                response.EnsureSuccessStatusCode();

                var streetView = System.Drawing.Image.FromStream(response.Content.ReadAsStreamAsync().Result);
                streetView.Save(filepath);
            }
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
