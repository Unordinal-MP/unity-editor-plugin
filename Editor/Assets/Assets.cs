using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unordinal.Editor
{
    public static class Assets
    {
        public static Images Images { get; } = new Images();
        public static StyleSheets StyleSheets { get; } = new StyleSheets();
    }

    public class Images {
        internal Images() { }

        public Image this[string index, string type = "png"] {
            get {
                Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath($"Packages/com.unordinal.hosting/Editor/Assets/{index}.{type}", typeof(Texture2D));
                return new Image() { image = texture };
            }
        }
    }

    public class StyleSheets {
        internal StyleSheets() { }

        public StyleSheet this[string index] {
            get {
                return AssetDatabase.LoadAssetAtPath<StyleSheet>($"Packages/com.unordinal.hosting/Editor/Assets/{index}.uss");
            }
        }
    }
}
