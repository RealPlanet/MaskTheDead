using HarmonyLib;
using System.Reflection;
using BepInEx.Logging;
using BepInEx;

namespace MaskTheDead
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource TheLogger;
        public static Configuration TheConfiguration;

        public Plugin()
        {
            TheConfiguration = new(Config);
        }

        private void Awake()
        {
            // Plugin startup logic
            TheLogger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
