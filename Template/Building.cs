using System.Collections.Generic;

namespace DigitalerPlanstempel.Template
{
    public class Building
    {
        public string BuildingHash { get; set; }
        public List<Storey> AllStoreys { get; set; }
    }
}
