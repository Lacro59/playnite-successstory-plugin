using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CommonPluginsShared.Controls
{
    public class PluginUserControlExtend : PluginUserControlExtendBase
    {
        internal virtual IPluginDatabase _PluginDatabase { get; set; }


        #region OnPropertyChange
        // When game selection is changed
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            if (_PluginDatabase == null || !_PluginDatabase.IsLoaded)
            {
                return;
            }

            if (newContext == null || (oldContext != null && oldContext.Id == newContext.Id))
            {
                return;
            }

            SetDefaultDataContext();

            MustDisplay = _ControlDataContext.IsActivated;

            // When control is not used
            if (!_ControlDataContext.IsActivated)
            {
                return;
            }

            try
            {
                PluginDataBaseGameBase PluginGameData = _PluginDatabase.Get(newContext, true);
                if (PluginGameData.HasData)
                {
                    SetData(newContext, PluginGameData);
                }
                else if (AlwaysShow)
                {
                    SetData(newContext, PluginGameData);
                }
                // When there is no plugin data
                else
                {
                    MustDisplay = false;
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
        #endregion


        public virtual Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return new Task<bool>(() => false);
        }
    }
}
