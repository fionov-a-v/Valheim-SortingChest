using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace SortingChest
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    public class SortingChestPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "sortingchest";
        public const string PluginName = "Sorting Chest";
        public const string PluginVersion = "1.0.0";

        private Harmony m_harmony;
        private ConfigFileWatcher m_configWatcher;

        private void Awake()
        {
            ChestConfig.Bind(Config);
            // Правка .cfg во время игры применяется сразу (а на сервере — рассылается игрокам).
            m_configWatcher = new ConfigFileWatcher(Config);
            ChestLocalization.Register();

            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            PrefabManager.OnPrefabsRegistered += ChestPiece.ApplyRecipe;
            SynchronizationManager.OnConfigurationSynchronized += (_, _) => ChestPiece.ApplyRecipe();
            ChestConfig.RecipeChanged += ChestPiece.ApplyRecipe;

            m_harmony = new Harmony(PluginGuid);
            m_harmony.PatchAll(typeof(Patches));

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} загружен");
        }

        private void OnVanillaPrefabsAvailable()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
            ChestPiece.Register();
        }

        private void OnDestroy()
        {
            m_harmony?.UnpatchSelf();
        }
    }
}
