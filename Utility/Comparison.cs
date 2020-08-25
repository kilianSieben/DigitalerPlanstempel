using System.Collections.Generic;
using System.Linq;

namespace DigitalerPlanstempel.Utility
{
    /// <summary>
    ///     Vergleichen von zwei erstellten Schablonen
    /// </summary>
    public class Comparison
    {
        public List<Distinction> Distinctions = new List<Distinction>();

        /// <summary>
        ///     Vergleich auf Gebäudeebene, falls Hash-Werte nicht übereinstimmen wird CompareStoreys aufgerufen
        /// </summary>
        public List<Distinction> CompareBuilding(dynamic templateModel, dynamic model2 )
        {
            if(templateModel.BuildingHash != model2.BuildingHash)
            {
                Distinction temp1 = new Distinction();
                Distinction temp2 = temp1.Copy();

               
                for(int i=0; i < templateModel.AllStoreys.Count; i++)
                {
                    CompareStoreys(templateModel.AllStoreys[i], model2.AllStoreys[i]);
                }
            }

            return Distinctions;
        }

        /// <summary>
        ///     Vergleich auf Stockwerkebene, falls Hash-Werte nicht übereinstimmen wird für jede Wand CompareWalls aufgerufen
        /// </summary>
        public void CompareStoreys(dynamic templateModelStorey, dynamic model2Storey)
        {
            var distinction = new Distinction();

            if (templateModelStorey.StoreyHash != model2Storey.StoreyHash)
            {
                distinction.StoreyName = model2Storey.StoreyName;

                //Bei abweichendem Namen des Stockwerk wird es in die Klasse Distinction festgehalten
                if(templateModelStorey.StoreyName != model2Storey.StoreyName)
                {
                    Distinction storeyNameDistinction = distinction.Copy();
                    storeyNameDistinction.Status = "Der Name des Stockwerks hat sich geändert.";
                }

                //Ermitteln aller WandGlobalIds eines Stockwerks 
                var templateModelStoreyWallGlobalIds = new List<string>();
                var model2StoreyWallGlobalIds = new List<string>();
                foreach (var item in templateModelStorey.AllWalls)
                {
                    templateModelStoreyWallGlobalIds.Add(item.GlobalId.ToString());
                }

                foreach(var item in model2Storey.AllWalls)
                {
                    model2StoreyWallGlobalIds.Add(item.GlobalId.ToString());
                }

                //Feststellen ob eine neue Wand hinzugekommen ist oder eine Wand gelöscht wurde
                var missingWalls1 = templateModelStoreyWallGlobalIds.Except(model2StoreyWallGlobalIds).ToList();
                foreach(var item in missingWalls1)
                {
                    var temp = distinction.Copy();
                    temp.WallGlobalId = item;
                    temp.Status = "Diese Wand existiert nicht mehr im aktuellen Gebäudemodell.";
                    Distinctions.Add(temp);
                }

                var missingWalls2 = model2StoreyWallGlobalIds.Except(templateModelStoreyWallGlobalIds).ToList();
                foreach (var item in missingWalls2)
                {
                    var temp = distinction.Copy();
                    temp.WallGlobalId = item;
                    temp.Status = "Diese Wand existiert nur im aktuellen Gebäudemodell.";
                    Distinctions.Add(temp);
                }

                //Vergleichen der einzelnen Wände
                foreach (var item in templateModelStorey.AllWalls)
                {
                    foreach(var element in model2Storey.AllWalls)
                    {
                        if(item.GlobalId == element.GlobalId)
                        {
                            CompareWalls(item, element, distinction);
                        }
                    }
                }
                
            }

        }

        /// <summary>
        ///     Vergleich auf Wandebene, falls Hash-Werte nicht übereinstimmen wird Startpunkt,Endpunkt überprüft und CompareProperties, CompareAllDoors und CompareAllWindows aufgerufen
        /// </summary>
        public void CompareWalls(dynamic templateModelWall, dynamic model2Wall, Distinction tempDistinction)
        {
            var distinction = tempDistinction.Copy();

            if (templateModelWall.WallHash != model2Wall.WallHash)
            {
                //Falls der Startpunkt abweicht wird dieser in den Distinction aufgenommen
                if(templateModelWall.StartPoint.PointHash != model2Wall.StartPoint.PointHash)
                {
                    Distinction startPointDistinction = distinction.Copy();
                    startPointDistinction.WallGlobalId = templateModelWall.GlobalId;
                    startPointDistinction.WallPropertyName = "Startpunkt";
                    startPointDistinction.Status = "Startpunkt der beiden Wände stimmt nicht überein.";
                    startPointDistinction.OldValue = "X = " + templateModelWall.StartPoint.X + " | Y = " + templateModelWall.StartPoint.Y + " | Z = " + templateModelWall.StartPoint.Z;
                    startPointDistinction.NewValue = "X = " + model2Wall.StartPoint.X + " | Y = " + model2Wall.StartPoint.Y + " | Z = " + model2Wall.StartPoint.Z;

                    Distinctions.Add(startPointDistinction);
                }
                //Falls der Endpunkt abweicht wird dieser in den Distinction aufgenommen
                if (templateModelWall.EndPoint.PointHash != model2Wall.EndPoint.PointHash)
                {
                    Distinction endPointDistinction = distinction.Copy();
                    endPointDistinction.WallGlobalId = templateModelWall.GlobalId;
                    endPointDistinction.WallPropertyName = "Endpunkt";
                    endPointDistinction.Status = "Endpunkt der beiden Wände stimmt nicht überein.";
                    endPointDistinction.OldValue = "X = " + templateModelWall.EndPoint.X + " | Y = " + templateModelWall.EndPoint.Y + " | Z = " + templateModelWall.EndPoint.Z;
                    endPointDistinction.NewValue = "X = " + model2Wall.EndPoint.X + " | Y = " + model2Wall.EndPoint.Y + " | Z = " + model2Wall.EndPoint.Z;

                    Distinctions.Add(endPointDistinction);
                }

                //Vergleichen aller Eigenschaften dieser Wände
                for(int i = 0; i < templateModelWall.AllProperties.Count; i++)
                {
                    distinction.WallGlobalId = templateModelWall.GlobalId;
                    CompareProperties(templateModelWall.AllProperties[i], model2Wall.AllProperties[i], distinction,"Wall");
                }

                //Vergleichen der Fenster und Türen dieser Wände
                CompareAllDoors(templateModelWall.AllDoors, model2Wall.AllDoors, distinction);
                CompareAllWindows(templateModelWall.AllWindows, model2Wall.AllWindows, distinction);

            }
        }

        /// <summary>
        ///     Vergleich auf Eigenschaftsebene, falls Hash-Werte nicht übereinstimmen wird die Veränderung zu Distinction hinzugefügt 
        /// </summary>
        public void CompareProperties(dynamic templateModelProperty, dynamic model2Property, Distinction tempDistinction, string type)
        {
            var distinction = tempDistinction.Copy();

            if(templateModelProperty.PropertyHash != model2Property.PropertyHash && type == "Wall")
            {
                distinction.WallPropertyName = templateModelProperty.PropertyName;
                distinction.Status = "Property: " + templateModelProperty.PropertyName + " hat sich in der aktuellen Version verändert.";
                distinction.OldValue = templateModelProperty.PropertyValue;
                distinction.NewValue = model2Property.PropertyValue;
                Distinctions.Add(distinction);
            }
            else if (templateModelProperty.PropertyHash != model2Property.PropertyHash && type == "notWall")
            {
                distinction.WallElementPropertyName = templateModelProperty.PropertyName;
                distinction.Status = "Property: " + templateModelProperty.PropertyName + " hat sich in der aktuellen Version verändert.";
                distinction.OldValue = templateModelProperty.PropertyValue;
                distinction.NewValue = model2Property.PropertyValue;
                Distinctions.Add(distinction);
            }
        }

        /// <summary>
        ///     Ermitteln von gelöschten oder neu hinzugekommenen Türen. Türen mit gleicher Id in den Modellen werden im Anschluss verglichen
        /// </summary>
        public void CompareAllDoors(dynamic templateModelAllDoors, dynamic model2AllDoors,Distinction tempDistinction)
        {
            var distinction = tempDistinction.Copy();
            var templateModelDoorGlobalIds = new List<string>();
            var model2DoorGlobalIds = new List<string>();
            foreach (var item in templateModelAllDoors)
            {
                templateModelDoorGlobalIds.Add(item.GlobalId.ToString());
            }

            foreach (var item in model2AllDoors)
            {
                model2DoorGlobalIds.Add(item.GlobalId.ToString());
            }

            var missingDoors1 = templateModelDoorGlobalIds.Except(model2DoorGlobalIds).ToList();
            foreach (var item in missingDoors1)
            {
                var temp = distinction.Copy();
                temp.WallElementGlobalId = item;
                temp.WallElement = "Tür";
                temp.Status = "Diese Tür existiert nicht mehr im aktuellen Gebäudemodell.";
                Distinctions.Add(temp);
            }

            var missingDoors2 = model2DoorGlobalIds.Except(templateModelDoorGlobalIds).ToList();
            foreach (var item in missingDoors2)
            {
                var temp = distinction.Copy();
                temp.WallElementGlobalId = item;
                temp.WallElement = "Tür";
                temp.Status = "Diese Tür existiert nur im aktuellen Gebäudemodell.";
                Distinctions.Add(temp);
            }

            foreach (var item in templateModelAllDoors)
            {
                foreach (var element in model2AllDoors)
                {
                    if (item.GlobalId == element.GlobalId)
                    {
                        CompareDoors(item, element, distinction);
                    }
                }
            }
        }

        /// <summary>
        ///     Vergleich auf Türebene, falls Hash-Werte nicht übereinstimmen wird die Veränderung zu Distinction hinzugefügt 
        /// </summary>
        public void CompareDoors(dynamic templateModelDoor, dynamic model2Door, Distinction tempDistinction)
        {
            var distinction = tempDistinction.Copy();

            if(templateModelDoor.DoorHash != model2Door.DoorHash)
            {
                distinction.WallElement = "Tür";
                distinction.WallElementGlobalId = templateModelDoor.GlobalId;
                if (templateModelDoor.StartPoint.PointHash != model2Door.StartPoint.PointHash)
                {
                    Distinction startPointDistinction = distinction.Copy();
                    startPointDistinction.WallElementPropertyName = "Startpunkt";
                    startPointDistinction.Status = "Startpunkt der beiden Türen stimmt nicht überein.";
                    startPointDistinction.OldValue = "X = " + templateModelDoor.StartPoint.X + " | Y = " + templateModelDoor.StartPoint.Y + " | Z = " + templateModelDoor.StartPoint.Z;
                    startPointDistinction.NewValue = "X = " + model2Door.StartPoint.X + " | Y = " + model2Door.StartPoint.Y + " | Z = " + model2Door.StartPoint.Z;
                    Distinctions.Add(startPointDistinction);
                }

                for (int i = 0; i < templateModelDoor.AllProperties.Count; i++)
                {
                    CompareProperties(templateModelDoor.AllProperties[i], model2Door.AllProperties[i], distinction,"notWall");
                }
            }
        }

        /// <summary>
        ///     Ermitteln von gelöschten oder neu hinzugekommenen Fenstern. Fenster mit gleicher Id in den Modellen werden im Anschluss verglichen
        /// </summary>
        public void CompareAllWindows(dynamic templateModelAllWindows, dynamic model2AllWindows, Distinction tempDistinction)
        {
            var distinction = tempDistinction.Copy();
            var templateModelWindowGlobalIds = new List<string>();
            var model2WindowGlobalIds = new List<string>();
            foreach (var item in templateModelAllWindows)
            {
                templateModelWindowGlobalIds.Add(item.GlobalId.ToString());
            }

            foreach (var item in model2AllWindows)
            {
                model2WindowGlobalIds.Add(item.GlobalId.ToString());
            }

            var missingWindows1 = templateModelWindowGlobalIds.Except(model2WindowGlobalIds).ToList();
            foreach (var item in missingWindows1)
            {
                var temp = distinction.Copy();
                temp.WallElementGlobalId = item;
                temp.WallElement = "Fenster";
                temp.Status = "Dieses Fenster existiert nicht mehr im aktuellen Gebäudemodell.";
                Distinctions.Add(temp);
            }

            var missingWindows2 = model2WindowGlobalIds.Except(templateModelWindowGlobalIds).ToList();
            foreach (var item in missingWindows2)
            {
                var temp = distinction.Copy();
                temp.WallElementGlobalId = item;
                temp.WallElement = "Fenster";
                temp.Status = "Dieses Fenster existiert nur im aktuellen Gebäudemodell.";
                Distinctions.Add(temp);
            }

            foreach (var item in templateModelAllWindows)
            {
                foreach (var element in model2AllWindows)
                {
                    if (item.GlobalId == element.GlobalId)
                    {
                        CompareWindows(item, element, distinction);
                    }
                }
            }
        }

        /// <summary>
        ///     Vergleich auf Fensterebene, falls Hash-Werte nicht übereinstimmen wird die Veränderung zu Distinction hinzugefügt 
        /// </summary>
        public void CompareWindows(dynamic templateModelWindow, dynamic model2Window, Distinction tempDistinction)
        {
            var distinction = tempDistinction.Copy();

            if (templateModelWindow.WindowHash != model2Window.WindowHash)
            {
                distinction.WallElement = "Fenster";
                distinction.WallElementGlobalId = templateModelWindow.GlobalId;
                if (templateModelWindow.StartPoint.PointHash != model2Window.StartPoint.PointHash)
                {

                    Distinction startPointDistinction = distinction.Copy();
                    startPointDistinction.WallElementPropertyName = "Startpunkt";
                    startPointDistinction.Status = "Startpunkt der beiden Fenster stimmt nicht überein.";
                    startPointDistinction.OldValue = "X = " + templateModelWindow.StartPoint.X + " | Y = " + templateModelWindow.StartPoint.Y + " | Z = " + templateModelWindow.StartPoint.Z;
                    startPointDistinction.NewValue = "X = " + model2Window.StartPoint.X + " | Y = " + model2Window.StartPoint.Y + " | Z = " + model2Window.StartPoint.Z;
                    Distinctions.Add(startPointDistinction);
                }

                for (int i = 0; i < templateModelWindow.AllProperties.Count; i++)
                {
                    CompareProperties(templateModelWindow.AllProperties[i], model2Window.AllProperties[i], distinction,"notWall");
                }
            }
        }


    }
}
