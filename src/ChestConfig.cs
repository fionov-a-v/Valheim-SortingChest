using System;
using BepInEx.Configuration;
using SortingChest.Logic;
using UnityEngine;

namespace SortingChest
{
    /// <summary>
    /// Настройки мода. Игровые синхронизируются с сервера (Jotunn, IsAdminOnly), поэтому у всех игроков
    /// на сервере распределяющий сундук работает одинаково; клиентские (эффекты, место кнопки) — свои у каждого.
    /// </summary>
    internal static class ChestConfig
    {
        public static ConfigEntry<float> Radius;
        public static ConfigEntry<bool> IncludePrivate;
        public static ConfigEntry<bool> IncludeVehicles;
        public static ConfigEntry<MatchMode> Mode;
        public static ConfigEntry<string> NeverSortInto;

        public static ConfigEntry<bool> LoadMaterials;
        public static ConfigEntry<string> LoadExtra;
        public static ConfigEntry<string> LoadExcluded;
        public static ConfigEntry<bool> LoadSkipHotbar;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<string> ExtraCost;
        public static ConfigEntry<string> CostOverride;
        public static ConfigEntry<string> Station;

        public static ConfigEntry<bool> ShowEffects;
        public static ConfigEntry<Vector2> LoadButtonOffset;

        /// <summary>Поменялись рецепт, станок или «можно строить» — надо переприменить к префабу.</summary>
        public static event Action RecipeChanged;

        public static void Bind(ConfigFile cfg)
        {
            const string sorting = "1 - Sorting";
            Radius = Synced(cfg, sorting, "Radius", 20f,
                Text("Radius around the sorting chest, m, in which it looks for chests to carry items into.",
                    "Радиус вокруг распределяющего сундука, м, в котором он ищет сундуки, куда разнести вещи."),
                new AcceptableValueRange<float>(1f, 100f));
            IncludePrivate = Synced(cfg, sorting, "IncludePrivateChests", false,
                Text("Also carry items into personal chests (only your own: other players' personal chests cannot be opened anyway).",
                    "Раскладывать и в личные сундуки (только свои: чужие личные сундуки и так не открыть)."), null);
            IncludeVehicles = Synced(cfg, sorting, "IncludeCartsAndShips", false,
                Text("Also carry items into the storage of carts and ships standing nearby.",
                    "Раскладывать и в грузовые отсеки тележек и кораблей, стоящих рядом."), null);
            Mode = Synced(cfg, sorting, "MatchMode", MatchMode.FirstItem,
                Text("Which chests take an item. FirstItem — a chest whose first item (top-left occupied slot) is the same; " +
                     "AnyItem — a chest where the same item lies anywhere.",
                    "Какой сундук принимает предмет. FirstItem — тот, где первым (верхняя левая занятая ячейка) лежит такой же; " +
                    "AnyItem — тот, где такой же предмет лежит где угодно."), null);
            NeverSortInto = Synced(cfg, sorting, "NeverSortInto", "",
                Text("Never put items into these pieces (prefab names separated by commas). Machines with storage — kilns, smelters, " +
                     "fermenters, the obliterator, also from other mods — are skipped automatically; list here what is not recognized.",
                    "Никогда не класть вещи в эти постройки (имена префабов через запятую). Машины с хранилищем — печи, плавильни, " +
                    "бродильни, уничтожитель, в том числе из других модов, — пропускаются сами; сюда — то, что не распознаётся."), null);

            const string load = "2 - Load button";
            LoadMaterials = Synced(cfg, load, "LoadMaterials", true,
                Text("The \"Load resources\" button moves all materials from the player's inventory: wood, stone, ore, metal bars, hides, " +
                     "resin, coal and so on (everything of the Material type).",
                    "Кнопка «Загрузить ресурсы» переносит из инвентаря игрока все материалы: дерево, камень, руду, слитки, шкуры, " +
                    "смолу, уголь и т.п. (всё с типом «материал»)."), null);
            LoadExtra = Synced(cfg, load, "AlsoLoad", "",
                Text("Also load these items, even if they are not materials (prefab names separated by commas, e.g. TrophySkeleton, Fish1).",
                    "Загружать и эти предметы, даже если это не материалы (имена префабов через запятую, например TrophySkeleton, Fish1)."), null);
            LoadExcluded = Synced(cfg, load, "NeverLoad", "",
                Text("Never load these items (prefab names separated by commas, e.g. SurtlingCore, DragonEgg).",
                    "Никогда не загружать эти предметы (имена префабов через запятую, например SurtlingCore, DragonEgg)."), null);
            LoadSkipHotbar = Synced(cfg, load, "SkipHotbar", false,
                Text("Do not take items from the hotbar (the first row of the inventory).",
                    "Не брать предметы из панели быстрого доступа (первый ряд инвентаря)."), null);

            const string recipe = "3 - Recipe";
            Enabled = Synced(cfg, recipe, "Enabled", true,
                Text("Whether the sorting chest can be built.", "Можно ли строить распределяющий сундук."), null);
            ExtraCost = Synced(cfg, recipe, "ExtraCost", "TrophySkeleton:1,SurtlingCore:2,GreydwarfEye:5",
                Text("What the sorting chest costs on top of the reinforced chest (Item:Amount separated by commas).",
                    "Сколько распределяющий сундук стоит сверх армированного (Предмет:Кол-во через запятую)."), null);
            CostOverride = Synced(cfg, recipe, "FullCostOverride", "",
                Text("If not empty — the full cost instead of \"reinforced chest + ExtraCost\" (Item:Amount separated by commas).",
                    "Если не пусто — полная стоимость вместо «армированный сундук + ExtraCost» (Предмет:Кол-во через запятую)."), null);
            Station = Synced(cfg, recipe, "CraftingStation", "",
                Text("Crafting station the sorting chest is built next to (piece_workbench, forge, ...); empty — the same as for the reinforced chest.",
                    "Станок, рядом с которым строится распределяющий сундук (piece_workbench, forge, ...); пусто — тот же, что у армированного."), null);
            ExtraCost.SettingChanged += (_, _) => RecipeChanged?.Invoke();
            CostOverride.SettingChanged += (_, _) => RecipeChanged?.Invoke();
            Station.SettingChanged += (_, _) => RecipeChanged?.Invoke();
            Enabled.SettingChanged += (_, _) => RecipeChanged?.Invoke();

            const string client = "4 - Client";
            ShowEffects = cfg.Bind(client, "ShowEffects", true,
                Text("Show the magic: the rune circle on the floor, the light, sparks and the wisps flying to the chests (this client only).",
                    "Показывать магию: круг рун на полу, свет, искры и огоньки, летящие к сундукам (только для этого клиента)."));
            LoadButtonOffset = cfg.Bind(client, "LoadButtonOffset", Vector2.zero,
                Text("Shift of the \"Load resources\" button from its place to the right of the chest window, in UI pixels (x right, y up).",
                    "Сдвиг кнопки «Загрузить ресурсы» от её места справа от окна сундука, в пикселях интерфейса (x вправо, y вверх)."));
        }

        public static SortSettings SortSettings() => new SortSettings
        {
            Radius = Radius.Value,
            IncludePrivate = IncludePrivate.Value,
            IncludeVehicles = IncludeVehicles.Value,
            Mode = Mode.Value,
            Excluded = SortRules.ParseNames(NeverSortInto.Value),
        };

        public static LoadSettings LoadSettings() => new LoadSettings
        {
            Materials = LoadMaterials.Value,
            SkipHotbar = LoadSkipHotbar.Value,
            Extra = SortRules.ParseNames(LoadExtra.Value),
            Excluded = SortRules.ParseNames(LoadExcluded.Value),
        };

        /// <summary>Описание настройки на двух языках: в .cfg — строка по-английски и под ней по-русски.</summary>
        private static string Text(string en, string ru) => en + "\n" + ru;

        private static ConfigEntry<T> Synced<T>(ConfigFile cfg, string section, string key, T value, string description,
            AcceptableValueBase range)
        {
            var attributes = new ConfigurationManagerAttributes { IsAdminOnly = true };
            return cfg.Bind(section, key, value, new ConfigDescription(description, range, attributes));
        }
    }
}
