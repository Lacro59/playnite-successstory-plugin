using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.WutheringWaves
{
    public class WuWaGroup
    {
        [SerializationPropertyName("Id")]
        public int Id { get; set; }

        [SerializationPropertyName("Category")]
        public int Category { get; set; }

        [SerializationPropertyName("Sort")]
        public int Sort { get; set; }

        [SerializationPropertyName("Name")]
        public string Name { get; set; }

        [SerializationPropertyName("SmallIcon")]
        public string SmallIcon { get; set; }

        [SerializationPropertyName("Icon")]
        public string Icon { get; set; }

        [SerializationPropertyName("BackgroundIcon")]
        public string BackgroundIcon { get; set; }

        [SerializationPropertyName("DropId")]
        public int DropId { get; set; }

        [SerializationPropertyName("Enable")]
        public bool Enable { get; set; }
    }
}
