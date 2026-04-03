using System;


namespace InfrastructureApp.ViewModels
{
    ///This is what the UI understands.
    /// We never expose TripChekcs raw JSON to the view
    /// 
    public class RoadCameraViewModel
    {
        public string CameraId { get; set; } = "";

        public string? Name { get; set; }

        public string? Road { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? ImageUrl { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }
    }
}