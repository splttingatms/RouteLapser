using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;

namespace RouteVideoPreviewer
{
    class GoogleMapsApi
    {
        private string Key { get; set; }

        public GoogleMapsApi() 
            : this(System.Configuration.ConfigurationManager.AppSettings["GoogleApiKey"])
        {
        }

        public GoogleMapsApi(string key)
        {
            Key = key;
        }

        public async Task<Image> GetStreetViewImage(double lat, double lng, double heading)
        {
            // YouTube uses 16:9 aspect ratio players
            // https://support.google.com/youtube/answer/1722171?hl=en
            var width = 640;
            var height = 400;
            //var heading = 235; // 0 and 360 north, 90 east, 180 south
            var pitch = 0; // default 0, 90 degrees indicates straight up -90 indicates straight down
            var fov = 120; // default 90, max 120, smaller number means higher level of zoom

            var path = $"https://maps.googleapis.com/maps/api/streetview?location={lat},{lng}&size={width}x{height}&fov={fov}&heading={heading}&pitch={pitch}&key={Key}";
            using (var client = new HttpClient())
            using (var response = client.GetAsync(path).Result)
            {
                response.EnsureSuccessStatusCode();
                return System.Drawing.Image.FromStream(await response.Content.ReadAsStreamAsync());
            }
        }
    }
}
