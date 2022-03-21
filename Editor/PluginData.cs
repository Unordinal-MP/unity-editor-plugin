using UnityEngine;
using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Unordinal.Editor.Services;

namespace Unordinal.Editor
{
    public class PluginData
    {
        public string StudioID { get; set; } = GetStudioName();
        public Guid ProjectID { get; set; }
        public string ProjectName { get; set; } = Application.productName;
        
        private static string GetStudioName()
        {
            return IsDefaultCompanyName() ? NameGenerator.GetRandomName() : Application.companyName;
        }
        

        private static bool IsDefaultCompanyName()
        {
            return Application.companyName == "DefaultCompany"; // Default Unity company name
        }
    }

    public class PluginDataFactory
    {
        private static string PluginDataFile => Path.Combine(new DirectoryInfo(Application.dataPath).FullName, "Unordinal", "PluginData.json");

        private readonly ILogger<PluginDataFactory> logger;

        public PluginDataFactory(ILogger<PluginDataFactory> logger) {
            this.logger = logger;
        }
        
        public PluginData LoadPluginData()
        {
            // Try to load data from file.
            var result = ReadFromFile();
            
            // Save to file.
            // (By always saving to file, added/removed fields will automatically be added/removed in the file as well.)
            SavePluginData(result);
            return result;
        }
        
        public void SavePluginData(PluginData result)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PluginDataFile));
                using (var writer = new StreamWriter(PluginDataFile))
                {
// TODO: Have a look on how to remove this warning the correct way.
#pragma warning disable CS1701 // Disable warning
                    writer.Write(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
#pragma warning restore CS1701
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occured when trying to store plugin data");
            }
        }

        private PluginData ReadFromFile()
        {
            if (!File.Exists(PluginDataFile)) return new PluginData();
            try
            {
                var readData = JsonSerializer.Deserialize<PluginData>(File.ReadAllText(PluginDataFile));
                return readData ?? new PluginData();
            }
            catch(Exception e)
            {
                logger.LogError(e, "An error occured when trying to load plugin data from file");
                return new PluginData();
            }
        }
    }
}
