using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace RecipeSearch
{
    // Client-only: no network code, no RPCs, no Jotunn version check, so it works on vanilla servers.
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jordan.valheim.recipesearch";
        public const string Name = "Recipe Search";
        public const string Version = "0.2.1";

        internal static Plugin Instance;

        internal static ConfigEntry<bool> ClearSearchOnClose;
        internal static ConfigEntry<bool> MatchPrefabNames;
        internal static ConfigEntry<bool> CraftableFirst;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            ClearSearchOnClose = Config.Bind("Search", "ClearOnClose", true,
                "Clear the search box when the inventory closes.");

            MatchPrefabNames = Config.Bind("Search", "MatchPrefabNames", true,
                "Also match internal prefab names (e.g. 'SwordBronze'), not just displayed names.");

            CraftableFirst = Config.Bind("Search", "CraftableFirst", false,
                "When searching, list craftable recipes before uncraftable ones (otherwise best match first).");

            MatchPrefabNames.SettingChanged += (_, __) => RecipeQuery.ClearCache();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            SearchUi.Destroy();
        }

        internal static void Warn(string msg) => Instance?.Logger.LogWarning(msg);
    }
}
