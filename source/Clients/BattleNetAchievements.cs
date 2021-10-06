using CommonPlayniteShared.PluginLibrary.BattleNetLibrary.Models;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;

namespace SuccessStory.Clients
{
    abstract class BattleNetAchievements : GenericAchievements
    {
        protected static IWebView _WebViewOffscreen;
        internal static IWebView WebViewOffscreen
        {
            get
            {
                if (_WebViewOffscreen == null)
                {
                    _WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
                }
                return _WebViewOffscreen;
            }

            set
            {
                _WebViewOffscreen = value;
            }
        }

        protected const string UrlOauth2 = @"https://account.blizzard.com:443/oauth2/authorization/account-settings";
        protected const string UrlApiStatus = @"https://account.blizzard.com/api/";


        public BattleNetAchievements(string ClientName, string LocalLang = "", string LocalLangShort = "") : base(ClientName, LocalLang, LocalLangShort)
        {
            
        }


        #region Battle.net
        // TODO Rewrite authentification
        protected BattleNetApiStatus GetApiStatus()
        {
            try
            {
                // This refreshes authentication cookie
                WebViewOffscreen.NavigateAndWait(UrlOauth2);
                WebViewOffscreen.NavigateAndWait(UrlApiStatus);
                var textStatus = WebViewOffscreen.GetPageText();
                return Serialization.FromJson<BattleNetApiStatus>(textStatus);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }
        #endregion
    }
}
