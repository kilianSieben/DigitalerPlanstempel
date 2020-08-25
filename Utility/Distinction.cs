

namespace DigitalerPlanstempel.Utility
{
    /// <summary>
    ///     Klasse um die ermittelten Veränderungen festzuhalten.
    /// </summary>
    public class Distinction
    {
        public string StoreyName { get; set; }
        public string WallGlobalId { get; set; }
        public string WallPropertyName { get; set; }
        public string WallElement { get; set; }
        public string WallElementGlobalId { get; set; }
        public string WallElementPropertyName{ get; set; }
        public string Status { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public Distinction Copy()
        {
            return (Distinction)this.MemberwiseClone();
        }

    }
}
