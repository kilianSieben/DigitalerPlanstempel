using System.Collections.Generic;

namespace DigitalerPlanstempel.BuildingElements
{
    public class Wall
    {
        public string WallHash { get; set; }
        public string GlobalId { get; set; }
        public CartesianPoint StartPoint { get; set; }
        public CartesianPoint EndPoint { get; set; }
        public List<Property> AllProperties { get; set; }
        public List<Door> AllDoors { get; set; }
        public List<Window> AllWindows { get; set; }
    }
}
