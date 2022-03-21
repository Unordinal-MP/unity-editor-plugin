using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Unity;
using Unity.Extension;
using FactoryLifetime = Unity.FactoryLifetime;
namespace Unordinal.Editor.Utils
{
    public class LoggingExtension: UnityContainerExtension
    {
        private static readonly MethodInfo LoggerFactoryLogMethod
            = typeof(LoggerFactoryExtensions).
              GetMethods(BindingFlags.Static | BindingFlags.Public).
              First(x => x.ContainsGenericParameters);
        
        private readonly ILoggerFactory loggerFactory;

        public LoggingExtension(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        protected override void Initialize()
        {
            Container.RegisterFactory(
                typeof(ILogger<>), 
                UnityContainer.All, 
                (container, type, name) => LoggerFactoryLogMethod
                                           .MakeGenericMethod(type.GenericTypeArguments[0])
                                           .Invoke(null, new object[] { loggerFactory }),
                FactoryLifetime.Singleton);
        }
    }
}
