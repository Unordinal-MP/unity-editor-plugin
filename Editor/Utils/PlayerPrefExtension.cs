//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Text.Json;
//using Unity.Builder;
//using Unity.Events;
//using Unity.Extension;
//using Unity.Strategies;
//using UnityEngine;
//using static Unordinal.Editor.Utils.PlayerPrefAttribute;

//namespace Unordinal.Editor.Utils
//{
//    public class PlayerPrefExtension : UnityContainerExtension
//    {
//        internal static bool PlayerPrefsDirty = false;

//        private readonly PlayerPrefAttributeHandlingManager manager;

//        private readonly IGuiEventManager guiEventManager;

//        public PlayerPrefExtension(IGuiEventManager guiEventManager)
//        {
//            this.guiEventManager = guiEventManager;
//            this.manager = new PlayerPrefAttributeHandlingManager(guiEventManager);
//        }

//        protected override void Initialize()
//        {
//            Context.RegisteringInstance += (object sender, RegisterInstanceEventArgs args) =>
//            {
//                manager.AddPlayerPrefManagement(args.Instance, args.Instance.GetType().Name);
//            };
//            Context.Strategies.Add(new AddPlayerPrefManagementBuilderStrategy(manager), UnityBuildStage.Initialization);
//            this.guiEventManager.OnAfterUpdated += () =>
//            {
//                if (PlayerPrefsDirty)
//                {
//                    PlayerPrefs.Save();
//                    PlayerPrefsDirty = false;
//                }
//            };
//        }

//        private class AddPlayerPrefManagementBuilderStrategy : BuilderStrategy
//        {
//            private readonly PlayerPrefAttributeHandlingManager manager;

//            public AddPlayerPrefManagementBuilderStrategy(PlayerPrefAttributeHandlingManager manager)
//            {
//                this.manager = manager;
//            }

//            public override void PreBuildUp(ref BuilderContext context)
//            {
//                manager.AddPlayerPrefManagement(context.Existing, context.Existing.GetType().Name);
//            }
//        }

//        private class PlayerPrefAttributeHandlingManager
//        {
//            private readonly Dictionary<KeyValuePair<string, string>, object> handlers = new Dictionary<KeyValuePair<string, string>, object>(); // this is to keep a strong reference somewhere, and for deduplication

//            private readonly IGuiEventSource guiEventSource;

//            public PlayerPrefAttributeHandlingManager(IGuiEventSource guiEventSource)
//            {
//                this.guiEventSource = guiEventSource;
//            }

//            public void AddPlayerPrefManagement(object bean, string beanKey)
//            {
//                foreach (var propertyInfo in bean.GetType().GetProperties(PlayerPrefAttribute.SupportedPropertyModifiers))
//                {
//                    foreach (var attribute in propertyInfo.GetCustomAttributes(typeof(PlayerPrefAttribute), false) as PlayerPrefAttribute[])
//                    {
//                        AddPlayerPrefAttributeHandler(attribute, bean, beanKey);
//                    }
//                }
//            }

//            private void AddPlayerPrefAttributeHandler(PlayerPrefAttribute attribute, object bean, string beanKey)
//            {
//                var bindingKey = new KeyValuePair<string, string>(beanKey, attribute.fieldName);
//                if (!handlers.ContainsKey(bindingKey))
//                {
//                    handlers.Add(bindingKey, new PlayerPrefAttributeHandler(bean, attribute, guiEventSource));
//                }

//            }
//        }
//    }

//    [AttributeUsage(AttributeTargets.Property)]
//    public class PlayerPrefAttribute : Attribute
//    {
//        internal static BindingFlags SupportedPropertyModifiers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

//        public string Name { get; }

//        internal readonly string fieldName;

//        public PlayerPrefAttribute(string name, [CallerMemberName] string fieldName = null)
//        {
//            this.Name = name;
//            this.fieldName = fieldName;
//        }

//        public class PlayerPrefAttributeHandler
//        {
//            private object cachedValue;
//            private readonly PlayerPrefAttribute attribute;
//            private readonly WeakReference<object> target;
//            private readonly IGuiEventSource guiEventSource;
//            private PropertyInfo property;
//            private Func<string, object> reader;
//            private Action<string, object> writer;

//            public PlayerPrefAttributeHandler(object target, PlayerPrefAttribute attribute, IGuiEventSource guiEventSource)
//            {
//                this.target = new WeakReference<object>(target, false);
//                this.attribute = attribute;
//                this.guiEventSource = guiEventSource;
//                property = target.GetType().GetProperty(attribute.fieldName, SupportedPropertyModifiers);
//                this.reader = PlayerPrefsAccessorFactory.ResolveAccessor(property.PropertyType);
//                this.writer = PlayerPrefsAccessorFactory.ResolveMutator(property.PropertyType);
//                this.guiEventSource.OnAfterEnabled += Initialize;
//                this.guiEventSource.OnBeforeUpdated += Flush;
//            }

//            private void Initialize()
//            {
//                target.WithStrongReference((strongTarget) =>
//                {
//                    if (PlayerPrefs.HasKey(attribute.Name))
//                    {
//                        cachedValue = reader(attribute.Name);
//                        property.SetValue(strongTarget, cachedValue);
//                    }
//                    else
//                    {
//                        var initialValue = property.GetValue(strongTarget);
//                        cachedValue = initialValue;
//                        PlayerPrefExtension.PlayerPrefsDirty = true;
//                    }
//                    guiEventSource.OnAfterEnabled -= Initialize;
//                });
//            }

//            private void Flush()
//            {
//                target.WithStrongReference((strongTarget) =>
//                {
//                    var newValue = property.GetValue(strongTarget);
//                    if (cachedValue == newValue) return;
//                    cachedValue = newValue;
//                    writer(attribute.Name, newValue);
//                    PlayerPrefExtension.PlayerPrefsDirty = true;
//                });
//            }
//        }
//    }

//    internal static class PlayerPrefsAccessorFactory
//    {
//        public static Func<string, object> ResolveAccessor(Type targetType)
//        {
//            if (targetType == typeof(int))
//            {
//                return (name) => (object)PlayerPrefs.GetInt(name);
//            }
//            else if (targetType == typeof(float))
//            {
//                return (name) => (object)PlayerPrefs.GetFloat(name);
//            }
//            else if (targetType == typeof(string))
//            {
//                return PlayerPrefs.GetString;
//            }
//            else
//            {
//                return (name) => JsonSerializer.Deserialize(PlayerPrefs.GetString(name), targetType);
//            }
//        }

//        public static Action<string, object> ResolveMutator(Type targetType)
//        {
//            if (targetType == typeof(int))
//            {
//                return (name, value) => PlayerPrefs.SetInt(name, (int)value);
//            }
//            else if (targetType == typeof(float))
//            {
//                return (name, value) => PlayerPrefs.SetFloat(name, (float)value);
//            }
//            else if (targetType == typeof(string))
//            {
//                return (name, value) => PlayerPrefs.SetString(name, value.ToString());
//            }
//            else
//            {
//                return (name, value) => PlayerPrefs.SetString(name, JsonSerializer.Serialize(value));
//            }
//        }
//    }
//}
