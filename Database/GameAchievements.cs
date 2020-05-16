using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Database
{
    /// <summary>
    /// Specifies <see cref="GameAchievements"/> fields.
    /// </summary>
    public enum GameAchievementsField
    {
        name,
        haveAchivements
    }

    /// <summary>
    /// Represents GameAchievements file.
    /// </summary>
    class GameAchievements
    {
        public string Name { get; set; }
        public bool HaveAchivements { get; set; }
    }
}
