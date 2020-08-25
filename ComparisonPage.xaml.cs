using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ModelGraphGen;
using DigitalerPlanstempel.Template;
using System;
using Newtonsoft.Json.Linq;
using System.IO;
using DigitalerPlanstempel.Utility;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;
using DigitalerPlanstempel.Neo4j;

namespace DigitalerPlanstempel
{
    /// <summary>
    /// Interaction logic for ComparisonPage.xaml
    /// </summary>
    public partial class ComparisonPage : Page
    {

        private Permission Permission;
        private ModelTemplate Model2;
        List<Distinction> Distinctions;

        public ComparisonPage(Permission Permission)
        {
            InitializeComponent();
            this.Permission = Permission;
        }

        /// <summary>
        /// Hinzufügen der geöffneten Datei zur Graphdatenbank
        /// </summary>
        private void ClickOnModelToNeo4j(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "IFC-Files (*.ifc)|*.ifc";
            string sourceFile = null;
            string resultFile = null;
            if (openFileDialog.ShowDialog() == true)
            {
                sourceFile = openFileDialog.FileName;
            }

            if (sourceFile != null)
            {
                var parser = new Instance2Neo4jParser
                {
                    SourceLocation = sourceFile,
                    TargetLocation = resultFile
                };

                var modelName = "model2";
                parser.SendToDb(false, modelName, Neo4jAccessData.Uri, Neo4jAccessData.User, Neo4jAccessData.Password);
                MessageBox.Show("Modell wurde zur Graphdatenbank hinzugefügt.", "");
            }
            else
            {
                MessageBox.Show("Ifc-Datei konnte nicht hinzugefügt werden. Probieren Sie es erneut.");
            }
        }

        /// <summary>
        /// Vergleich der beiden Schablonen
        /// </summary>
        private void ClickOnComparison(object sender, RoutedEventArgs e)
        {
            ComparisonGrid.Visibility = Visibility.Visible;
            if (Permission.VerifySignature())
            {
                Console.WriteLine(true);
                LabelSignaturVerifizierung.Visibility= Visibility.Visible;
            }

            //Umwandeln der JSON-Datei in einen dynamic Typ, um damit die Objekte in der Datei zu erreichen.
            dynamic templateModel = JObject.Parse(Permission.ModelTemplate);
            dynamic model2 = JObject.Parse(Model2.JsonTemplate);

            Comparison comparison = new Comparison();
            Distinctions = comparison.CompareBuilding(templateModel, model2);

            //Alle Veränderungen werden in einer Tabelle dargestellt.
            NewElements.ItemsSource = Distinctions;
        }

        /// <summary>
        /// Schablone für das zweite Modell wird erstellt
        /// </summary>
        private void ClickOnSchablone(object sender, RoutedEventArgs e)
        {
            Model2 = new ModelTemplate("model2",Permission.StoreyRestriction, Permission.ExaminationRestriction);
            VergleichenButton.Visibility= Visibility.Visible;
        }

        /// <summary>
        /// Aufrufen der HTML von xBIM WeXplorer, Code für die Verwendung des WebViewers wurde von 
        /// http://docs.xbim.net/XbimWebUI/tutorial-5_Colourful_building.html | http://docs.xbim.net/XbimWebUI/5_Colourful_building.live.html  und https://github.com/xBimTeam/XbimWebUI übernommen
        /// </summary>
        private void ClickOnGrafischDarstellen(object sender, RoutedEventArgs e)
        {
            var path = Constants.FolderPath + @"WebViewer\data\templateModel.ifc";
            
            //Erzeugen der Modell IFC-Datei aus dem Base64String
            Byte[] bytes = Convert.FromBase64String(Permission.ModelFile);
            File.WriteAllBytes(path, bytes);

            string wexBimB64 = "";

            //Erzeugen der wexBim Datei vom Modell. Dateien im wexBim Format werden für den Web Viewer benötigt
            using (var model = IfcStore.Open(path))
            {
                var context = new Xbim3DModelContext(model);
                context.CreateContext();

                var wexBimFilename = Path.ChangeExtension(path, "wexBIM");
                using (var wexBiMfile = File.Create(wexBimFilename))
                {
                    using (var wexBimBinaryWriter = new BinaryWriter(wexBiMfile))
                    {
                        model.SaveAsWexBim(wexBimBinaryWriter);
                        wexBimBinaryWriter.Close();
                    }
                    wexBiMfile.Close();
                }

                //Umwandeln des wexBim Datei in einen Base64String um es in der Javascript Datei des Web Viewers zu verwenden.
                var bytesWexBim = File.ReadAllBytes(wexBimFilename);
                wexBimB64 = Convert.ToBase64String(bytesWexBim);
            }

            File.Delete(path);

            //Ermitteln der EntityIds aller veränderten Bauteile 
            var tempNeo4j = new Neo4JConnector(Neo4jAccessData.Uri, Neo4jAccessData.User, Neo4jAccessData.Password,"templateModel");
            List<int> entityIds = new List<int>();
            foreach (var item in Distinctions)
            {
                var wallGlobalId = tempNeo4j.GetIdForChangedElements(item.WallGlobalId);
                var wallElementGlobalId = tempNeo4j.GetIdForChangedElements(item.WallElementGlobalId);
                if (!entityIds.Contains(wallGlobalId) && wallGlobalId != 0 && wallElementGlobalId==0)
                {
                    entityIds.Add(wallGlobalId);
                }
                else if (!entityIds.Contains(wallElementGlobalId) && wallElementGlobalId !=0)
                {
                    entityIds.Add(wallElementGlobalId);
                }    
            }

            //Speichern der BauteilIds und des Base64Strings der wexBim datei.
            var pathChangedElements = Constants.FolderPath + @"WebViewer\data\templateModel.js";
            string input = "var data = {\"changedElements\" : [" + String.Join(",", entityIds) + "]};";
            input += $@"var dataWexBimB64 = ""{wexBimB64}"";";
            File.WriteAllText(pathChangedElements, input);

            //Aufrufen des Web Viewers
            System.Diagnostics.Process.Start(Constants.FolderPath + @"WebViewer\webViewer.html");

        }

        //Restart des Programms
        private void ClickOnZurueck(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        //Angabe welcher RadioButton ausgewählt ist
        private void RadioButtonElement_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            var buildingElementRestriction = radioButton.Content.ToString();
            var tempDistinctions = new List<Distinction>();
            if (Distinctions != null)
            {
                foreach (var item in Distinctions)
                {
                    tempDistinctions.Add(item.Copy());
                }
                switch (buildingElementRestriction)
                {
                    case "Wände":
                        tempDistinctions.RemoveAll(item => item.WallElement != null);
                        NewElements.ItemsSource = tempDistinctions;
                        break;
                    case "Fenster":
                        tempDistinctions.RemoveAll(item => item.WallElement == "Tür" || item.WallElement == null);
                        NewElements.ItemsSource = tempDistinctions;
                        break;
                    case "Türen":
                        tempDistinctions.RemoveAll(item => item.WallElement == "Fenster" || item.WallElement == null);
                        NewElements.ItemsSource = tempDistinctions;
                        break;
                    case "Alle genannten Optionen":
                        NewElements.ItemsSource = tempDistinctions;
                        break;
                }
            }
            
        }
    }
}
