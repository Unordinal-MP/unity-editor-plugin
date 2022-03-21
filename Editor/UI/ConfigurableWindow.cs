using System;
using UnityEditor;
using UnityEngine;
using Unordinal.Editor.Utils;
namespace Unordinal.Editor.UI
{
    public abstract class ConfigurableWindow : EditorWindow
    {
        private static readonly UnityVersion FirstVersionSupportingCreateGUI = new UnityVersion { Major = 2019, Minor = 4, Patch = 34 };
        
        private Lazy<IGuiEventPublisher> plugin = new Lazy<IGuiEventPublisher>(() => Plugin.Instance.EventManager);

        private IGuiEventPublisher eventPublisher {
            get {
                return plugin.Value;
            }
        }

        [NonSerialized]
        private bool guiInitialized;

        [NonSerialized]
        private bool enabled;

        public bool IsInitialized
        {
            get
            {
                return guiInitialized && enabled;
            }
        }

        private void OnEnable()
        {
            eventPublisher.BeforeEnabled();
            Plugin.Instance.BuildUp(GetType(), this);
            Enable();
            eventPublisher.AfterEnabled();
            enabled = true;

            if (UnityVersion.Parse(Application.unityVersion) < FirstVersionSupportingCreateGUI)
            {
                // then we shall call CreateGUI ourselves
                CreateGUI();
            }
        }

        private void OnFocus()
        {
            if (IsInitialized)
            {
                OnFocused();
            }
        }

        private void OnDisable()
        {
            enabled = false;
        }

        private void Update()
        {
            eventPublisher.BeforeUpdated();
            eventPublisher.AfterUpdated();
        }


        public void CreateGUI()
        {
            guiInitialized = false;
            eventPublisher.BeforeCreated();
            DoCreateGUI();
            guiInitialized = true;
        }

        protected virtual void OnFocused() { }

        protected virtual void Enable() { }

        protected abstract void DoCreateGUI();

    }
}
