using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unordinal.Editor.Services
{
    public class PortFinder
    {
        private const int _yieldTimeMs = 10;
        private const int _yieldAfterTimeMs = 100;

        private readonly ILogger<PortFinder> logger;

        public PortFinder(ILogger<PortFinder> logger)
        {
            this.logger = logger;
        }

        public sealed class Output
        {
            public List<Port> Ports = new List<Port>();
            public bool AreStartScenesNeeded
            {
                get
                {
                    return _startSceneSettings != StartSceneSettings.StartScenesNotNeeded;
                }
            }

            private StartSceneSettings _startSceneSettings;

            public void MarkScenesNeeded()
            {
                if (_startSceneSettings == StartSceneSettings.Default || _startSceneSettings == StartSceneSettings.StartScenesNeeded)
                {
                    _startSceneSettings = StartSceneSettings.StartScenesNeeded;
                }
                else
                {
                    _startSceneSettings = StartSceneSettings.ConflictingResults;
                }
            }

            public void MarkScenesNotNeeded()
            {
                if (_startSceneSettings == StartSceneSettings.Default || _startSceneSettings == StartSceneSettings.StartScenesNotNeeded)
                {
                    _startSceneSettings = StartSceneSettings.StartScenesNotNeeded;
                }
                else
                {
                    _startSceneSettings = StartSceneSettings.ConflictingResults;
                }
            }
        }

        private sealed class YieldState
        {
            private double _timeAtLastYield;

            public bool CheckYield()
            {
                double time = EditorApplication.timeSinceStartup;
                bool shouldYield = time - _timeAtLastYield > 0.001 * _yieldAfterTimeMs;

                _timeAtLastYield = time;

                return shouldYield;
            }
        }

        private sealed class SharedTypeHandlerState
        {
            public bool AnyFishNetTransport;
            public bool AnyFishNetManager;
            public bool AnyMirageSocketFactory;
            public bool AnyMirageManager;
        }

        private enum StartSceneSettings
        {
            Default, //we haven't seen any interesting objects, so default to scenes are needed
            StartScenesNeeded,
            StartScenesNotNeeded,
            ConflictingResults, //assume they are needed
        }

        public sealed class ApplicationIsPlayingException : Exception
        {
            public ApplicationIsPlayingException()
                : base("EditorApplication.isPlaying is currently true, can't find ports")
            {
            }
        }

        private sealed class TypeHandler
        {
            public Type Type;
            public Type ConcreteType;

            private Action<Type, object> _checkTypeInstance { get; }
            private bool _showedException;

            public TypeHandler(Action<Type, object> checkTypeInstance)
            {
                _checkTypeInstance = checkTypeInstance;
            }

            public TypeHandler(TypeHandler basalType)
            {
                Type = basalType.Type;
                _checkTypeInstance = basalType._checkTypeInstance;
            }
            
            public void InvokeCheckTypeInstance(object instance)
            {
                try
                {
                    _checkTypeInstance(Type, instance);
                }
                catch (Exception ex)
                {
                    //we can't predict future API breakages
                    //and we don't want to spam user's log with potentially hundreds of messages
                    if (!_showedException)
                    {
                        _showedException = true;
                        Debug.LogError("Exception whilst checking instance of " + ConcreteType.FullName + " (see next error):");
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private static Type GetScriptTypeOfUnityObject(UnityEngine.Object o)
        {
            MonoScript script = o as MonoScript;
            if (script != null)
            {
                return script.GetClass();
            }
            ScriptableObject scriptableObject = o as ScriptableObject;
            if (scriptableObject != null)
            {
                return scriptableObject.GetType();
            }

            return null;
        }

        private sealed class ScriptSearch
        {
            private readonly Dictionary<Type, TypeHandler> _byType;
            private readonly HashSet<string> _processedScriptGuids = new HashSet<string>();
            private readonly HashSet<string> _interestingScriptGuids = new HashSet<string>();

            public ScriptSearch(Dictionary<Type, TypeHandler> byType)
            {
                _byType = byType;
            }

            public bool SearchFileForInterestingScripts(string path)
            {
                if (new FileInfo(path).Length > 50 * 1024 * 1024)
                    return false; //cut down on worst cases

                //this is the quickest way to find script refs and without overloading Unity
                var lines = File.ReadLines(path);
                bool first = true;
                foreach (var line in lines)
                {
                    if (first)
                    {
                        first = false;
                        if (!line.StartsWith("%YAML"))
                            return true;
                    }

                    if (!line.StartsWith("  m_Script: {fileID:"))
                        continue;

                    int guidBegins = line.IndexOf("guid: ") + 6;
                    if (guidBegins < 0)
                        continue;

                    int guidEnds = line.IndexOf(',', guidBegins);
                    string scriptGuid = line.Substring(guidBegins, guidEnds - guidBegins);

                    if (_interestingScriptGuids.Contains(scriptGuid))
                    {
                        return true;
                    }

                    if (_processedScriptGuids.Contains(scriptGuid))
                        continue;
                    _processedScriptGuids.Add(scriptGuid);

                    string scriptOrAssemblyPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
                    if (scriptOrAssemblyPath == string.Empty)
                        continue;
                    if (scriptOrAssemblyPath.StartsWith("Library/"))
                        continue;

                    UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(scriptOrAssemblyPath);
                    foreach (var o in all)
                    {
                        Type scriptType = GetScriptTypeOfUnityObject(o);
                        if (scriptType == null)
                            continue;
                        if (_byType.ContainsKey(scriptType))
                        {
                            _interestingScriptGuids.Add(scriptGuid);
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private static Task Yield()
        {
            return Task.Delay(_yieldTimeMs);
        }

        private async Task SearchLoadedAssemblies(Dictionary<string, TypeHandler> byName, YieldState yieldState)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (byName.TryGetValue(type.FullName, out TypeHandler typeHandler))
                    {
                        typeHandler.Type = type;
                        typeHandler.ConcreteType = type;
                    }
                    else
                    {
                        Type currentType = type;
                        Type basalType = type;

                        while (currentType != null)
                        {
                            if (currentType.FullName == null)
                            {
                                //wtf, see which type this is
                                break;
                            }

                            if (byName.ContainsKey(currentType.FullName))
                            {
                                basalType = currentType;
                                break;
                            }

                            currentType = currentType.BaseType;
                        }

                        if (basalType != type)
                        {
                            byName[type.FullName] = new TypeHandler(byName[basalType.FullName])
                            {
                                ConcreteType = type,
                            };
                        }
                    }
                }

                if (yieldState.CheckYield())
                    await Yield();
            }
        }

        public async Task<Output> FindPorts()
        {
            //note that Unity will always restarting environment when pushing play
            //this means that no FindPorts() will be in progress on restart
            //hence it is sufficient to check this here at start (rather in every await)
            if (EditorApplication.isPlaying)
            {
                throw new ApplicationIsPlayingException();
            }

            var yieldState = new YieldState();
            if (yieldState.CheckYield())
                await Yield();

            var output = new Output();
            var state = new SharedTypeHandlerState();
            var byName = new Dictionary<string, TypeHandler>();

            RegisterTypeHandlers(byName, output, state);

            await SearchLoadedAssemblies(byName, yieldState);

            var byType = new Dictionary<Type, TypeHandler>();

            foreach (var kv in byName)
            {
                TypeHandler typeHandler = kv.Value;
                if (typeHandler.ConcreteType == null)
                    continue; //unresolved

                byType[typeHandler.ConcreteType] = typeHandler;
            }

            var scriptSearch = new ScriptSearch(byType);

            await SearchScriptableObjects(byType, scriptSearch, yieldState);
            await SearchScenes(byType, scriptSearch, yieldState);

            if (state.AnyFishNetManager && !state.AnyFishNetTransport)
            {
                //FishNet automatically adds Tugboat at runtime if no transport exists
                output.Ports.Add(new Port() { Number = 7770, Protocol = Protocol.UDP });
            }

            if (state.AnyMirageManager && !state.AnyMirageSocketFactory)
            {
                //Mirage defaults to Mirror-like UDP. however not entirely sure of the mechanism at time of writing
                output.Ports.Add(new Port() { Number = 7777, Protocol = Protocol.UDP });
            }

            output.Ports = output.Ports.Distinct().ToList();
            return output;
        }

        private async Task SearchScriptableObjects(Dictionary<Type, TypeHandler> byType, ScriptSearch scriptSearch, YieldState yieldState)
        {
            string[] objects = AssetDatabase.FindAssets("t:ScriptableObject")
                .Select(s => AssetDatabase.GUIDToAssetPath(s))
                .ToArray();

            foreach (string path in objects)
            {
                if (path.StartsWith("Packages/"))
                    continue;
                if (path.StartsWith("Library/"))
                    continue;

                if (scriptSearch.SearchFileForInterestingScripts(path))
                {
                    UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var o in all)
                    {
                        Type scriptType = GetScriptTypeOfUnityObject(o);
                        if (scriptType == null)
                            continue;
                        if (byType.TryGetValue(scriptType, out TypeHandler typeHandler))
                        {
                            typeHandler.InvokeCheckTypeInstance(o);
                        }
                    }
                }

                if (yieldState.CheckYield())
                    await Yield();
            }
        }

        private async Task SearchScenes(Dictionary<Type, TypeHandler> byType, ScriptSearch scriptSearch, YieldState yieldState)
        {
            string[] scenes = AssetDatabase.FindAssets("t:Scene")
                .Select(s => AssetDatabase.GUIDToAssetPath(s))
                .ToArray();

            //preliminary filtering to find "interesting" scenes with at least one handled type
            //because checking every single scene in a large project is ridiculously time consuming

            var interestingScenes = new List<string>();

            foreach (string path in scenes)
            {
                //filters against library samples (rather than user's own application)
                //these are not perfect obviously
                if (path.StartsWith("Library/"))
                    continue;
                if (path.StartsWith("Assets/Mirror"))
                    continue;
                if (path.StartsWith("Assets/FishNet"))
                    continue;
                if (path.StartsWith("Assets/Mirage"))
                    continue;
                if (path.StartsWith("Assets/DarkRift"))
                    continue;
                if (path.Contains("Example")) //mirror & fishnet
                    continue;
                if (path.Contains("Demo"))
                    continue; //dark rift

                if (scriptSearch.SearchFileForInterestingScripts(path))
                {
                    interestingScenes.Add(path);
                }

                if (yieldState.CheckYield())
                    await Yield();
            }

            //scene processing, look for and check interesting type instances

            //we load scenes additively and then remove them because we don't want to ruin
            //the user's changes

            foreach (string sceneName in interestingScenes)
            {
                Scene openedScene = EditorSceneManager.OpenScene(sceneName, OpenSceneMode.Additive);

                GameObject[] objs = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in objs)
                {
                    foreach (MonoBehaviour script in obj.GetComponents<MonoBehaviour>())
                    {
                        if (script == null)
                            continue; //don't know why this happens, might be e.g. missing GUID reference?

                        if (byType.TryGetValue(script.GetType(), out TypeHandler typeHandler))
                        {
                            typeHandler.InvokeCheckTypeInstance(script);
                        }
                    }
                }

                EditorSceneManager.CloseScene(openedScene, true);

                //TODO: discuss risks of yielding to user when we're opening their scenes...
                //maybe there needs to be a clear warning label somewhere or it should remain blocked
                //if (yieldState.CheckYield())
                //    await Yield();
            }
        }

        private void RegisterTypeHandlers(Dictionary<string, TypeHandler> byName, Output output, SharedTypeHandlerState state)
        {
            void UdpPort(int port)
            {
                output.Ports.Add(new Port { Number = port, Protocol = Protocol.UDP });
            }

            void TcpPort(int port)
            {
                output.Ports.Add(new Port { Number = port, Protocol = Protocol.TCPIP });
            }

            // MIRROR ===============================================================

            byName["Mirror.NetworkManager"] = new TypeHandler((type, instance) =>
            {
                output.MarkScenesNotNeeded();
            });

            byName["kcp2k.KcpTransport"] = new TypeHandler((type, instance) =>
            {
                int port = (ushort)type.GetField("Port").GetValue(instance);
                UdpPort(port);
            });

            byName["Mirror.TelepathyTransport"] = new TypeHandler((type, instance) =>
            {
                int port = (ushort)type.GetField("port").GetValue(instance);
                TcpPort(port);
            });

            byName["Mirror.SimpleWeb.SimpleWebTransport"] = new TypeHandler((type, instance) =>
            {
                int port = (ushort)type.GetField("port").GetValue(instance);
                TcpPort(port);
            });

            // PHOTON ===============================================================

            void PhotonAppSettings(object appSettings)
            {
                Type appSettingsType = appSettings.GetType();

                object protocol = appSettingsType.GetField("Protocol").GetValue(appSettings);
                int protocolValue = (int)Convert.ChangeType(protocol, typeof(int)); //Enum.GetUnderlyingType(protocol.GetType())

                int port = (int)appSettingsType.GetField("Port").GetValue(appSettings);
                if (port == 0)
                {
                    //PUN2 defaults
                    if (protocolValue == 0) //UDP
                        port = 5055;
                    else
                        port = 4530;
                }

                if (protocolValue == 0) //UDP
                    UdpPort(port);
                else
                    TcpPort(port);
            }

            byName["Photon.Pun.ServerSettings"] = new TypeHandler((type, instance) =>
            {
                object appSettings = type.GetField("AppSettings").GetValue(instance);
                PhotonAppSettings(appSettings);
            });

            byName["Fusion.Photon.Realtime.PhotonAppSettings"] = new TypeHandler((type, instance) =>
            {
                object appSettings = type.GetField("AppSettings").GetValue(instance);
                PhotonAppSettings(appSettings);
            });

            // UNITY NETCODE ===============================================================

            byName["Unity.Netcode.Transports.UNET.UNetTransport"] = new TypeHandler((type, instance) =>
            {
                int connectPort = (int)type.GetField("ConnectPort").GetValue(instance);
                UdpPort(connectPort);
                int serverListenPort = (int)type.GetField("ServerListenPort").GetValue(instance);
                UdpPort(serverListenPort);
            });

            // FISHNET (don't google this) =================================================

            byName["FishNet.Managing.NetworkManager"] = new TypeHandler((type, instance) =>
            {
                state.AnyFishNetManager = true;
            });

            byName["FishNet.Tugboat.Tugboat"] = new TypeHandler((type, instance) =>
            {
                state.AnyFishNetTransport = true;
                int port = (ushort)type.GetField("_port", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
                UdpPort(port);
            });

            byName["FishNet.Bayou.Bayou"] = new TypeHandler((type, instance) =>
            {
                state.AnyFishNetTransport = true;
                int port = (ushort)type.GetField("_port", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
                TcpPort(port);
            });

            byName["FishySteamworks.FishySteamworks"] = new TypeHandler((type, instance) =>
            {
                state.AnyFishNetTransport = true;
                int port = (ushort)type.GetField("_port", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
                UdpPort(port);
            });

            byName["FishyFacepunch.FishyFacepunch"] = new TypeHandler((type, instance) =>
            {
                state.AnyFishNetTransport = true;
                int port = (ushort)type.GetField("_port", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
                UdpPort(port);
            });

            // MIRAGE =============================================================

            byName["Mirage.NetworkManager"] = new TypeHandler((type, instance) =>
            {
                state.AnyMirageManager = true;
            });

            byName["Mirage.Sockets.Udp.UdpSocketFactory"] = new TypeHandler((type, instance) =>
            {
                state.AnyMirageSocketFactory = true;
                int port = (ushort)type.GetField("Port").GetValue(instance);
                UdpPort(port);
            });

            // DARK RIFT 2 ===========================================================

            byName["DarkRift.Client.Unity.UnityClient"] = new TypeHandler((type, instance) =>
            {
                int port = (ushort)type.GetField("port", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
                UdpPort(port);
                TcpPort(port);
            });
        }
    }
}
