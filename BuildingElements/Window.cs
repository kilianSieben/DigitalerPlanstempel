using System.Collections.Generic;

namespace DigitalerPlanstempel.BuildingElements
{
    public class Window
    {
        public string WindowHash { get; set; }
        public string GlobalId { get; set; }
        public CartesianPoint StartPoint { get; set; }
        public List<Property> AllProperties { get; set; }
    }
}
