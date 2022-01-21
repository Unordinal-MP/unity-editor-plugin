using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Unordinal.Hosting
{
    public class RegionalDeploymentService
    {
        private readonly HttpClient client;

        public RegionalDeploymentService()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Accept", "*/*");
        }

        public async Task<Dictionary<string, long>> Ping(List<Region> regions, CancellationToken token)
        {
            List<Task<(string, long)>> resultDict = new List<Task<(string, long)>>();

            foreach (var region in regions)
            {
                Task<(string,long)> ms = MeasureTime(region, token);
                resultDict.Add(ms);

                if (token.IsCancellationRequested) { return new Dictionary<string, long>(); }
            }

            await Task.Run(() => Task.WhenAll(resultDict.ToArray()), token);
            return resultDict.Select(x => x.Result).ToDictionary(key => key.Item1, ms => ms.Item2);
        }

        private async Task<(string, long)> MeasureTime(Region region, CancellationToken token)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var url = $"https://{region.Domain}.blob.core.windows.net/$root/unordinalFile.json";
            await client.GetAsync(url, token);

            stopWatch.Stop();
            long ms = stopWatch.ElapsedMilliseconds;
            return (region.Name, ms);
        }

        public List<Region> ListOfRegions()
        {
            var regions = new List<Region>();
            regions.Add(new Region() { Domain = "unordinalwesteurope", Name = "West Europe" });
            regions.Add(new Region() { Domain = "unordinaleastasia", Name = "East Asia" });
            regions.Add(new Region() { Domain = "unordinalwestus", Name = "West US" });
            regions.Add(new Region() { Domain = "unordinaleastus", Name = "East US" });
            regions.Add(new Region() { Domain = "unordinalcentralindia", Name = "Central India" });
            regions.Add(new Region() { Domain = "unordinalgermanywestcent", Name = "Germany West Central" });

            return regions;
        }

        public struct Region
        {
            public string Domain { get; set; }
            public string Name { get; set; }
        }
    }

}