using System.Collections.Generic;

namespace DigitalerPlanstempel.BuildingElements
{
    public class Door
    {
        public string DoorHash { get; set; }
        public string GlobalId { get; set; }
        public CartesianPoint StartPoint { get; set; }
        public List<Property> AllProperties { get; set; }
    }
}
