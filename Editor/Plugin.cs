using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Unity;
using Unity.Injection;
using Unity.Microsoft.Logging;
using UnityEditor;

namespace Unordinal.Hosting
{
    /// <summary>
    /// IoC Container for the plugin
    /// </summary>
    public class Plugin
    {
        internal static readonly UnityContainer container = new UnityContainer();

        internal static ILogger<Plugin> logger;

        private Plugin() { }

        static Plugin()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => {
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddProvider(new UnityLoggerProvider())
                       .AddConsole();
            });
            container.AddExtension(new LoggingExtension(loggerFactory));

            logger = loggerFactory.CreateLogger<Plugin>();

            container.RegisterSingleton<Auth0Client>();
            container.RegisterSingleton<ServerBundler>();
            container.RegisterSingleton<TarGzArchiver>();
            container.RegisterSingleton<UnityServerBuilder>();
            container.RegisterSingleton<FileUploader>();

            var userContextProvider = new UserContextProvider();
            container.RegisterInstance<ITokenStorage>(userContextProvider);
            container.RegisterInstance<IUserInfoHolder>(userContextProvider);
            container.RegisterInstance<List<string>>("anonymousUrls", new List<string>() {
                $"{PluginSettings.Auth0BaseUrl}/oauth/device/code",
                $"{PluginSettings.Auth0BaseUrl}/oauth/token"
            });
            container.RegisterSingleton<RefreshTokenHttpMessageHandler>(new InjectionConstructor(
                new ResolvedParameter<Lazy<Auth0Client>>(),
                new ResolvedParameter<ITokenStorage>(),
                new ResolvedParameter<List<string>>("anonymousUrls")));
            container.RegisterSingleton<HttpClient>(new InjectionConstructor(
                new ResolvedParameter<RefreshTokenHttpMessageHandler>()));
            container.RegisterSingleton<UnordinalApi>();
        }
    }

    public abstract class ConfigurableWindow<T>: EditorWindow where T: EditorWindow
    {

        public static T Initialize()
        {
            Plugin.logger.LogDebug(String.Format("Opening window of type {0}", typeof(T)));
            if (EditorWindow.HasOpenInstances<T>())
            {
                Plugin.logger.LogDebug(String.Format("Window of type {0} already exists", typeof(T)));
                return EditorWindow.GetWindow<T>();
            }
            Plugin.logger.LogDebug(String.Format("Creating window of type {0}", typeof(T)));
            T window = Plugin.container.BuildUp(EditorWindow.CreateWindow<T>());
            window.Show();
            return window;
        }

        public void OnEnable()
        {
            Plugin.container.BuildUp(typeof(T), this);
            AfterEnabled();
        }

        protected abstract void AfterEnabled();

    }
}
