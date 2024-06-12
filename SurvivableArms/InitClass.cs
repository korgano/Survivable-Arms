using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using System.Reflection;
using HBS.Logging;

namespace SurvivableArms
{
    public class InitClass
    {
        public static void Init()
        {
            var harmony = HarmonyInstance.Create("Battletech.realitymachina.SurvivableArms");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            var logger = Logger.GetLogger("SurvivableArms");
            logger.LogAtLevel(LogLevel.Debug, "SurvivableArms DLL Initialized");
        }
    }
}
