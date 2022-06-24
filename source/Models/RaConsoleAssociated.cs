using Playnite.SDK;
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
    }

    public class Platform
    {
        public Guid Id { get; set; }
        public string Name => API.Instance.Database.Platforms?.Where(x => x.Id == Id)?.FirstOrDefault()?.Name ?? string.Empty;
        public bool IsSelected { get; set; }
    }
}
