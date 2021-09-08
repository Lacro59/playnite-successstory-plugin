using CommonPluginsShared.Collections;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Interfaces
{
    public interface IPluginDatabase
    {
        bool IsLoaded { get; set; }

        Task<bool> InitializeDatabase();

        PluginDataBaseGameBase Get(Game game, bool OnlyCache = false, bool Force = false);
    }
}
