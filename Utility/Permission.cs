using System;
using System.Security.Cryptography;

namespace DigitalerPlanstempel.Utility
{
    /// <summary>
    ///     Bestätigte Prüfung wird in dieser Klasse gespeichert.
    /// </summary>
    public class Permission
    {
        public string ExaminerName { get; private set; }
        public string ModelName { get; private set; }
        public string ModelFile { get; private set; }
        public string ModelTemplate { get; private set; }
        public string StoreyRestriction { get; private set; }
        public string ExaminationRestriction { get; private set; }
        public byte[] ModelSignature { get; private set; }
        public DateTime PermissionDate { get; private set; }
        private RSACryptoServiceProvider Rsa;

        public Permission(string examiner, string modelName, string modelTemplate, string file, string storeyRestriction, string examinationRestriction)
        {
            ExaminerName = examiner;
            ModelName = modelName;
            ModelFile = file;
            StoreyRestriction = storeyRestriction;
            ExaminationRestriction = examinationRestriction;
            ModelTemplate = modelTemplate;
            PermissionDate = DateTime.Now;

            byte[] hashValue = HashPermissionValues();
            var signature = Signature.CreateSignature(hashValue);

            ModelSignature = signature.Item1;
            Rsa = signature.Item2;
            
        }

        /// <summary>
        ///     Verifizieren der digitalen Signatur
        /// </summary>
        public bool VerifySignature()
        {
            byte[] hashValue = HashPermissionValues();
            var result = Signature.VerifySignature(hashValue, ModelSignature, Rsa);
            return result;
        }

        /// <summary>
        ///     Erstellen des Hash-Werts für Prüfername, Modellname, Modelldatei, Schablone und dem Zeitpunkt der Prüfung.
        /// </summary>
        public byte[] HashPermissionValues()
        {

            string modelInfo = ExaminerName + ModelName + ModelFile + ModelTemplate + PermissionDate.ToString();
            byte[] hashValue;

            using (SHA256 sha256 = SHA256.Create())
            {
                hashValue = Hashing.GetHashBytes(sha256, modelInfo);
            }

            return hashValue;
        }

    }
}
