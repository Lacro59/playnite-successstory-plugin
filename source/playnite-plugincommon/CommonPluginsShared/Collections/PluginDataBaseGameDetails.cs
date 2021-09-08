using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Collections
{
    public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGameBase
    {
        public abstract List<T> Items { get; set; }

        public abstract Y ItemsDetails { get; set; }


        [DontSerialize]
        public override bool HasData
        {
            get
            {
                return Items.Count > 0;
            }
        }
    }
}
