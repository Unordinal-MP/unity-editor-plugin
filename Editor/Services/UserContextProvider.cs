using System;
using System.Text.Json;
using Unity;
using UnityEditor;
using Unordinal.Editor.External;
using Unordinal.Editor.Utils;

namespace Unordinal.Editor.Services
{
    public class UserContextProvider : ITokenStorage, IUserInfoHolder
    {
        public UserInfo UserInfo { get; set; }

        public IUserInfoHolder TestTest { get; set; }

        public UserContextProvider()    
        {
            // Automatically called by UnityContainer.
            Load();
        }

        public override void Clear() {
            UserInfo = default;
            Token = null;
            RefreshToken = null;
            TokenExpirationDate = default;
            Save();
        }

        #region EditorPrefs

        public new void Save()
        {
            base.Save();

            var json = JsonSerializer.Serialize(UserInfo);
            EditorPrefs.SetString(UnordinalKeys.userInfoKey, json);
        }


        public new void Load()
        {
            base.Load();

            if(EditorPrefs.HasKey(UnordinalKeys.userInfoKey))
            {
                var json = EditorPrefs.GetString(UnordinalKeys.userInfoKey);
                try
                {
                    UserInfo = JsonSerializer.Deserialize<UserInfo>(json);
                }
                catch(Exception)
                {
                    // Failed to deserialize the object. (might have been an empty string)
                    // Defaults are automatically used instead.
                }
            }
        }

        #endregion
    }
}
