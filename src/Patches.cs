using HarmonyLib;

namespace SortingChest
{
    [HarmonyPatch]
    internal static class Patches
    {
        /// <summary>Каждый сундук: в реестр и научить отвечать на просьбу распределяющего сундука отдать владение.</summary>
        [HarmonyPatch(typeof(Container), "Awake")]
        [HarmonyPostfix]
        private static void ContainerAwake(Container __instance)
        {
            ContainerAccess.OnAwake(__instance);
        }

        [HarmonyPatch(typeof(Container), nameof(Container.SetInUse))]
        [HarmonyPrefix]
        private static void SetInUsePrefix(Container __instance, out bool __state)
        {
            __state = __instance.IsInUse();
        }

        /// <summary>Распределяющий сундук закрыли (был открыт — стал закрыт) — руны разносят вещи.</summary>
        [HarmonyPatch(typeof(Container), nameof(Container.SetInUse))]
        [HarmonyPostfix]
        private static void SetInUsePostfix(Container __instance, bool inUse, bool __state)
        {
            if (!__state || inUse || __instance.IsInUse() || !__instance.IsOwner())
            {
                return;
            }
            DistributingChest chest = __instance.GetComponent<DistributingChest>();
            if (chest != null)
            {
                Sorter.Begin(__instance, chest);
            }
        }

        /// <summary>Пока руны работают, распределяющий сундук не открыть (иначе вещи менялись бы прямо во время раскладки).</summary>
        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        [HarmonyPrefix]
        private static bool InteractPrefix(Container __instance, Humanoid character, bool hold, ref bool __result)
        {
            if (hold)
            {
                return true;
            }
            DistributingChest chest = __instance.GetComponent<DistributingChest>();
            if (chest == null || !chest.IsSorting)
            {
                return true;
            }
            character.Message(MessageHud.MessageType.Center, "$sortingchest_msg_working");
            __result = true;
            return false;
        }

        /// <summary>При наведении: строка про радиус и кольцо радиуса на земле (как у охранного камня).</summary>
        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        [HarmonyPostfix]
        private static void HoverText(Container __instance, ref string __result)
        {
            DistributingChest chest = __instance.GetComponent<DistributingChest>();
            if (chest == null)
            {
                return;
            }
            chest.ShowMarker();
            __result += "\n<color=#7FE6FF>" + ChestLocalization.Format("$sortingchest_hover", ChestConfig.Radius.Value.ToString("0.#")) + "</color>";
        }

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        [HarmonyPostfix]
        private static void InventoryGuiUpdate(InventoryGui __instance)
        {
            LoadButton.Update(__instance);
        }

        [HarmonyPatch(typeof(Game), "Start")]
        [HarmonyPostfix]
        private static void GameStart()
        {
            MagicFx.RegisterRpc();
        }
    }
}
