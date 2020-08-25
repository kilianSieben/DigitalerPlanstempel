using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ModelGraphGen;
using DigitalerPlanstempel.Template;
using DigitalerPlanstempel.Utility;
using System.Windows.Controls;
using DigitalerPlanstempel.Neo4j;

namespace DigitalerPlanstempel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Permission Permission;
        private string SourcefilePath;
        private string StoreyRestriction;
        private string ExaminationRestriction;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Bestätigen der Prüfung. Modell kann zur Graphdatenbank hinzugefügt werden oder es wird nur eine Schablone erstellt.
        /// </summary>
        private void ClickOnNewTemplate(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "IFC-Files (*.ifc)|*.ifc";
            string sourceFile = null;
            string resultFile = null;
            if (openFileDialog.ShowDialog() == true)
            {
                var filestream = openFileDialog.OpenFile();
                LabelShowOpenFile.Content = openFileDialog.FileName;
                sourceFile = openFileDialog.FileName;
                SourcefilePath = openFileDialog.FileName;
            }

            if (ExaminerName.Text == "" || ModelName.Text == "" || sourceFile ==null || ExaminerName.Text == "Name des Prüfers" || ModelName.Text == "Name des Gebäudemodells")
            {
                MessageBox.Show("Geben sie Prüfer- und Modellname, sowie die zu prüfende IFC-Datei an. ");
            }
            else
            {
                var result = MessageBox.Show("Soll die IFC-Datei in die Graphdatenbank geladen werden?", "", MessageBoxButton.YesNo);

                //Hinzufügen des Modells in die Graphdatenbank
                if (result == MessageBoxResult.Yes)
                {
                    var parser = new Instance2Neo4jParser
                    {
                        SourceLocation = sourceFile,
                        TargetLocation = resultFile
                    };

                    var modelName = "templateModel";
                    parser.SendToDb(false, modelName, Neo4jAccessData.Uri, Neo4jAccessData.User, Neo4jAccessData.Password);
                    MessageBox.Show("IFC-Datei wurde in die Graphdatenbank geladen.", "Neue Schablone");
                }

                //Erstellen der Schablone für das geprüfte Modell
                var TemplateModel = new ModelTemplate("templateModel", StoreyRestriction, ExaminationRestriction);

                Byte[] bytes = File.ReadAllBytes(sourceFile);
                string file = Convert.ToBase64String(bytes);
                //Erzeugen einer neuen Genehmigung
                Permission = new Permission(ExaminerName.Text, ModelName.Text, TemplateModel.JsonTemplate, file, StoreyRestriction, ExaminationRestriction);

                //Aufrufen der nächsten Seite
                ComparisonPage comparisonPage = new ComparisonPage(Permission);
                this.Content = comparisonPage;
            }
        }

        //Angabe welcher RadioButton ausgewählt ist
        private void RadioButtonStorey_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            StoreyRestriction = radioButton.Content.ToString();
        }

        //Angabe welcher RadioButton ausgewählt ist
        private void RadioButtonExamination_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            ExaminationRestriction = radioButton.Content.ToString();
            
        }

    }
}
