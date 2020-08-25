using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using DigitalerPlanstempel.Neo4j;
using DigitalerPlanstempel.Utility;
using DigitalerPlanstempel.BuildingElements;

namespace DigitalerPlanstempel.Template
{
    /// <summary>
    ///     Ermitteln aller Ebenen der Schablone
    /// </summary>
    public class CreateTemplate
    {
        //Die Funktionen dieser Klasse rufen Funktionen aus der Klasse Neo4JConnector auf, um Abfragen zu erstellen.
        public Neo4JConnector Con;
        private string ExaminationRestriction;
        public CreateTemplate(string modelName, string examinationRestriction)
        {
            var uri =  Neo4jAccessData.Uri;
            var user = Neo4jAccessData.User;
            var password = Neo4jAccessData.Password;

            ExaminationRestriction = examinationRestriction;
            Con = new Neo4JConnector(uri, user, password, modelName);
        }

        /// <summary>
        ///     Liste mit Eigenschaften, kann je nach Wunsch erweitert werden
        /// </summary>
        public List<string> GetPropertyNames(int id, string type)
        {
            var propertyNames = new List<string>();

            if (type == "Wall")
            {
                propertyNames.Add("Height");
                propertyNames.Add("Length");
                propertyNames.Add("Width");
                propertyNames.Add("Material");
                propertyNames.Add("'LoadBearing'");
                if (ExaminationRestriction != "Statikprüfung")
                {
                    propertyNames.Add("'ThermalTransmittance'");
                    propertyNames.Add("Name");
                }
                
            }
            else
            {
                propertyNames.Add("Height");
                propertyNames.Add("Width");
                if (ExaminationRestriction != "Statikprüfung")
                {
                    propertyNames.Add("Material");
                    propertyNames.Add("Name");
                }
            }

            return propertyNames;
        }

        /// <summary>
        ///     Erzeugen der Property Klasse.
        ///     Eigenschaften werden speziell für den Bauteiltypen ermittelt
        /// </summary>
        public Property GetProperties(int id, string name, string entityName)
        {
            var result = new Property();

            result.PropertyName = name;
            if (entityName == "Wall")
            {
                result.PropertyValue = Con.GetPropertyValueWall(id, name);
                result.PropertyHash = Con.GetPropertyHashWall(id, name);
            }
            else if (entityName == "Door" || entityName == "Window")
            {
                result.PropertyValue = Con.GetPropertyValueDoorOrWindow(id, name);
                result.PropertyHash = Con.GetPropertyHashDoorOrWindow(id, name);
            }
            
            return result;
        }

        /// <summary>
        ///     Erzeugen der Wall Klasse.
        ///     Aufrufen der GetProperties Funktion
        /// </summary>
        public Door GetDoor(int id)
        {
            var result = new Door();
            var allProperties = new List<Property>();
            var propertyNames = GetPropertyNames(id, "Door");
            foreach (var name in propertyNames)
            {
                allProperties.Add(GetProperties(id, name, "Door"));
            }

            result.GlobalId = Con.GetGlobalId(id);
            result.AllProperties = allProperties.OrderBy(o => o.PropertyName).ToList();
            result.StartPoint = Con.GetStartPointDoorOrWindow(id);

            string doorJson = JsonSerializer.Serialize(result);
            using (SHA256 sha256Hash = SHA256.Create())
            {
                result.DoorHash = Hashing.GetHash(sha256Hash, doorJson);
            }

            return result;
        }

        /// <summary>
        ///     Erzeugen der Fenster Klasse.
        ///     Aufrufen der GetProperties Funktion
        /// </summary>
        public Window GetWindow(int id)
        {
            var result = new Window();
            var allProperties = new List<Property>();
            var propertyNames = GetPropertyNames(id, "Window");
            foreach (var name in propertyNames)
            {
                allProperties.Add(GetProperties(id, name, "Window"));
            }

            result.GlobalId = Con.GetGlobalId(id);
            result.AllProperties = allProperties.OrderBy(o => o.PropertyName).ToList();
            result.StartPoint = Con.GetStartPointDoorOrWindow(id);

            string doorJson = JsonSerializer.Serialize(result);
            using (SHA256 sha256Hash = SHA256.Create())
            {
                result.WindowHash = Hashing.GetHash(sha256Hash, doorJson);
            }

            return result;
        }

        /// <summary>
        ///     Erzeugen der Wall Klasse.
        ///     Aufrufen der GetWindow,GetDoor und GetProperties Funktion
        /// </summary>
        public Wall GetWall(int id)
        {
            var result = new Wall();
            var allProperties = new List<Property>();
            var propertyNames = GetPropertyNames(id, "Wall");
            //Alle Eigenschaften für eine Wand ermitteln
            foreach (var name in propertyNames)
            {
                allProperties.Add(GetProperties(id, name, "Wall"));
            }

            //Alle Türen für eine Wand ermitteln
            var allDoors = new List<Door>();
            var doorIds = Con.GetDoorOrWindowIds(id, " IFCDOOR");
            foreach(var doorId in doorIds)
            {
                allDoors.Add(GetDoor(doorId));
            }
            result.AllDoors = allDoors.OrderBy(o=>o.StartPoint.X).ThenByDescending(o=>o.StartPoint.Y).ToList();

            //Alle Fenster für eine Wand ermitteln
            var allWindows = new List<Window>();
            var windowIds = Con.GetDoorOrWindowIds(id, " IFCWINDOW");
            foreach (var windowId in windowIds)
            {
                allWindows.Add(GetWindow(windowId));
            }
            result.AllWindows = allWindows.OrderBy(o => o.StartPoint.X).ThenByDescending(o => o.StartPoint.Y).ToList();

            //Startpunkt und Endpunkt der Wand ermitteln.
            result.GlobalId = Con.GetGlobalId(id);
            result.AllProperties = allProperties.OrderBy(o => o.PropertyName).ToList();
            result.StartPoint = Con.GetStartPointWall(id);
            var tempProperty = result.AllProperties.Where(o => o.PropertyName == "Length").ToList();
            result.EndPoint = Con.GetEndPointWall(id, result.StartPoint, tempProperty[0]);

            //Alle Attribute der Wand hashen.
            string wallJson = JsonSerializer.Serialize(result);
            using (SHA256 sha256Hash = SHA256.Create())
            {
                result.WallHash = Hashing.GetHash(sha256Hash, wallJson);
            }

            return result;
        }

        /// <summary>
        ///     Erzeugen der Storey Klasse.
        ///     Aufrufen der GetWall Funktion
        /// </summary>
        public Storey GetStorey(string storey)
        {
            var result = new Storey();
            var allWalls = new List<Wall>();
            var wallIds = Con.GetWallIds(storey);
            foreach(var id in wallIds)
            {
                //Unterscheidung ob es eine Statikprüfung oder eine normale Baugenehmigung ist. 
                //Nicht tragende Bauteile werden dabei nicht mehr aufgeführt.
                var temp = GetWall(id);
                if (temp.AllProperties.Any(item=>item.PropertyValue== "IFCBOOLEAN(.T.)") && ExaminationRestriction=="Statikprüfung")
                {
                    allWalls.Add(temp);
                }
                else if(ExaminationRestriction == "Baugenehmigung")
                {
                    allWalls.Add(temp);
                }
            }

            //Wände werden nach Startpunkt geordnet
            result.AllWalls = allWalls.OrderBy(o => o.StartPoint.X).ThenByDescending(o=>o.StartPoint.Y).ToList();
            result.StoreyName = storey;
            string storeyJson = JsonSerializer.Serialize(result);
            using (SHA256 sha256Hash = SHA256.Create())
            {
                result.StoreyHash = Hashing.GetHash(sha256Hash, storeyJson);
            }
            return result;
        }

        /// <summary>
        ///     Startpunkt bei der Erstellung einer Schablone. 
        ///     Erzeugen der Building Klasse. 
        ///     Aufrufen der GetStorey Funktion
        /// </summary>
        public Building GetBuilding(string storeyRestriction)
        {
            var result = new Building();
            var allStoreys = new List<Storey>();
            //Ermittlung aller vorhandenen Stockwerke
            var storeyNames = Con.GetStoreyNames();
            var tempStoreyNames = storeyNames.Select(item => (string)item.Clone()).ToList(); 

            //Alle Stockwerke bis auf das ausgewählte Stockwerk werden bei einer Teilprüfung entfernt
            if(storeyRestriction != "Gesamtes Gebäude")
            {
                foreach(var item in tempStoreyNames)
                {
                    if (item.Contains(storeyRestriction)==false)
                    {
                        storeyNames.Remove(item);
                    }
                }
            }

            //Erstellen aller Stockwerke als Klasse Storey
            foreach(var name in storeyNames)
            {
                allStoreys.Add(GetStorey(name));
            }
            result.AllStoreys = allStoreys;

            //Hashen des gesamten Gebäudes mit allen Bauteilen
            string buildingJson = JsonSerializer.Serialize(result);
            using (SHA256 sha256Hash = SHA256.Create())
            {
                result.BuildingHash = Hashing.GetHash(sha256Hash, buildingJson);
            }

            return result;
        }
    }
}
