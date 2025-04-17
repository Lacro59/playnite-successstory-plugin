using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.WutheringWaves
{
    public class WuWaTraduction
    {
        [SerializationPropertyName("Id")]
        public string Id { get; set; }

        [SerializationPropertyName("Content")]
        public string Content { get; set; }
    }
}
