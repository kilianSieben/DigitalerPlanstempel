using System.Text.Json;
using System.IO;
using System.Text.RegularExpressions;
using DigitalerPlanstempel.Utility;

namespace DigitalerPlanstempel.Template
{
    /// <summary>
    ///     Erzeugen und speichern der Schablone
    /// </summary>
    public class ModelTemplate
    {
        public string JsonTemplate { get; private set; }
        public ModelTemplate(string modelName, string storeyRestriction, string examinationRestriction)
        {
            var template = new CreateTemplate(modelName, examinationRestriction);
            var model = template.GetBuilding(storeyRestriction);

            var path = Constants.FolderPath + @"ModelTemplates\" + modelName + ".json";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            JsonTemplate = JsonSerializer.Serialize(model, options);
            JsonTemplate = Regex.Unescape(JsonTemplate);
            File.WriteAllText(path, JsonTemplate);
        }

    }
}
