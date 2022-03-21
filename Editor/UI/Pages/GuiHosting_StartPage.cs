using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unordinal.Editor.UI;
using Unordinal.Editor.Utils;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace Unordinal.Editor
{
    public partial class GuiHosting
    {
        public event Action<PortChange> PortsChanged;

        private bool playWithFriendsToggled = false;
        private bool ShowOptions = false;

        private SceneAsset clientScene;
        private SceneAsset serverScene;

        private Button firstRemoveButton;
        private Button hiddenSettingsButton;
        private VisualElement settingsContainer;
        private VisualElement settingsContentContainer;
        private VisualElement portsContainer;
        private VisualElement clientSceneContainer;
        private ObjectField _ServerField;
        private ObjectField _ClientField;
        private VisualElement SceneContainer;
        private VisualElement warningsContainer;
        private Button deployButton;
        private Toggle playWithFriendsToggleButton;
        private VisualElement arrowContainer;
        private Image arrowUpImage;
        private Image arrowDownImage;

        private string sceneFieldTooltip = "When 'None' is selected, the scene at index 0 in build settings will be used.";

        private List<Port> ports = new List<Port> { new Port { Number = 7777, Protocol = Protocol.UDP } };
        public List<Port> Ports
        {
            get { return ports; }
            set
            {
                ports = value;
                anyUserAddedPorts = true;
                RenderPorts();
            }
        }

        private bool anyUserAddedPorts;

        private UnityEngine.Object LoadServerStartScene()
        {
            var result = new UnityEngine.Object();

            if(EditorPrefs.HasKey(UnordinalKeys.serverStartSceneKey))
            {
                var scenePath = EditorPrefs.GetString(UnordinalKeys.serverStartSceneKey);
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if(sceneAsset != null)
                {
                    result = sceneAsset;
                }
            }

            return result;
        }

        private void StoreServerStartScene(SceneAsset scene)
        {
            EditorPrefs.SetString(UnordinalKeys.serverStartSceneKey, GetScenePath(scene));
        }

        private UnityEngine.Object LoadClientStartScene()
        {
            var result = new UnityEngine.Object();

            if (EditorPrefs.HasKey(UnordinalKeys.clientStartSceneKey))
            {
                var scenePath = EditorPrefs.GetString(UnordinalKeys.clientStartSceneKey);
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    result = sceneAsset;
                }
            }

            return result;
        }

        private void StoreClientStartScene(SceneAsset scene)
        {
            EditorPrefs.SetString(UnordinalKeys.clientStartSceneKey, GetScenePath(scene));
        }

        private UnityEngine.Object GetFirstScene()
        {
            if (EditorBuildSettings.scenes.Length <= 0) return new UnityEngine.Object();

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
        }

        public VisualElement CreateStartPage()
        {
            var page = CreatePageBase(
                "This is where it all starts!",
                "Your game is specified to run on a specific port, make sure to add the same port below."
                );

            settingsContainer = new VisualElement();
            settingsContainer.AddToClassList("board");

            settingsContentContainer = new VisualElement();
            settingsContentContainer.style.paddingTop = 20;
            settingsContentContainer.style.paddingBottom = 20;
            settingsContentContainer.style.paddingLeft = 25;
            settingsContentContainer.style.paddingRight = 25;

            portsContainer = new VisualElement();
            RenderPorts();

            // This container will contain the scene settings for server & client.
            SceneContainer = new VisualElement();

            // pick the scene of client and server
            _ServerField = new ObjectField("Server Start Scene");
            _ServerField.tooltip = sceneFieldTooltip;
            _ServerField.style.marginTop = 13;
            _ServerField.objectType = typeof(UnityEngine.SceneManagement.Scene);
            _ServerField.value = LoadServerStartScene();
            _ServerField.RegisterValueChangedCallback((_val) =>
            {
                serverScene = (SceneAsset)_val.newValue;

                StoreServerStartScene(serverScene);

                // Evalutate if deploy button should be enabled
                CheckIfStartSceneIsMissing();
            });

            // Client container which gets collapsed (and child items can hence have whatever margin/etc. and we just collapse the parent container.
            // Thise one should not have any margin/etc on it since we will collapse it and then we dont want to store values
            // for reseting the margin etc.. it's easier to just let this container be default. And adjusting the children instead.
            clientSceneContainer = new VisualElement();

            _ClientField = new ObjectField("Client Start Scene");
            _ClientField.objectType = typeof(UnityEngine.SceneManagement.Scene);
            _ClientField.tooltip = sceneFieldTooltip;
            _ClientField.value = LoadClientStartScene();
            _ClientField.style.marginTop = 15;
            HandleClientSceneFieldVisibility(playWithFriendsToggled);
            _ClientField.RegisterValueChangedCallback((_val) =>
            {
                clientScene = (SceneAsset)_val.newValue;

                StoreClientStartScene(clientScene);

                // Evalutate if deploy button should be enabled
                CheckIfStartSceneIsMissing();
            });

            arrowContainer = new VisualElement();
            //arrowContainer.style.alignContent = Align.Center;
            //arrowContainer.style.alignItems = Align.Center;
            arrowContainer.style.alignSelf = Align.Center;

            // a drop down button 
            arrowDownImage = Assets.Images["ArrowDown"];
            arrowDownImage.AddToClassList("arrow");

            arrowUpImage = Assets.Images["ArrowUp"];
            arrowUpImage.AddToClassList("arrow");

            var optionsLabel = new Label("OPTIONS");
            optionsLabel.style.fontSize = 14;
            optionsLabel.style.alignContent = Align.Center;
            optionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            optionsLabel.style.marginLeft = 3;

            hiddenSettingsButton = new Button();
            hiddenSettingsButton.AddToClassList("dropdownButton");
            hiddenSettingsButton.clicked += () =>
            {
                ShowOptions = !ShowOptions;

                HandleOptionsVisibility(ShowOptions);
            };

            // Create information section about why deploy can't be started.
            warningsContainer = CreateWarningAboutMissingScenes();

            // Process button
            deployButton = Controls.BigButton("Deploy now");
            deployButton.style.marginTop = 40;
            deployButton.style.marginBottom = 25;
            deployButton.clicked += () =>
            {
                // Clean up (deploy should only be started if build is successful).
                EditorPrefs.SetBool(UnordinalKeys.shouldDoADeployKey, false);

                // We are on main thread, build server/client.
                originalGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                originalTarget = EditorUserBuildSettings.activeBuildTarget;
                HandleScenesInBuildSettings();
                PreDeploy();
            };

            // Play with friends toggle button
            var playWithFriendsTooltip = "When enabled, a client will be built and made available for download. In addition to the deployed and hosted server.";
            //var pwfLabel = new Label("playWithFriends");
            playWithFriendsToggleButton = new Toggle();
            playWithFriendsToggleButton.AddToClassList("play-with-friends-toggle-button");
            playWithFriendsToggleButton.text = "PLAY WITH FRIENDS";
            playWithFriendsToggleButton.tooltip = playWithFriendsTooltip;
            playWithFriendsToggleButton.value = playWithFriendsToggled;
            var checkBoxColor = new Color(3.0f / 255.0f, 132.0f / 255.0f, 150.0f / 255.0f);
            var playWithFriendsCheckmark = playWithFriendsToggleButton.Children().ElementAt(0).Children().ElementAt(0);
            playWithFriendsCheckmark.style.height = 25;
            playWithFriendsCheckmark.style.width = 25;
            playWithFriendsCheckmark.style.borderRightWidth = 2;
            playWithFriendsCheckmark.style.borderLeftWidth = 2;
            playWithFriendsCheckmark.style.borderTopWidth = 2;
            playWithFriendsCheckmark.style.borderBottomWidth = 2;
            playWithFriendsCheckmark.style.borderRightColor = checkBoxColor;
            playWithFriendsCheckmark.style.borderLeftColor = checkBoxColor;
            playWithFriendsCheckmark.style.borderTopColor = checkBoxColor;
            playWithFriendsCheckmark.style.borderBottomColor = checkBoxColor;
            var checkmarkCornerRadius = 4;
            playWithFriendsCheckmark.style.borderBottomLeftRadius = checkmarkCornerRadius;
            playWithFriendsCheckmark.style.borderBottomRightRadius = checkmarkCornerRadius;
            playWithFriendsCheckmark.style.borderTopLeftRadius = checkmarkCornerRadius;
            playWithFriendsCheckmark.style.borderTopRightRadius = checkmarkCornerRadius;
            playWithFriendsCheckmark.style.marginRight = 5;
            playWithFriendsCheckmark.style.unityBackgroundImageTintColor = Color.white;
            var playWithFriendsLabel = playWithFriendsToggleButton.Children().ElementAt(0).Children().ElementAt(1);
            playWithFriendsLabel.style.height = 25;
            playWithFriendsLabel.style.fontSize = 16;
            playWithFriendsLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            var test = playWithFriendsToggleButton.Children().ElementAt(0);
            playWithFriendsToggleButton.RegisterValueChangedCallback(_val =>
            {
                SetPlayWithFriendsValue(_val.newValue);

                // Evaluate if deploy button should be disabled.
                CheckIfStartSceneIsMissing();
            });

            // Dashboard button
            Button dashBoardButton = Controls.BigButtonWithoutBackground("Go to dashboard");
            dashBoardButton.clicked += OnDashboard;

            CheckIfStartSceneIsMissing(); // Evaluate if warning should be visible when generate.
            EditorBuildSettings.sceneListChanged += CheckIfStartSceneIsMissing;

            // Layout
            {
                page.Add(settingsContainer);
                {
                    //settingsContainer.Add(optionsLabel);
                    //settingsContainer.Add(settingsContentContainer);
                    {
                        settingsContentContainer.Add(portsContainer);
                        settingsContentContainer.Add(SceneContainer);
                        {
                            SceneContainer.Add(_ServerField);
                            SceneContainer.Add(clientSceneContainer);
                            {
                                clientSceneContainer.Add(_ClientField);
                            }
                        }
                    }
                }
                page.Add(hiddenSettingsButton);
                {
                    hiddenSettingsButton.Add(arrowContainer);
                    {
                        arrowContainer.Add(arrowDownImage);
                    }
                    hiddenSettingsButton.Add(optionsLabel);
                }
                page.Add(warningsContainer);
                page.Add(deployButton);
                page.Add(playWithFriendsToggleButton);
                page.Add(dashBoardButton);
            }

            Task.Delay(1).ContinueWith(delegate
            {
                ShowOptions = EditorPrefs.GetBool(UnordinalKeys.OptionsVisibleKey, false);
                if(ShowOptions)
                {
                    // At this point in the code execution, we only want to expand settings if all settings should be visible.
                    // Otherwise we run the risk of showing the default port 7777 for 0.5 sec until the port finder has found
                    // a port. In which case the port will be hidden when collapsed, and the default port 7777 will be removed
                    // after 0.5 seconds.
                    HandleOptionsVisibility(ShowOptions);
                }

                var pwfEnabled = EditorPrefs.GetBool(UnordinalKeys.playWithFriendsEnabledKey, false);
                SetPlayWithFriendsValue(pwfEnabled);

            }, TaskScheduler.FromCurrentSynchronizationContext());

            return page;
        }

        private VisualElement CreateWarningAboutMissingScenes()
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(134.0f / 255.0f, 6.0f / 255.0f, 6.0f / 255.0f);
            var borderRadius = 10;
            container.style.borderBottomLeftRadius = borderRadius;
            container.style.borderBottomRightRadius = borderRadius;
            container.style.borderTopLeftRadius = borderRadius;
            container.style.borderTopRightRadius = borderRadius;

            var warningTextSize = 10;
            var deployWarningTitle = new Label("DEPLOY DISABLED:");
            deployWarningTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            deployWarningTitle.style.color = Color.white;
            deployWarningTitle.style.fontSize = warningTextSize + 2;
            deployWarningTitle.style.marginBottom = 4;

            var deployWarningText = new Label("ADD SCENE TO BUILD SETTINGS");
            deployWarningText.style.unityFontStyleAndWeight = FontStyle.Italic;
            deployWarningText.style.color = Color.white;
            deployWarningText.style.fontSize = warningTextSize;

            // Layout
            {
                container.Add(deployWarningTitle);
                container.Add(deployWarningText);
            }

            return container;
        }

        private void HandleScenesInBuildSettings()
        {
            // Add server scene to build settings.
            SceneAsset serverSceneAsset = _ServerField.value != null ? (SceneAsset)_ServerField.value : null;
            AddSceneToBuildSettings(serverSceneAsset, WhichBuild.Server);

            if (playWithFriendsToggled)
            {
                // Add client scene to build settings.
                SceneAsset clientSceneAsset = _ClientField.value != null ? (SceneAsset)_ClientField.value : null;
                AddSceneToBuildSettings(clientSceneAsset, WhichBuild.Client);
            }
        }

        private void CheckIfStartSceneIsMissing()
        {
            var hasScenes = EditorBuildSettings.scenes.Count() != 0;
            HandleWarningVisibility(!hasScenes);
        }

        private void HandleWarningVisibility(bool ShowWarning)
        {
            var serverSceneSpecified = _ServerField.value != null;
            var clientSceneSpecified = _ClientField.value != null;
            var pluginHasStartScenes = playWithFriendsToggled ? serverSceneSpecified && clientSceneSpecified : serverSceneSpecified;
            if(pluginHasStartScenes)
            {
                // Warning should not be visible if the start scene fields are specified in the plugin.
                ShowWarning = false;
            }

            deployButton.SetEnabled(!ShowWarning); // Deploy button is enabled when there is no warning.
            warningsContainer.visible = ShowWarning;
            var size = ShowWarning ? new StyleLength(StyleKeyword.Auto) : 0;
            warningsContainer.style.height = size;
            warningsContainer.style.width = size;
            // Margin
            var margin = ShowWarning ? 30 : 0;
            warningsContainer.style.marginTop = margin;
            warningsContainer.style.marginBottom = -margin;
            // Padding
            var padding = ShowWarning ? 10 : 0;
            warningsContainer.style.paddingLeft = padding;
            warningsContainer.style.paddingRight = padding;
            warningsContainer.style.paddingBottom = padding;
            warningsContainer.style.paddingTop = padding;
        }

        private void SetPlayWithFriendsValue(bool toggled)
        {
            playWithFriendsToggled = toggled;
            if (playWithFriendsToggleButton.value != toggled)
            {
                playWithFriendsToggleButton.value = toggled;
            }

            EditorPrefs.SetBool(UnordinalKeys.playWithFriendsEnabledKey, toggled);

            HandleClientSceneFieldVisibility(toggled);

            // Method is inside a different file, but still in same partial class.
            HandlePlayWithFriendsResultVisibility(toggled);
        }

        private void HandleClientSceneFieldVisibility(bool visible)
        {
            if (visible)
            {
                // Show client scene option
                clientSceneContainer.visible = true;
                clientSceneContainer.style.height = new StyleLength(StyleKeyword.Auto); ;
            }
            else
            {
                // Hide client scene option
                clientSceneContainer.visible = false;
                clientSceneContainer.style.height = 0;
            }
        }

        private void HandleOptionsVisibility(bool visible, bool initialization = false)
        {
            ShowOptions = visible;

            if (!initialization)
            {
                EditorPrefs.SetBool(UnordinalKeys.OptionsVisibleKey, visible);

                void RemoveIfExists(Image image)
                {
                    if (arrowContainer.Children().Contains(image))
                    {
                        arrowContainer.Remove(image);
                    }
                }
                void AddIfNotExists(Image image)
                {
                    if (!arrowContainer.Children().Contains(image))
                    {
                        arrowContainer.Add(image);
                    }
                }

                if (visible)
                {
                    RemoveIfExists(arrowDownImage);
                    AddIfNotExists(arrowUpImage);
                }
                else
                {
                    RemoveIfExists(arrowUpImage);
                    AddIfNotExists(arrowDownImage);
                }
            }

            if(!hasFoundPorts)
            {
                // When we havn't automatically found a port, we need to show
                // the port settings even when options are collapsed.
                if (!settingsContainer.Contains(settingsContentContainer))
                    settingsContainer.Add(settingsContentContainer);

                // Toggling options button will still hide/show the rest
                // of the options.
                HandleSceneVisibility(visible);
            }
            else
            {
                // We have automatically found a port, we can now hide/show
                // all of the options upon options toggle.

                // Scenes always visible sinze we are hiding/showing parent element.
                HandleSceneVisibility(true);

                if (visible)
                {
                    if (!settingsContainer.Contains(settingsContentContainer))
                        settingsContainer.Add(settingsContentContainer);
                }
                else
                {
                    if(settingsContainer.Contains(settingsContentContainer))
                    {
                        settingsContainer.Remove(settingsContentContainer);
                    }
                }
            }
        }

        private void HandleSceneVisibility(bool visible)
        {
            if (visible)
            {
                SceneContainer.style.height = new StyleLength(StyleKeyword.Auto);
                SceneContainer.style.width = new StyleLength(StyleKeyword.Auto);
                SceneContainer.visible = true;
            }
            else
            {
                SceneContainer.style.height = 0;
                SceneContainer.style.width = 0;
                SceneContainer.visible = false;
            }
        }

        private void AddSceneToBuildSettings(SceneAsset sceneToAdd, WhichBuild buildType)
        {
            if (sceneToAdd == null)
            {
                // User has selected "None" as the scene, clear EditorPrefs.
                EditorPrefs.DeleteKey(UnordinalKeys.PlatformKey(buildType));
                return;
            }

            // Find the scene path for the scene we want to add.
            var pathForSceneToAdd = string.Empty;
            pathForSceneToAdd = GetScenePath(sceneToAdd);

            var _editorSceneToAdd = new EditorBuildSettingsScene(pathForSceneToAdd, true);
            var _scenesInBuildSettings = EditorBuildSettings.scenes.Where(_scene => _scene.enabled).ToList();

            bool sceneIsNotInBuildSettings = _scenesInBuildSettings.FindIndex(_scene => _scene.path == _editorSceneToAdd.path) == -1;
            if (sceneIsNotInBuildSettings)
            {
                // Add the scene on index 0.
                _scenesInBuildSettings.Insert(0, _editorSceneToAdd);
            }
            else
            {
                var sceneIsNotAtIndexZero = _scenesInBuildSettings[0].path != _editorSceneToAdd.path;
                if (sceneIsNotAtIndexZero)
                {
                    // The selected scene is not at index 0, swap.

                    int _index = _scenesInBuildSettings.FindIndex(_s => _s.path == _editorSceneToAdd.path);
                    _scenesInBuildSettings[_index] = _scenesInBuildSettings[0];
                    _scenesInBuildSettings[0] = _editorSceneToAdd;
                }
            }
            
            EditorBuildSettings.scenes = _scenesInBuildSettings.ToArray(); // Update Unitys build settings.
            StorePluginConfiguration(EditorBuildSettings.scenes, buildType); // Save in EditorPrefs.
        }

        private string GetScenePath(SceneAsset scene)
        {
            if (scene == null) return string.Empty;

            var result = string.Empty;

            var _allScenesInProject = AssetDatabase.FindAssets("t:Scene");
            foreach (var _assetGUID in _allScenesInProject)
            {
                string _path = AssetDatabase.GUIDToAssetPath(_assetGUID);
                var _scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_path);

                if (_scene != null && _scene.GetInstanceID() == scene.GetInstanceID())
                {
                    result = _path;
                    break;
                }
            }

            return result;
        }

        private void StorePluginConfiguration(EditorBuildSettingsScene[] scenes, WhichBuild buildType)
        {
            string _value = string.Empty;

            for (int i = 0; i < scenes.Length; i++)
            {
                _value += i == 0 ? $"{scenes[i].path}" : $",{scenes[i].path}";
            }

            EditorPrefs.SetString(UnordinalKeys.PlatformKey(buildType), _value);
        }

        private void RenderPorts(bool calledFromPortFinder = false)
        {
            if (portsContainer == null) return;

            portsContainer.Clear();
            foreach (var port in Ports)
            {
                CreateProcessSettings(port);
            }

            if(calledFromPortFinder)
            {
                // Ports can have been found.
                // Evaluate options visibility
                HandleOptionsVisibility(ShowOptions);
            }
        }

        private void CreateProcessSettings(Port port)
        {
            var row = CreatePortAndProtocol(port);
            Button addBtn = (Button)CreateAddRemovePortButton(true);
            addBtn.clicked += OnAddPort;
            row.Add(addBtn);
            row.style.marginBottom = 10; // Override unity style sheet

            Button removeBtn = (Button)CreateAddRemovePortButton(false);
            removeBtn.name = "remove-button";
            removeBtn.clicked += OnRemovePort(port, row);

            if (portsContainer.childCount == 0)
            {
                firstRemoveButton = removeBtn;
            }

            // Layout
            portsContainer.Add(row);
            {
                row.Add(removeBtn);
            }

            EvaluateRemoveButonVisibility();
        }

        private void EvaluateRemoveButonVisibility()
        {
            firstRemoveButton.visible = (portsContainer.childCount > 1);
        }

        VisualElement CreatePortAndProtocol(Port port)
        {
            VisualElement rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;

            var portTooltip = "The port number can be found inside the transport added to the NetworkManager component";
            var portLabel = new Label("Port");
            portLabel.tooltip = portTooltip;
            var portInput = new IntegerField();
            portInput.tooltip = portTooltip;
            portInput.RegisterValueChangedCallback(x =>
            {
                port.Number = x.newValue;
                PortsChanged?.Invoke(PortChange.PortNumber);
            });
            portInput.AddToClassList("port-input");
            portInput.name = "port-field";
            portInput.value = port.Number;

            var protocolLabel = new Label("Protocol");
            var protocolComboBox = new EnumField(port.Protocol);
            protocolComboBox.RegisterValueChangedCallback(x =>
            {
                port.Protocol = (Protocol)(x.newValue);
                PortsChanged?.Invoke(PortChange.PortProtocol);
            });
            protocolComboBox.AddToClassList("port-input");
            protocolComboBox.name = "protocol-field";

            // Layout
            {
                // Port label
                rowContainer.Add(portLabel);
                // Port input
                rowContainer.Add(portInput);

                // Protocol label
                rowContainer.Add(protocolLabel);
                // Protocol input
                rowContainer.Add(protocolComboBox);
            }

            return rowContainer;
        }

        private VisualElement CreateAddRemovePortButton(bool isAddButton)
        {
            return Controls.ButtonWithClass(isAddButton ? "+" : "-", false, "add-remove-port-button");
        }

        private void OnAddPort()
        {
            anyUserAddedPorts = true;
            var newPort = new Port
            { Number = 7777, Protocol = Protocol.UDP };
            Ports.Add(newPort);
            CreateProcessSettings(newPort);
            PortsChanged?.Invoke(PortChange.PortAdded);
        }

        private Action OnRemovePort(Port port, VisualElement row)
        {
            return (() =>
            {
                Ports.Remove(port);

                if (portsContainer.childCount > 1)
                {
                    portsContainer.Remove(row);
                }
                EvaluateRemoveButonVisibility();
                var removeButtons = portsContainer.Query<Button>().Where(b => b.name == "remove-button").ToList();
                if (removeButtons.Count == 1)
                {
                    removeButtons[0].visible = false;
                    firstRemoveButton = removeButtons[0];
                }
                PortsChanged?.Invoke(PortChange.PortRemoved);
            });
        }
    }
}
