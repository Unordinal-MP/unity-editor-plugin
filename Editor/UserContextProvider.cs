using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unordinal.Hosting
{
    public class UserContextProvider : ITokenStorage, IUserInfoHolder
    {
        private const string UserInfoName = "oAuthUserInfo.Name";
        private const string UserInfoSub = "oAuthUserInfo.Sub";
        private const string OAuthToken = "oAuthToken";
        private const string OAuthRefreshToken = "oAuthRefreshToken";
        private const string OAuthTokenExpDate = "oAuthTokenExpDate";

        private static readonly string[] Keys = new string[] {
            UserInfoName, UserInfoSub, OAuthToken, OAuthRefreshToken, OAuthTokenExpDate
        };

        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        public UserInfo userInfo {
            get {
                return new UserInfo {
                name = cache[UserInfoName],
                sub = cache[UserInfoSub]
                };
            }

            set {
                cache[UserInfoName] = value.name;
                cache[UserInfoSub] = value.sub;
            }
        }

        public override string token {
            get { return cache[OAuthToken]; }

            set { cache[OAuthToken] = value; }
        }

        public override string refreshToken
        {
            get { return cache[OAuthRefreshToken]; }

            set { cache[OAuthRefreshToken] = value; }
        }

        public override DateTime tokenExpirationDate
        {
            get
            {
                DateTime result;
                DateTime.TryParse(cache[OAuthTokenExpDate], out result);
                return result;
            }

            set { cache[OAuthTokenExpDate] = value.ToString(); }
        }

        public override void Load() {
            foreach (string key in Keys) {
                cache[key] = PlayerPrefs.GetString(key);
            }
        }

        public override void Clear() {
            cache.Clear();
            foreach (string key in Keys)
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        public override void Save()
        {
            foreach (string key in Keys)
            {
                PlayerPrefs.SetString(key, cache[key]);
            }
            PlayerPrefs.Save();
        }
    }
}
