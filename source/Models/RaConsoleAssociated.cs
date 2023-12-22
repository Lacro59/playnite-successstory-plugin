using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class RaConsoleAssociated
    {
        public int RaConsoleId { get; set; }
        public string RaConsoleName { get; set; }
        public List<Platform> Platforms { get; set; }
        [DontSerialize]
        public SelectableDbItemList SelectablePlatforms { get; set; }


        public void GetSelectable()
        {
            SelectablePlatforms = new SelectableDbItemList(API.Instance.Database.Platforms, Platforms?.Select(x => x.Id)?.ToList());
        }

        public void SetSelectable()
        {
            Platforms = API.Instance.Database.Platforms.Where(x => SelectablePlatforms.GetSelectedIds().Any(y => y == x.Id)).ToList();
        }
    }
}
