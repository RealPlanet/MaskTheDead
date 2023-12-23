using HarmonyLib;
using MaskTheDead.Components;

namespace MaskTheDead.Patchers
{
    [HarmonyPatch]
    public class GrabbableObjectMaskPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), "Start")]
        private static bool StartPatch(ref GrabbableObject __instance)
        {
            if (__instance == null)
            {
                Plugin.TheLogger.LogFatal("This instance is null, cannot patch item");
                return true;
            }

            HauntedMaskItem mask = __instance as HauntedMaskItem;
            if (mask == null)
            {
                // Not a mask
                return true;
            }

            if (!GameNetworkManager.Instance.isHostingGame)
            {
                Plugin.TheLogger.LogInfo("Avoding patch for non hosts");
                return true;
            }
            
            Plugin.TheLogger.LogInfo("Adding mask watcher to haunted mask!");
            // Let the host handle the possession, then let the RPC handle the clients
            mask.gameObject.AddComponent<MaskTheDeadComponent>();      
            return true;
        }
    }    
}
