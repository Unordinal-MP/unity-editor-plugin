using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unordinal.Editor.Services;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        private bool hasFoundPorts = false;

        private bool _startPortFinder = false; //setting it to false at start so that port finding doesn't trigger until asked
        private bool _isFindingPorts;
        private void StartFindingPorts()
        {
            _startPortFinder = true;
        }

        private async Task RunPortFinding()
        {
            if (!_startPortFinder)
                return;
            if (_isFindingPorts)
                return;

            _isFindingPorts = true;
            await FindPortsAsync();
            _isFindingPorts = false;
        }

        private async Task FindPortsAsync()
        {
            var portFinderOutput = new PortFinder.Output();

            try
            {
                portFinderOutput = await portFinder.FindPorts();
            }
            catch (PortFinder.ApplicationIsPlayingException)
            {
                return;
            }
            catch (Exception ex)
            {
                //I've had troubles with Unity not printing certain exceptions in plugin for some reason
                Debug.LogException(ex);
            }

            hasFoundPorts = portFinderOutput.Ports.Count() > 0;

            ports = ports.Union(portFinderOutput.Ports).ToList();
            
            if (ports.Count == 0)
            {
                //otherwise add button disappears...
                ports.Add(new Port() { Number = 7777, Protocol = Protocol.UDP });
            }
            else if (ports.Count > 1 && !anyUserAddedPorts)
            {
                //remove auto added one
                ports.Remove(new Port() { Number = 7777, Protocol = Protocol.UDP });
            }

            _startPortFinder = false;
            RenderPorts(true);
        }
    }
}
