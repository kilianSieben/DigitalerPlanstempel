using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using Neo4j.Driver.V1;
using DigitalerPlanstempel.Utility;
using DigitalerPlanstempel.BuildingElements;

namespace DigitalerPlanstempel.Neo4j
{
    /// <summary>
    ///     Diese Klasse stellt die Verbindung zur Graphdatenbank her.
    ///     Hier werden alle Abfragen an die Query definiert. 
    /// </summary>
    public class Neo4JConnector : IDisposable
    {
        private readonly IDriver _driver;
        private string ModelName;

        public Neo4JConnector(string uri, string user, string password, string modelName)
        {
            ModelName = modelName;
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }

        public void Dispose()
        {
            // if driver exists, dispose him
            _driver?.Dispose();
        }

        /// <summary>
        ///     Abfrage aller IFCBUILDINGSTOREY-Knoten für ein spezielles Modell.
        /// </summary>
        public List<string> GetStoreyNames()
        {
            var storeyName = new List<string>();
            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(n:IFCBUILDINGSTOREY) " +
                                        "WHERE n.model = $ModelName " +
                                        "RETURN n.prop2 AS names",
                        new {ModelName});
                    foreach(var record in result)
                    {
                        storeyName.Add(record["names"].As<string>());
                    }
                    return result;
                });
            }
            return storeyName;
        }

        /// <summary>
        ///     Abfrage aller IFCWALLSTANDARDCASE-EntityIds für ein spezielles Stockwerk.
        /// </summary>
        public List<int> GetWallIds(string storey)
        {
            var wallIds = new List<int>();
            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(storey:IFCBUILDINGSTOREY)-[]-(:IFCRELCONTAINEDINSPATIALSTRUCTURE)-[]-(walls:IFCWALLSTANDARDCASE) " +
                                        "WHERE storey.model = $ModelName AND storey.prop2 = $storey " +
                                        "RETURN walls.EntityId as entityId",
                        new { ModelName, storey });
                    foreach (var record in result)
                    {
                        wallIds.Add(record["entityId"].As<int>());
                    }


                    return result;
                });
            }
            return wallIds;
        }

        /// <summary>
        ///     Abfrage aller IFCWINDOW oder IFCDOOR-EntityIds für eine spezielle Wand.
        /// </summary>
        public List<int> GetDoorOrWindowIds(int id, string type)
        {
            var doorIds = new List<int>();
            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(wall:IFCWALLSTANDARDCASE)-[]-(:IFCRELVOIDSELEMENT)-[]-(:IFCOPENINGELEMENT)-[]-(:IFCRELFILLSELEMENT)-[]-(element) " +
                                        "WHERE wall.model = $ModelName AND wall.EntityId = $id AND element.name= $type " +
                                        "RETURN element.EntityId as entityId",
                        new { ModelName, id, type });
                    foreach (var record in result)
                    {
                        doorIds.Add(record["entityId"].As<int>());
                    }
                    return result;
                });
            }
            return doorIds;
        }

        /// <summary>
        ///     Ermitteln der GlobalId für einen speziellen Knoten.
        /// </summary>
        public string GetGlobalId(int id)
        {
            var globalId = "";

            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(n) " +
                                        "WHERE n.model = $ModelName AND n.EntityId = $id " +
                                        "RETURN n.prop0 as id",
                        new { ModelName, id });
                    foreach (var record in result)
                    {
                        globalId = record["id"].As<string>();
                    }
                    return result;
                });
            }


            return globalId;
        }

        /// <summary>
        ///     Ermitteln der EntityId eines veränderten Elements.
        /// </summary>
        public int GetIdForChangedElements(string globalId)
        {
            int entityId=0;
            if (globalId == null)
            {
                return entityId;
            }
            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(n) " +
                                        "WHERE n.model = $ModelName AND n.prop0 = $globalId " +
                                        "RETURN n.EntityId as id",
                        new { ModelName, globalId});
                    foreach (var record in result)
                    {
                        entityId = record["id"].As<int>();
                    }
                    return result;
                });
            }

            return entityId;
        }

        /// <summary>
        ///     Abfrage von einer Eigenschaft einer Wand.
        ///     Je nach Art der Eigenschaft sind andere Abfragen notwendig.
        /// </summary>
        public string GetPropertyValueWall(int id, string name)
        {
            string propertyValue = null;

            if (name == "Length" || name == "Width" || name == "Height")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        //Runden der Ergebnisse damit dadurch keine Abweichungen im Hash-Wert enstehen.
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCRELDEFINESBYPROPERTIES)-[]-(n3:IFCELEMENTQUANTITY)-[]-(n4:IFCQUANTITYLENGTH) " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName AND n4.prop0 CONTAINS $name " +
                                            "WITH toFloat(n4.prop3) AS float " +
                                            "return round(100*float)/100 AS value",
                            new { ModelName, id, name});
                        foreach (var record in result)
                        {
                            propertyValue = record["value"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Material")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1:IFCWALLSTANDARDCASE)-[]-(n2:IFCRELASSOCIATESMATERIAL)-[]-(n3:IFCMATERIALLAYERSETUSAGE)-[]-(n4:IFCMATERIALLAYERSET)-[]-(n5:IFCMATERIALLAYER)-[]-(n6:IFCMATERIAL) " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "RETURN n6.prop0 AS material",
                            new { ModelName, id});
                        foreach (var record in result)
                        {
                            propertyValue = record["material"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Name")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1) " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName " +
                                            "return n1.prop2 as value",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyValue = record["value"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else
            {
                //Hier können alle Eigenschaften Abgefragt die in einem IFCPROPERTYSINGLEVALUE gespeichert sind.
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(relProperties:IFCRELDEFINESBYPROPERTIES)-[]-(propertySet:IFCPROPERTYSET)-[]-(property:IFCPROPERTYSINGLEVALUE) " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId = $id AND property.prop0 = $name " +
                                            "RETURN property.prop2 as value",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyValue = record["value"].As<string>();
                        }
                        return result;
                    });
                }
            }
            
            return propertyValue;
        }

        /// <summary>
        ///     Abfrage von einer Eigenschaft einer Tür oder eines Fensters.
        ///     Je nach Art der Eigenschaft sind andere Abfragen notwendig.
        ///     Die Abfragen für gleiche Eigenschaften sehen bei Fenster und Türen im Vergleich zur Wand anders aus.
        /// </summary>
        public string GetPropertyValueDoorOrWindow(int id, string name)
        {
            string propertyValue = null;

            if (name == "Height")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1) " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName " +
                                            "WITH toFloat(n1.prop8) AS float " +
                                            "return round(100*float)/100 AS value",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyValue = record["value"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Width")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1) " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName " +
                                            "WITH toFloat(n1.prop9) AS float " +
                                            "return round(100*float)/100 AS value",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyValue = record["value"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Material")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCRELASSOCIATESMATERIAL)-[]-(n3:IFCMATERIALLIST)-[]-(n4:IFCMATERIAL) " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "RETURN n4.prop0 AS material",
                            new { ModelName, id });
                        foreach (var record in result)
                        {
                            if (propertyValue == null)
                            {
                                propertyValue = record["material"].As<string>();
                            }
                            else
                            {
                                propertyValue = propertyValue + " und " + record["material"].As<string>();
                            }
                            
                        }
                        return result;
                    });
                }
            }
            else if (name == "Name")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1) " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName " +
                                            "return n1.prop2 as value",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyValue = record["value"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(relProperties:IFCRELDEFINESBYPROPERTIES)-[]-(propertySet:IFCPROPERTYSET)-[]-(property:IFCPROPERTYSINGLEVALUE) " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId = $id AND property.prop0 = $name " +
                                            "RETURN property.prop2 as value",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            if(record["value"].As<string>()!= "IFCLABEL('')")
                            {
                                propertyValue = record["value"].As<string>();
                            }
                            
                        }
                        return result;
                    });
                }
            }


            return propertyValue;
        }

        /// <summary>
        ///     Abfrage des Startpunkts der Wand.
        /// </summary>
        public CartesianPoint GetStartPointWall(int id)
        {
            var startPoint = new CartesianPoint();

            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(n1)-[]-(n2:IFCLOCALPLACEMENT)-[]-(n3:IFCAXIS2PLACEMENT3D)-[]-(n4:IFCCARTESIANPOINT) " +
                                        "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                        "RETURN n4 AS node",
                        new { ModelName, id });
                    foreach (var record in result)
                    {
                        var node = record["node"].As<INode>();
                        var numbers = node["prop0"].As<List<double>>();
                        startPoint.X = string.Format("{0:0.00}",numbers[0]);
                        startPoint.Y = string.Format("{0:0.00}", numbers[1]);
                        startPoint.Z = string.Format("{0:0.00}", numbers[2]);
                    }
                    return result;
                });
            }
            startPoint.PointHash = GetPointHashWall(id);
            return startPoint;
        }

        /// <summary>
        ///     Ermitteln des Endpunkts der Wand. 
        ///     Der Endpunkt wird mithilfe des Startpunkts der Länge und der Direction der Wand ermittelt.
        /// </summary>
        public CartesianPoint GetEndPointWall(int id, CartesianPoint startPoint, Property length)
        {
            var endPoint = new CartesianPoint();
            var directionXY = new Direction { X = 1.0, Y = 0.0, Z = 0.0 };
            var directionZ = new Direction { X = 0.0, Y = 0.0, Z = 1.0 };

            //Ermitteln der Direction der Wand
            using (var session = _driver.Session())
            {
                var greeting = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(wall)-[*3]-(n1:IFCDIRECTION) " +
                        "WHERE wall.EntityId = $id AND wall.model = $ModelName " +
                        "RETURN n1 AS node",
                        new {ModelName, id});

                    foreach (var record in result)
                    {
                        var node = record["node"].As<INode>();
                        var numbers = node["prop0"].As<List<double>>();

                        if (numbers[2] == 1)
                        {
                            directionZ.Z = numbers[2];
                        }
                        else
                        {
                            directionXY.X = numbers[0];
                            directionXY.Y = numbers[1];
                        }
                    }

                    return result;
                });
            }

            //Endpunkt berechnen mit Startpunkt, Länge und Direction der Wand
            var endPointX = (Convert.ToDouble(startPoint.X) + Convert.ToDouble(length.PropertyValue) * directionXY.X / (Math.Sqrt(Math.Pow(directionXY.X, 2) + Math.Pow(directionXY.Y, 2)))).ToString();
            var endPointY = (Convert.ToDouble(startPoint.Y) + Convert.ToDouble(length.PropertyValue) * directionXY.Y / (Math.Sqrt(Math.Pow(directionXY.X, 2) + Math.Pow(directionXY.Y, 2)))).ToString();
            var endPointZ = (Convert.ToDouble(startPoint.Z)).ToString();

            endPoint.X = string.Format("{0:0.00}", endPointX);
            endPoint.Y = string.Format("{0:0.00}", endPointY);
            endPoint.Z = string.Format("{0:0.00}", endPointZ);

            //Hashen des Endpunktes
            string pointJson = JsonSerializer.Serialize(endPoint);
            using (MD5 md5Hash = MD5.Create())
            {
                endPoint.PointHash = Hashing.GetHash(md5Hash, pointJson);
            }

            return endPoint;
        }

        /// <summary>
        ///     Abfrage des Startpunkts der Tür bzw. Wand.
        /// </summary>
        public CartesianPoint GetStartPointDoorOrWindow(int id)
        {
            var startPoint = new CartesianPoint();
            var version = "";

            //Kleine Unterscheidung je nach Modell bei der Ermittlung des Startpunkts.
            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(n1)-[]-(n2:IFCLOCALPLACEMENT)-[]-(n3:IFCAXIS2PLACEMENT3D)-[]-(n4:IFCCARTESIANPOINT) " +
                                        "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                        "RETURN n4 AS node",
                        new { ModelName, id });
                    foreach (var record in result)
                    {
                        var node = record["node"].As<INode>();
                        var numbers = node["prop0"].As<List<double>>();                   
                        startPoint.X = string.Format("{0:0.00}", numbers[0]);
                        startPoint.Y = string.Format("{0:0.00}", numbers[1]);
                        startPoint.Z = string.Format("{0:0.00}", numbers[2]);
                        version = "first";

                    }
                    return result;
                });

                if (startPoint.X == "0,00" && startPoint.X == "0,00" && startPoint.X == "0,00")
                {
                    var nodeResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCRELFILLSELEMENT)-[]-(n3:IFCOPENINGELEMENT)-[]-(n4:IFCLOCALPLACEMENT)-[]-(n5:IFCAXIS2PLACEMENT3D)-[]-(n6:IFCCARTESIANPOINT) " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "RETURN n6 AS node",
                            new { ModelName, id });
                        foreach (var record in result)
                        {
                            var node = record["node"].As<INode>();
                            var numbers = node["prop0"].As<List<double>>();
                            startPoint.X = string.Format("{0:0.00}", numbers[0]);
                            startPoint.Y = string.Format("{0:0.00}", numbers[1]);
                            startPoint.Z = string.Format("{0:0.00}", numbers[2]);
                            version = "second";
                        }
                        return result;
                    });
                }
            }

            //Hash muss je nach Ermittlungsweg des Startpunkts anders durchgeführt werden. 
            startPoint.PointHash = GetPointHashDoorOrWindow(id, version);
            return startPoint;
        }

        /// <summary>
        ///     Ermittlung des Hash-Wertes für eine Wandeingeschaft.
        /// </summary>
        public string GetPropertyHashWall(int id, string name)
        {
            var propertyHash = "";

            //Hash-Ermittlung hängt von der Ermittlungsart der Eigenschaftswerte weiter oben ab. Siehe GetPropertyValueWall
            if (name == "Length" || name == "Width" || name == "Height")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCRELDEFINESBYPROPERTIES)-[]-(n3:IFCELEMENTQUANTITY)-[]-(n4:IFCQUANTITYLENGTH) " +
                                            "WITH n1,n4 " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName AND n4.prop0 CONTAINS $name " +
                                            "WITH round(100*toFloat(n4.prop3))/100 AS value,n1,n4 " +
                                            "WITH collect([n1.name,n4.name,n4.prop0,value]) AS result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id, name});
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Material")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1:IFCWALLSTANDARDCASE)-[]-(n2:IFCRELASSOCIATESMATERIAL)-[]-(n3:IFCMATERIALLAYERSETUSAGE)-[]-(n4:IFCMATERIALLAYERSET)-[]-(n5:IFCMATERIALLAYER)-[]-(n6:IFCMATERIAL) " +
                                            "WITH n1,n6 " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "WITH collect([n1.name,n6.name,n6.prop0]) as result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id });
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n)-[r1]-(n1:IFCRELDEFINESBYPROPERTIES)-[r2]-(n2:IFCPROPERTYSET)-[r3]-(n3:IFCPROPERTYSINGLEVALUE) " +
                                            "WITH n1,n3 " +
                                            "WHERE n.model = $ModelName AND n.EntityId = $id AND n3.prop0 = $name " +
                                            "WITH collect([n1.name,n3.name,n3.prop0,n3.prop2]) AS result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }

            return propertyHash;
        }

        /// <summary>
        ///     Ermittlung des Hash-Wertes für ein Fenster oder eine Tür.
        /// </summary>
        public string GetPropertyHashDoorOrWindow(int id, string name)
        {
            var propertyHash = "";

            //Hash-Ermittlung hängt von der Ermittlungsart der Eigenschaftswerte weiter oben ab. Siehe GetPropertyValueDoorOrWindow
            if (name == "Height")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1) " +
                                            "WITH n1 " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName " +
                                            "WITH round(100*toFloat(n1.prop8))/100 AS value,n1 " +
                                            "WITH collect([n1.name,value]) AS result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Width")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1) " +
                                            "WITH n1 " +
                                            "WHERE n1.EntityId=$id AND n1.model =$ModelName " +
                                            "WITH round(100*toFloat(n1.prop9))/100 AS value,n1 " +
                                            "WITH collect([n1.name,value]) AS result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else if (name == "Material")
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCRELASSOCIATESMATERIAL)-[]-(n3:IFCMATERIALLIST)-[]-(n4:IFCMATERIAL) " +
                                            "WITH n1,n4 " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "WITH collect([n1.name,n4.name,n4.prop0]) as result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id });
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }
            else
            {
                using (var session = _driver.Session())
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n)-[]-(n1:IFCRELDEFINESBYPROPERTIES)-[]-(n2:IFCPROPERTYSET)-[]-(n3:IFCPROPERTYSINGLEVALUE) " +
                                            "WITH n1,n3 " +
                                            "WHERE n.model = $ModelName AND n.EntityId = $id AND n3.prop0 = $name " +
                                            "WITH collect([n1.name,n3.name,n3.prop0,n3.prop2]) AS result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id, name });
                        foreach (var record in result)
                        {
                            propertyHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
            }

            return propertyHash;
        }

        /// <summary>
        ///     Ermittlung des Hash-Wertes für den Startpunkt einer Wand
        /// </summary>
        public string GetPointHashWall(int id)
        {
            var pointHash = "";
            using (var session = _driver.Session())
            {
                var entityResult = session.WriteTransaction(tx =>
                {
                    var result = tx.Run("MATCH(n1)-[]-(n2:IFCLOCALPLACEMENT)-[]-(n3:IFCAXIS2PLACEMENT3D)-[]-(n4:IFCCARTESIANPOINT) " +
                                        "WITH n4 " +
                                        "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                        "WITH collect([n4.name,n4.prop0]) as result " +
                                        "RETURN apoc.hashing.fingerprint(result) AS hash",
                        new { ModelName, id });
                    foreach (var record in result)
                    {
                        pointHash = record["hash"].As<string>();
                        
                    }
                    return result;
                });
            }

            return pointHash;
        }

        /// <summary>
        ///     Ermittlung des Hash-Wertes für den Startpunkt einer Tür oder eines Fensters
        /// </summary>
        public string GetPointHashDoorOrWindow(int id, string version)
        {
            var pointHash = "";

            using (var session = _driver.Session())
            {
                if(version == "first")
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCLOCALPLACEMENT)-[]-(n3:IFCAXIS2PLACEMENT3D)-[]-(n4:IFCCARTESIANPOINT) " +
                                            "WITH n4 " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "WITH collect([n4.name,n4.prop0]) as result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id });
                        foreach (var record in result)
                        {
                            pointHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
                else
                {
                    var entityResult = session.WriteTransaction(tx =>
                    {
                        var result = tx.Run("MATCH(n1)-[]-(n2:IFCRELFILLSELEMENT)-[]-(n3:IFCOPENINGELEMENT)-[]-(n4:IFCLOCALPLACEMENT)-[]-(n5:IFCAXIS2PLACEMENT3D)-[]-(n6:IFCCARTESIANPOINT) " +
                                            "WITH n6 " +
                                            "WHERE n1.model = $ModelName AND n1.EntityId=$id " +
                                            "WITH collect([n6.name,n6.prop0]) as result " +
                                            "RETURN apoc.hashing.fingerprint(result) AS hash",
                            new { ModelName, id });
                        foreach (var record in result)
                        {
                            pointHash = record["hash"].As<string>();
                        }
                        return result;
                    });
                }
                
            }

            return pointHash;
        }


    }
}
