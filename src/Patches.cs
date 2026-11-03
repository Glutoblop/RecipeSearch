using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace RecipeSearch
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatches
    {
        [HarmonyPostfix, HarmonyPatch(nameof(InventoryGui.Awake))]
        private static void Awake(InventoryGui __instance)
        {
            try { SearchUi.Build(__instance); }
            catch (System.Exception e) { Plugin.Warn("Failed to build search UI: " + e); }
        }

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.OnDestroy))]
        private static void OnDestroy() => SearchUi.Destroy();

        [HarmonyPostfix, HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void Hide() => SearchUi.OnInventoryHidden();

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.Update))]
        private static void Update() => SearchUi.Tick();

        // Filter the incoming recipe list by search before vanilla builds the UI elements.
        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.UpdateRecipeList))]
        private static void UpdateRecipeListPrefix(ref List<Recipe> recipes)
        {
            recipes = RecipeQuery.Filter(recipes);
        }

        // Vanilla sorts by craftable/name; when searching, re-sort by match quality instead.
        [HarmonyPostfix, HarmonyPatch(nameof(InventoryGui.UpdateRecipeList))]
        private static void UpdateRecipeListPostfix(InventoryGui __instance)
        {
            if (!RecipeQuery.Searching) return;
            var list = __instance.m_availableRecipes;
            var scores = RecipeQuery.Scores;
            int Score(InventoryGui.RecipeDataPair p) => scores.TryGetValue(p.Recipe, out int s) ? s : 0;

            IOrderedEnumerable<InventoryGui.RecipeDataPair> sorted = Plugin.CraftableFirst.Value
                ? list.OrderByDescending(p => p.CanCraft).ThenByDescending(Score)
                : list.OrderByDescending(Score).ThenByDescending(p => p.CanCraft);
            var result = sorted.ToList(); // LINQ OrderBy is stable, so vanilla order breaks ties

            list.Clear();
            list.AddRange(result);
            for (int i = 0; i < list.Count; i++)
                ((RectTransform)list[i].InterfaceElement.transform).anchoredPosition =
                    new Vector2(0f, i * -__instance.m_recipeListSpace);
        }
    }

    // While typing in the search box, swallow game hotkeys (Tab closes inventory, number keys use hotbar, etc).
    [HarmonyPatch(typeof(ZInput))]
    internal static class ZInputPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(ZInput.GetButtonDown), typeof(string))]
        private static bool GetButtonDown(ref bool __result) => Pass(ref __result);

        [HarmonyPrefix, HarmonyPatch(nameof(ZInput.GetButtonUp), typeof(string))]
        private static bool GetButtonUp(ref bool __result) => Pass(ref __result);

        [HarmonyPrefix, HarmonyPatch(nameof(ZInput.GetButton), typeof(string))]
        private static bool GetButton(ref bool __result) => Pass(ref __result);

        [HarmonyPrefix, HarmonyPatch(nameof(ZInput.GetKeyDown), typeof(KeyCode), typeof(bool))]
        private static bool GetKeyDown(ref bool __result) => Pass(ref __result);

        [HarmonyPrefix, HarmonyPatch(nameof(ZInput.GetKeyUp), typeof(KeyCode), typeof(bool))]
        private static bool GetKeyUp(ref bool __result) => Pass(ref __result);

        [HarmonyPrefix, HarmonyPatch(nameof(ZInput.GetKey), typeof(KeyCode), typeof(bool))]
        private static bool GetKey(ref bool __result) => Pass(ref __result);

        private static bool Pass(ref bool result)
        {
            if (!SearchUi.BlockGameInput) return true;
            result = false;
            return false;
        }
    }
}
