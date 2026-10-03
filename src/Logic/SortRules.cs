using System;
using System.Collections.Generic;

namespace SortingChest.Logic
{
    /// <summary>По какому признаку сундук «принимает» предмет.</summary>
    public enum MatchMode
    {
        /// <summary>Только по первому предмету сундука (верхняя левая занятая ячейка).</summary>
        FirstItem = 0,

        /// <summary>По любому предмету, который уже лежит в сундуке.</summary>
        AnyItem = 1,
    }

    /// <summary>Предмет в ячейке: ключ (одинаковый у одинаковых предметов) и место в сетке.</summary>
    public struct SlotItem
    {
        public string Key;
        public int X;
        public int Y;

        public SlotItem(string key, int x, int y)
        {
            Key = key;
            X = x;
            Y = y;
        }
    }

    /// <summary>Что известно о сундуке-кандидате (собирается из игры, здесь только правила).</summary>
    public sealed class ChestFacts
    {
        /// <summary>Сам сортирующий сундук.</summary>
        public bool IsSelf;

        /// <summary>Другой сортирующий сундук (в них не раскладываем, иначе вещи ходили бы по кругу).</summary>
        public bool IsSortingChest;

        /// <summary>Поставлен игроком (у сундуков подземелий и могил создателя нет).</summary>
        public bool BuiltByPlayer;

        /// <summary>Личный сундук.</summary>
        public bool IsPrivate;

        /// <summary>Личный сундук этого игрока (чужой личный открыть нельзя вовсе).</summary>
        public bool IsOwnPrivate;

        /// <summary>Сундук тележки или корабля.</summary>
        public bool IsVehicle;

        /// <summary>Под охранным камнем, к которому у игрока нет доступа.</summary>
        public bool WardBlocked;

        /// <summary>
        /// Хранилище машины, а не сундук: печи, плавильни, бродильни, уничтожителя (в том числе из других модов —
        /// например, угольная печь BetterStations берёт дерево из своего «бака» и сжигает его).
        /// </summary>
        public bool IsMachine;

        /// <summary>Имя префаба постройки (для списка исключений в настройках).</summary>
        public string PrefabName;

        /// <summary>Расстояние от сортирующего сундука, м.</summary>
        public float Distance;
    }

    public sealed class SortSettings
    {
        public float Radius = 20f;
        public bool IncludePrivate;
        public bool IncludeVehicles;
        public MatchMode Mode = MatchMode.FirstItem;

        /// <summary>Постройки, в которые не раскладывать никогда (имена префабов).</summary>
        public HashSet<string> Excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Сундук для раскладки: какие ключи принимает и как далеко стоит.</summary>
    public sealed class ChestTarget
    {
        public int Index;
        public float Distance;
        public HashSet<string> Accepts = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class LoadSettings
    {
        /// <summary>Брать все материалы (дерево, камень, руда, слитки, шкуры, смола...).</summary>
        public bool Materials = true;

        /// <summary>Не трогать ряд быстрого доступа (первый ряд инвентаря).</summary>
        public bool SkipHotbar;

        /// <summary>Брать ещё и эти предметы (имена префабов), даже если это не материалы.</summary>
        public HashSet<string> Extra = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Никогда не брать эти предметы (имена префабов).</summary>
        public HashSet<string> Excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Правила сортировки и загрузки — без Unity, чтобы их можно было проверить тестами.</summary>
    public static class SortRules
    {
        /// <summary>Можно ли раскладывать в этот сундук.</summary>
        public static bool IsEligible(ChestFacts c, SortSettings s)
        {
            if (c.IsSelf || c.IsSortingChest || !c.BuiltByPlayer || c.WardBlocked || c.IsMachine)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(c.PrefabName) && s.Excluded.Contains(c.PrefabName))
            {
                return false;
            }
            if (c.Distance > s.Radius)
            {
                return false;
            }
            if (c.IsVehicle && !s.IncludeVehicles)
            {
                return false;
            }
            if (c.IsPrivate && (!s.IncludePrivate || !c.IsOwnPrivate))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Части имён компонентов-машин. Хранилище (Container) бывает не только у сундуков: у ванильного уничтожителя
        /// (Incinerator), у печей и бродилен из модов (BetterSmelter, BetterFermenter...). Если на постройке есть
        /// компонент с таким словом в имени типа — это машина, и вещи туда не кладём.
        /// </summary>
        private static readonly string[] s_machineWords =
        {
            "Smelter", "Fermenter", "Kiln", "Furnace", "Refinery", "Incinerator", "Obliterator", "Oven", "Cooking",
            "Windmill", "Spinning", "Beehive", "SapCollector", "Turret", "ShieldGenerator", "CraftingStation",
            "Distill", "Brewery",
        };

        /// <summary>Есть ли среди компонентов постройки (имена их типов) машина.</summary>
        public static bool LooksLikeMachine(IEnumerable<string> componentTypeNames)
        {
            foreach (string name in componentTypeNames)
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                foreach (string word in s_machineWords)
                {
                    if (name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Первый предмет сундука — тот, что в верхней левой занятой ячейке (сначала строка, потом столбец),
        /// как его видит игрок. Возвращает индекс в списке или -1, если сундук пуст.
        /// </summary>
        public static int FirstInGridOrder(IList<SlotItem> items)
        {
            int best = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (best < 0 || items[i].Y < items[best].Y || (items[i].Y == items[best].Y && items[i].X < items[best].X))
                {
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Какие предметы сундук принимает при данном режиме.</summary>
        public static HashSet<string> AcceptedKeys(IList<SlotItem> items, MatchMode mode)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (mode == MatchMode.AnyItem)
            {
                foreach (SlotItem item in items)
                {
                    keys.Add(item.Key);
                }
                return keys;
            }
            int first = FirstInGridOrder(items);
            if (first >= 0)
            {
                keys.Add(items[first].Key);
            }
            return keys;
        }

        /// <summary>
        /// Куда нести предмет с этим ключом: индексы подходящих сундуков, ближние первыми
        /// (когда ближний заполнится, остаток уходит в следующий). При равном расстоянии — по индексу.
        /// </summary>
        public static List<int> TargetsFor(string key, IList<ChestTarget> chests)
        {
            var result = new List<ChestTarget>();
            foreach (ChestTarget c in chests)
            {
                if (c.Accepts.Contains(key))
                {
                    result.Add(c);
                }
            }
            result.Sort((a, b) =>
            {
                int byDistance = a.Distance.CompareTo(b.Distance);
                return byDistance != 0 ? byDistance : a.Index.CompareTo(b.Index);
            });
            return result.ConvertAll(c => c.Index);
        }

        /// <summary>Есть ли смысл просить сундук: принимает ли он хоть один из ключей источника.</summary>
        public static bool AcceptsAny(ChestTarget chest, ICollection<string> sourceKeys)
        {
            foreach (string key in sourceKeys)
            {
                if (chest.Accepts.Contains(key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Переносит ли кнопка «Загрузить ресурсы» этот предмет из инвентаря игрока.</summary>
        public static bool ShouldLoad(string prefabName, bool isMaterial, bool equipped, int gridY, LoadSettings s)
        {
            if (equipped)
            {
                return false;
            }
            if (s.SkipHotbar && gridY == 0)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(prefabName))
            {
                if (s.Excluded.Contains(prefabName))
                {
                    return false;
                }
                if (s.Extra.Contains(prefabName))
                {
                    return true;
                }
            }
            return s.Materials && isMaterial;
        }

        /// <summary>Список имён через запятую/точку с запятой → множество (без пустых, без учёта регистра).</summary>
        public static HashSet<string> ParseNames(string list)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(list))
            {
                return result;
            }
            foreach (string part in list.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Trim();
                if (name.Length > 0)
                {
                    result.Add(name);
                }
            }
            return result;
        }

        /// <summary>
        /// «Предмет:Кол-во, Предмет:Кол-во» → список пар. Количество по умолчанию 1; неверные части пропускаются
        /// (их имена попадают в <paramref name="errors"/>).
        /// </summary>
        public static List<KeyValuePair<string, int>> ParseCost(string cost, List<string> errors = null)
        {
            var result = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrWhiteSpace(cost))
            {
                return result;
            }
            foreach (string part in cost.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = part.Split(':');
                string item = kv[0].Trim();
                int amount = 1;
                if (item.Length == 0 || kv.Length > 2 || (kv.Length == 2 && !int.TryParse(kv[1].Trim(), out amount)) || amount <= 0)
                {
                    errors?.Add(part.Trim());
                    continue;
                }
                result.Add(new KeyValuePair<string, int>(item, amount));
            }
            return result;
        }

        /// <summary>Сложить два рецепта: одинаковые предметы суммируются, порядок — как в первом, потом новые.</summary>
        public static List<KeyValuePair<string, int>> MergeCost(IEnumerable<KeyValuePair<string, int>> a,
            IEnumerable<KeyValuePair<string, int>> b)
        {
            var order = new List<string>();
            var sum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (IEnumerable<KeyValuePair<string, int>> list in new[] { a, b })
            {
                foreach (KeyValuePair<string, int> r in list)
                {
                    if (sum.TryGetValue(r.Key, out int have))
                    {
                        sum[r.Key] = have + r.Value;
                    }
                    else
                    {
                        sum[r.Key] = r.Value;
                        order.Add(r.Key);
                    }
                }
            }
            return order.ConvertAll(k => new KeyValuePair<string, int>(k, sum[k]));
        }
    }
}
