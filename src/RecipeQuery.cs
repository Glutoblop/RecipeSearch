using System;
using System.Collections.Generic;
using T = ItemDrop.ItemData.ItemType;

namespace RecipeSearch
{
    /// <summary>
    /// Smart recipe search. Every word in the query must match something about the recipe:
    ///   plain word  -> the item's name, one of its ingredients, or its category ("weapon", "food", ...)
    ///   uses:word   -> ingredients only           e.g. "uses:copper"
    ///   -word       -> exclude recipes matching it e.g. "sword -bronze"
    /// Name matches rank above category matches, which rank above ingredient matches.
    /// </summary>
    public static class RecipeQuery
    {
        public static string SearchText = string.Empty;
        public static bool Searching => !string.IsNullOrWhiteSpace(SearchText);

        // Score per recipe for the current search, used to re-sort after vanilla sorting.
        public static readonly Dictionary<Recipe, int> Scores = new Dictionary<Recipe, int>();

        private enum Kind { Any, Ingredient, Exclude }
        private struct Term { public Kind Kind; public string Text; }

        private class RecipeText
        {
            public string Name;
            public string Prefab;
            public List<string> Ingredients = new List<string>();
            public T Type;
        }

        private static readonly Dictionary<Recipe, RecipeText> _cache = new Dictionary<Recipe, RecipeText>();

        private static readonly Dictionary<string, T[]> Categories = new Dictionary<string, T[]>
        {
            ["weapon"]     = new[] { T.OneHandedWeapon, T.TwoHandedWeapon, T.TwoHandedWeaponLeft, T.Bow, T.Attach_Atgeir, T.Torch },
            ["melee"]      = new[] { T.OneHandedWeapon, T.TwoHandedWeapon, T.TwoHandedWeaponLeft, T.Attach_Atgeir },
            ["ranged"]     = new[] { T.Bow },
            ["bow"]        = new[] { T.Bow },
            ["armor"]      = new[] { T.Helmet, T.Chest, T.Legs, T.Hands, T.Shoulder, T.Shield, T.Utility, T.Trinket },
            ["armour"]     = new[] { T.Helmet, T.Chest, T.Legs, T.Hands, T.Shoulder, T.Shield, T.Utility, T.Trinket },
            ["helmet"]     = new[] { T.Helmet },
            ["chest"]      = new[] { T.Chest },
            ["legs"]       = new[] { T.Legs },
            ["cape"]       = new[] { T.Shoulder },
            ["shield"]     = new[] { T.Shield },
            ["utility"]    = new[] { T.Utility },
            ["trinket"]    = new[] { T.Trinket },
            ["tool"]       = new[] { T.Tool, T.Torch },
            ["ammo"]       = new[] { T.Ammo, T.AmmoNonEquipable },
            ["food"]       = new[] { T.Consumable },
            ["consumable"] = new[] { T.Consumable },
            ["potion"]     = new[] { T.Consumable },
            ["material"]   = new[] { T.Material },
        };

        public static void ClearCache() => _cache.Clear();

        public static List<Recipe> Filter(List<Recipe> recipes)
        {
            Scores.Clear();
            if (!Searching) return recipes;

            var terms = Parse(SearchText);
            if (terms.Count == 0) return recipes;

            var result = new List<Recipe>(recipes.Count);
            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.m_item == null) continue;
                int score = ScoreRecipe(GetText(recipe), terms);
                if (score < 0) continue;
                Scores[recipe] = score;
                result.Add(recipe);
            }
            return result;
        }

        private static List<Term> Parse(string query)
        {
            var terms = new List<Term>();
            foreach (string raw in query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Kind kind = Kind.Any;
                string word = raw;
                if (word.StartsWith("-") && word.Length > 1) { kind = Kind.Exclude; word = word.Substring(1); }
                else if (word.StartsWith("uses:", StringComparison.OrdinalIgnoreCase)) { kind = Kind.Ingredient; word = word.Substring(5); }
                else if (word.StartsWith("u:", StringComparison.OrdinalIgnoreCase)) { kind = Kind.Ingredient; word = word.Substring(2); }

                // Normalising can split a word ("fine-wood" -> "fine wood"); each piece becomes its own term.
                foreach (string piece in FuzzyMatcher.Normalize(word).Split(' '))
                    if (piece.Length > 0) terms.Add(new Term { Kind = kind, Text = piece });
            }
            return terms;
        }

        private static int ScoreRecipe(RecipeText r, List<Term> terms)
        {
            int total = 0;
            foreach (var term in terms)
            {
                switch (term.Kind)
                {
                    case Kind.Exclude:
                        // Excludes use plain substring so they don't accidentally fuzzy-remove things.
                        if (r.Name.Contains(term.Text) || r.Ingredients.Exists(i => i.Contains(term.Text))) return -1;
                        break;

                    case Kind.Ingredient:
                    {
                        int s = IngredientScore(r, term.Text);
                        if (s < 0) return -1;
                        total += s;
                        break;
                    }

                    default:
                    {
                        int s = Math.Max(NameScore(r, term.Text),
                                Math.Max(CategoryScore(r, term.Text), IngredientScore(r, term.Text)));
                        if (s < 0) return -1;
                        total += s;
                        break;
                    }
                }
            }
            return total;
        }

        private static int NameScore(RecipeText r, string token)
        {
            int s = FuzzyMatcher.TokenScore(token, r.Name);
            if (r.Prefab != null)
            {
                int p = FuzzyMatcher.TokenScore(token, r.Prefab);
                if (p >= 0) s = Math.Max(s, p - 50); // slight preference for the displayed name
            }
            return s;
        }

        private static int IngredientScore(RecipeText r, string token)
        {
            int best = -1;
            foreach (string ing in r.Ingredients)
                best = Math.Max(best, FuzzyMatcher.TokenScore(token, ing));
            return best < 0 ? -1 : best / 2; // ingredient hits rank below name hits
        }

        private static int CategoryScore(RecipeText r, string token)
        {
            if (token.Length < 3) return -1;
            foreach (var kv in Categories)
            {
                // "weap", "weapon", "weapons" all hit "weapon".
                bool hit = kv.Key.StartsWith(token, StringComparison.Ordinal) ||
                           (token.StartsWith(kv.Key, StringComparison.Ordinal) && token.Length <= kv.Key.Length + 1);
                if (hit && Array.IndexOf(kv.Value, r.Type) >= 0) return 450;
            }
            return -1;
        }

        private static RecipeText GetText(Recipe recipe)
        {
            if (_cache.TryGetValue(recipe, out var cached)) return cached;

            var shared = recipe.m_item.m_itemData.m_shared;
            var text = new RecipeText
            {
                Name = FuzzyMatcher.Normalize(Localize(shared.m_name)),
                Prefab = Plugin.MatchPrefabNames.Value ? FuzzyMatcher.Normalize(recipe.m_item.name) : null,
                Type = shared.m_itemType,
            };
            if (recipe.m_resources != null)
            {
                foreach (var req in recipe.m_resources)
                {
                    if (req?.m_resItem == null) continue;
                    text.Ingredients.Add(FuzzyMatcher.Normalize(Localize(req.m_resItem.m_itemData.m_shared.m_name)));
                    if (Plugin.MatchPrefabNames.Value)
                        text.Ingredients.Add(FuzzyMatcher.Normalize(req.m_resItem.name));
                }
            }
            _cache[recipe] = text;
            return text;
        }

        private static string Localize(string s) =>
            Localization.instance != null ? Localization.instance.Localize(s) : s;
    }
}
