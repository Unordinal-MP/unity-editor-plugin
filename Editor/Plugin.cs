using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Unity;
using Unity.Injection;
using UnityEditor;
using UnityEngine;
using Unordinal.Editor.External;
using Unordinal.Editor.Services;
using Unordinal.Editor.Utils;
using Unordinal.Hosting;
using LoggingExtension = Unordinal.Editor.Utils.LoggingExtension;

namespace Unordinal.Editor
{
    public sealed class Plugin
    {
        private static readonly Plugin plugin = new Plugin();

        public static Plugin Instance { get { return plugin; } }

        static Plugin()
        {
        }

        /// <summary>
        /// IoC Container for the plugin
        /// </summary>
        private readonly UnityContainer container;

        public GuiEventManager EventManager { get; } = new GuiEventManager();

        private Plugin()
        {
            container = InitializeUnityContainer();
        }

        private UnityContainer InitializeUnityContainer()
        {
            // A container is a list of items that can be used as parameters to create classes.
            var container = new UnityContainer();
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddProvider(new UnityLoggerProvider())
                       .AddConsole();
            });
            container.AddExtension(new LoggingExtension(loggerFactory));
            //container.AddExtension(new PlayerPrefExtension(EventManager));

            // Register a type with specific members to be injected as singleton.
            container.RegisterSingleton<UserContextProvider>();
            // When an object of type ITokenStorage is to be created, make it a UserContextprovider.
            container.RegisterType<ITokenStorage, UserContextProvider>();
            // When an object of type IUserInfoHolder is to be created, make it a UserContextprovider.
            container.RegisterType<IUserInfoHolder, UserContextProvider>();

            // Register an instance of an object.
            // This object can then be resolved and used by other registrations.
            container.RegisterInstance("anonymousUrls", new List<string>() {
                $"{PluginSettings.Auth0BaseUrl}/oauth/device/code",
                $"{PluginSettings.Auth0BaseUrl}/oauth/token",
                $"{PluginSettings.ApiBaseUrl}hosting/{PluginSettings.GetPluginVersion()}/CheckPluginSupport/"
            });

            container.RegisterSingleton<HttpMessageHandler, RefreshTokenHttpMessageHandler>(new InjectionConstructor(
                new ResolvedParameter<Lazy<Auth0Client>>(),
                new ResolvedParameter<ITokenStorage>(),
                new ResolvedParameter<List<string>>("anonymousUrls"),
                new HttpClientHandler()
                {
                    UseDefaultCredentials = true,
                    UseCookies = true
                }
            ));

            container.RegisterSingleton<HttpClient>(new InjectionConstructor(
                new ResolvedParameter<HttpMessageHandler>()));
            return container;
        }

        internal T BuildUp<T>(Type type, T obj)
        {
            return (T) container.BuildUp(type, obj);
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
#endif

        private static void SnapBackOutsideUnityWindow(GuiHosting window)
        {
            if (window == null)
                return;

            //attempted fix for bug where you no longer see the Unordinal window, click the menu, and nothing happens
            //because it is somewhere outside the screen

            //Unity REALLY has no good way of getting size of Unity window, a suggested fix floating online called UnityStats.screenRes isn't working in current version
            //default to screen resolution
            int x = 0;
            int y = 0;
            int w = Screen.currentResolution.width;
            int h = Screen.currentResolution.height;
#if UNITY_EDITOR_WIN
            //better quality possible with native Win32 calls
            IntPtr hWnd = GetActiveWindow(); //this is some sub-window of Unity
            hWnd = GetParent(hWnd); //main Unity window
            RECT workRect = new RECT();
            GetClientRect(hWnd, out workRect);
            w = workRect.right;
            h = workRect.bottom;
            GetWindowRect(hWnd, out workRect);
            x = workRect.left;
            y = workRect.top;
#endif

            var rect = EditorGUIUtility.PointsToPixels(window.position);
            const int margin = 10;
            if (rect.xMax < x + margin || rect.yMax < y + margin || rect.x >= x + w - margin || rect.y >= y + h - margin)
            {
                rect = window.position;
                rect.x = 0;
                rect.y = 0;
                window.position = rect;
            }
        }

        [MenuItem("Tools/Hosting")]
        public static GuiHosting InitializeHosting()
        {
            var window = EditorWindow.GetWindow<GuiHosting>();
            window.Show();
            window.minSize = new Vector2(480, 550);
            window.titleContent = new GUIContent("Unordinal Hosting");

            SnapBackOutsideUnityWindow(window);
            
            return window;
        }
    }
}
