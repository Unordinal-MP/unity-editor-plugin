using UnityEngine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using Unordinal.Editor.External;

namespace Unordinal.Editor
{
    class DeployData
    {
        private string ip;
        List<UnordinalApi.DeployPort> DeployPort;
        private readonly ILogger<PluginDataFactory> logger;

        private static string DeployDataFile => Path.Combine(new DirectoryInfo(Application.dataPath).FullName, "Unordinal", "DeployData.txt");


        public DeployData(string ip, List<UnordinalApi.DeployPort> DeployPort)
        {
            this.ip = ip;
            this.DeployPort = DeployPort;
        }
        
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DeployDataFile));
                using (var writer = new StreamWriter(DeployDataFile))
                {
                    foreach (var port in DeployPort)
                    {
                        writer.Write($"{ip}:{port.ExternalNumber}\n");
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occured when trying to save deploy data to file.");
            }
        }
    }
}
