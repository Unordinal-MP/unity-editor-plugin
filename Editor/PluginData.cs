using UnityEngine;
using System;
using System.IO;
using System.Text.Json;

namespace Unordinal.Hosting
{
    public class PluginData
    {
        private static string PluginDataFile => Path.Combine(Application.dataPath, @"Unordinal\PluginData.json").Replace("\\", "/");
        
        public string StudioID { get; set; } = GetStudioName();
        public Guid ProjectID { get; set; }
        public string ProjectName { get; set; } = Application.productName;
        
        public void Save()
        {
            // Create folder if doesn't exist.
            Directory.CreateDirectory(Path.GetDirectoryName(PluginDataFile));
            using (StreamWriter writer = new StreamWriter(PluginDataFile))
            {
                writer.WriteLineAsync(JsonSerializer.Serialize(this));
            }
        }

        public static PluginData LoadPluginData()
        {
            var result = new PluginData(); // Default (will be used if no file exist).

            // Try to load data from file.
            if (File.Exists(PluginDataFile))
            {
                try
                {
                    result = JsonSerializer.Deserialize<PluginData>(File.ReadAllText(PluginDataFile)) ?? new PluginData();
                }
                catch (Exception)
                {
                    // Empty
                }
            }

            // Save to file.
            // (By always saving to file, added/removed fields will automatically be added/removed in the file as well.)
            result.Save();

            return result;
        }

        private static string GetStudioName()
        {
            var studioName = string.Empty;

            if (Application.companyName == "DefaultCompany") // Default Unity company name
            {
                studioName = NameGenerator.GetRandomName();
            }
            else
            {
                studioName = Application.companyName;
            }
            
            return studioName;
        }
    }
}
