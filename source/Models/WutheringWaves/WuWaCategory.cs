using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.WutheringWaves
{
    public class WuWaCategory
    {
        [SerializationPropertyName("Id")]
        public int Id { get; set; }

        [SerializationPropertyName("Name")]
        public string Name { get; set; }

        [SerializationPropertyName("FunctionType")]
        public int FunctionType { get; set; }

        [SerializationPropertyName("SpritePath")]
        public string SpritePath { get; set; }

        [SerializationPropertyName("TexturePath")]
        public string TexturePath { get; set; }
    }
}
