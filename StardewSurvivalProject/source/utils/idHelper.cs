using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewSurvivalProject.source.utils
{
    enum toolType
    {
        AXE = 0,
        HOE = 1,
        FISHING_ROD = 2,
        PICKAXE = 3,
        WATERING_CAN = 4,
        MELEE_WEAPON = 5,
        SLINGSHOT = 6
    }

    enum weatherType
    {
        SUNNY = 0,
        RAIN = 1,
        WINDY = 2,
        STORM = 3,
        FESTIVAL = 4,
        SNOW = 5,
        WEDDING = 6,
    }

    enum weatherIconType
    {
        SNOW = 7,
        RAIN = 4,
        WINDY_SPRING = 3,
        WINDY_FALL = 6,
        WEDDING = 0,
        SUNNY = 2,
        STORM = 5,
        FESTIVAL = 1,
    }

    enum bodyStatus
    {
        //constant health loss
        BURN = 0,
        //drastically increase stamina usage
        HEATSTROKE = 1,
        //drastically slow down player
        HYPOTHERMIA = 2,
        //constant health loss
        FROSTBITE = 3,
        //drain stamina constantly
        FEVER = 4,
        //drain stamina in place of hunger
        STARVING = 5,
        //drain health in place of thirst
        DEHYDRATED = 6,

    }
}
