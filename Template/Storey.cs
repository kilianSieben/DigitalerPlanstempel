using System.Collections.Generic;
using DigitalerPlanstempel.BuildingElements;

namespace DigitalerPlanstempel.Template
{
    public class Storey
    {
        public string StoreyHash { get; set; }
        public string StoreyName { get; set; }
        public List<Wall> AllWalls { get; set; }
    }
}
