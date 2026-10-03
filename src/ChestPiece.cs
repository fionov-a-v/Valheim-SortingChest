using System;
using System.Collections.Generic;
using SortingChest.Logic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace SortingChest
{
    /// <summary>
    /// Префаб распределяющего сундука — клон армированного сундука (та же модель и те же 6×4 ячейки) с магией поверх.
    /// Стоимость — стоимость армированного сундука (читается из игры) плюс ExtraCost из настроек.
    /// </summary>
    internal static class ChestPiece
    {
        public const string PrefabName = "piece_sortingchest";
        private const string BaseName = "piece_chest";

        private static GameObject s_prefab;
        private static readonly List<KeyValuePair<string, int>> s_baseCost = new List<KeyValuePair<string, int>>();
        private static string s_baseStation = "";

        public static void Register()
        {
            GameObject basePrefab = PrefabManager.Instance.GetPrefab(BaseName);
            Piece basePiece = basePrefab != null ? basePrefab.GetComponent<Piece>() : null;
            if (basePiece == null || basePrefab.GetComponent<Container>() == null)
            {
                Logger.LogError($"Sorting Chest: ванильный сундук '{BaseName}' не найден — распределяющий сундук не добавлен");
                return;
            }
            s_baseCost.Clear();
            foreach (Piece.Requirement r in basePiece.m_resources)
            {
                if (r.m_resItem != null && r.m_amount > 0)
                {
                    s_baseCost.Add(new KeyValuePair<string, int>(r.m_resItem.gameObject.name, r.m_amount));
                }
            }
            s_baseStation = basePiece.m_craftingStation != null ? basePiece.m_craftingStation.name : "";

            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, basePrefab);
            prefab.GetComponent<Container>().m_name = "$sortingchest_piece";
            prefab.AddComponent<DistributingChest>();
            try
            {
                MagicFx.Init();
                MagicFx.Build(prefab);
            }
            catch (Exception e)
            {
                // Без магии сундук всё равно должен строиться и сортировать.
                Logger.LogError($"Sorting Chest: не удалось создать эффекты, сундук будет без них: {e}");
            }

            var config = new PieceConfig
            {
                Name = "$sortingchest_piece",
                Description = "$sortingchest_desc",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = StationName(),
                Enabled = ChestConfig.Enabled.Value,
                Icon = RenderIcon(prefab) ?? basePiece.m_icon,
                Requirements = Cost().ConvertAll(r => new RequirementConfig(r.Key, r.Value, 0, true)).ToArray(),
            };
            PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, config));
            s_prefab = prefab;
        }

        /// <summary>Применить рецепт, станок и «можно строить» из (синхронизированного) конфига к уже созданному префабу.</summary>
        public static void ApplyRecipe()
        {
            Piece piece = s_prefab != null ? s_prefab.GetComponent<Piece>() : null;
            if (piece == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }
            var requirements = new List<Piece.Requirement>();
            foreach (KeyValuePair<string, int> r in Cost())
            {
                GameObject item = ObjectDB.instance.GetItemPrefab(r.Key);
                ItemDrop drop = item != null ? item.GetComponent<ItemDrop>() : null;
                if (drop == null)
                {
                    Logger.LogWarning($"Sorting Chest: предмет '{r.Key}' из рецепта не найден");
                    continue;
                }
                requirements.Add(new Piece.Requirement { m_resItem = drop, m_amount = r.Value, m_recover = true });
            }
            piece.m_resources = requirements.ToArray();
            piece.m_craftingStation = FindStation(StationName());
            piece.m_enabled = ChestConfig.Enabled.Value;
        }

        private static List<KeyValuePair<string, int>> Cost()
        {
            var errors = new List<string>();
            List<KeyValuePair<string, int>> cost = string.IsNullOrWhiteSpace(ChestConfig.CostOverride.Value)
                ? SortRules.MergeCost(s_baseCost, SortRules.ParseCost(ChestConfig.ExtraCost.Value, errors))
                : SortRules.ParseCost(ChestConfig.CostOverride.Value, errors);
            foreach (string e in errors)
            {
                Logger.LogWarning($"Sorting Chest: неверная часть рецепта '{e}' (нужно Предмет:Кол-во)");
            }
            return cost;
        }

        private static string StationName()
        {
            string station = ChestConfig.Station.Value;
            return string.IsNullOrWhiteSpace(station) ? s_baseStation : station.Trim();
        }

        private static CraftingStation FindStation(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            name = CraftingStations.GetInternalName(name.Trim());
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (go == null)
            {
                go = PrefabManager.Instance.GetPrefab(name);
            }
            CraftingStation station = go != null ? go.GetComponent<CraftingStation>() : null;
            if (station == null)
            {
                Logger.LogWarning($"Sorting Chest: станок '{name}' не найден");
            }
            return station;
        }

        private static Sprite RenderIcon(GameObject prefab)
        {
            try
            {
                return RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab)
                {
                    Width = 128,
                    Height = 128,
                    Rotation = RenderManager.IsometricRotation,
                    UseCache = false,
                });
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Sorting Chest: иконка не отрисована, будет иконка армированного сундука: {e.Message}");
                return null;
            }
        }
    }
}
