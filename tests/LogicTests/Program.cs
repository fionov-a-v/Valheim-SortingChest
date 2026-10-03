using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SortingChest.Logic;
using SortingChest.Visuals;

namespace SortingChest.Tests
{
    /// <summary>
    /// Проверки правил сортировки и загрузки + PNG-превью рун (dotnet run -- &lt;папка для png&gt;).
    /// </summary>
    internal static class Program
    {
        private static int s_failed;
        private static int s_passed;

        private static int Main(string[] args)
        {
            Eligibility();
            FirstItem();
            Targets();
            Loading();
            Parsing();

            string outDir = args.Length > 0 ? args[0] : null;
            if (outDir != null)
            {
                Directory.CreateDirectory(outDir);
                WritePng(Path.Combine(outDir, "circle.png"), RuneArt.Circle(512), 512, 512);
                byte[] atlas = RuneArt.GlyphAtlas(RuneArt.GaldrWord, 128, out int w, out int h);
                WritePng(Path.Combine(outDir, "galdr_glyphs.png"), atlas, w, h);
                WritePng(Path.Combine(outDir, "dot.png"), RuneArt.Dot(64), 64, 64);
                // С прозрачностью — для сборки иконки (tests/icon.py).
                WritePng(Path.Combine(outDir, "circle_rgba.png"), RuneArt.Circle(1024), 1024, 1024, alpha: true);
                byte[] big = RuneArt.GlyphAtlas(RuneArt.GaldrWord, 256, out int bw, out int bh);
                WritePng(Path.Combine(outDir, "glyphs_rgba.png"), big, bw, bh, alpha: true);
                Console.WriteLine($"PNG: {outDir}");
            }

            Console.WriteLine($"passed {s_passed}, failed {s_failed}");
            return s_failed == 0 ? 0 : 1;
        }

        // ------------------------------------------------------------------ правила

        private static ChestFacts Chest(float distance = 5f) => new ChestFacts { BuiltByPlayer = true, Distance = distance };

        private static void Eligibility()
        {
            var s = new SortSettings { Radius = 20f };
            Check("обычный сундук рядом", SortRules.IsEligible(Chest(), s));
            Check("ровно на границе радиуса", SortRules.IsEligible(Chest(20f), s));
            Check("дальше радиуса — нет", !SortRules.IsEligible(Chest(20.01f), s));
            Check("сам себе — нет", !SortRules.IsEligible(new ChestFacts { IsSelf = true, BuiltByPlayer = true }, s));
            Check("другой распределяющий сундук — нет", !SortRules.IsEligible(new ChestFacts { IsSortingChest = true, BuiltByPlayer = true }, s));
            Check("сундук подземелья/могила — нет", !SortRules.IsEligible(new ChestFacts { BuiltByPlayer = false }, s));
            Check("под чужим охранным камнем — нет", !SortRules.IsEligible(new ChestFacts { BuiltByPlayer = true, WardBlocked = true }, s));

            var ownPrivate = new ChestFacts { BuiltByPlayer = true, IsPrivate = true, IsOwnPrivate = true };
            var otherPrivate = new ChestFacts { BuiltByPlayer = true, IsPrivate = true, IsOwnPrivate = false };
            Check("личный по умолчанию исключён", !SortRules.IsEligible(ownPrivate, s));
            var withPrivate = new SortSettings { Radius = 20f, IncludePrivate = true };
            Check("свой личный — если включено", SortRules.IsEligible(ownPrivate, withPrivate));
            Check("чужой личный — никогда", !SortRules.IsEligible(otherPrivate, withPrivate));

            Check("машина (печь, уничтожитель) — нет", !SortRules.IsEligible(new ChestFacts { BuiltByPlayer = true, IsMachine = true }, s));
            var excluded = new SortSettings { Radius = 20f, Excluded = SortRules.ParseNames("piece_chest_barrel, BBS_Something") };
            Check("исключён в настройках (без учёта регистра)", !SortRules.IsEligible(new ChestFacts { BuiltByPlayer = true, PrefabName = "Piece_Chest_Barrel" }, excluded));
            Check("не в списке исключений — можно", SortRules.IsEligible(new ChestFacts { BuiltByPlayer = true, PrefabName = "piece_chest" }, excluded));

            // Компоненты реальных построек: сундук — не машина; печь BetterStations и ванильный уничтожитель — машины.
            Check("сундук — не машина", !SortRules.LooksLikeMachine(new[] { "ZNetView", "Piece", "Container", "WearNTear" }));
            Check("наш сундук — не машина", !SortRules.LooksLikeMachine(new[] { "ZNetView", "Piece", "Container", "WearNTear", "DistributingChest", "CircleProjector" }));
            Check("угольная печь BetterStations — машина", SortRules.LooksLikeMachine(new[] { "ZNetView", "Piece", "Container", "WearNTear", "BetterSmelter" }));
            Check("бродильня BetterStations — машина", SortRules.LooksLikeMachine(new[] { "ZNetView", "Container", "BetterFermenter" }));
            Check("уничтожитель — машина", SortRules.LooksLikeMachine(new[] { "ZNetView", "Piece", "Container", "Incinerator" }));

            var cart = new ChestFacts { BuiltByPlayer = true, IsVehicle = true };
            Check("тележка/корабль по умолчанию исключены", !SortRules.IsEligible(cart, s));
            Check("тележка/корабль — если включено", SortRules.IsEligible(cart, new SortSettings { IncludeVehicles = true }));
        }

        private static void FirstItem()
        {
            var items = new List<SlotItem>
            {
                new SlotItem("$item_stone", 3, 0),
                new SlotItem("$item_wood", 1, 1),
                new SlotItem("$item_resin", 0, 2),
                new SlotItem("$item_flint", 2, 0),
            };
            Check("первый — верхняя строка, левый столбец", SortRules.FirstInGridOrder(items) == 3);
            Check("пустой сундук — -1", SortRules.FirstInGridOrder(new List<SlotItem>()) == -1);

            HashSet<string> first = SortRules.AcceptedKeys(items, MatchMode.FirstItem);
            Check("FirstItem: принимает только первый", first.Count == 1 && first.Contains("$item_flint"));
            HashSet<string> any = SortRules.AcceptedKeys(items, MatchMode.AnyItem);
            Check("AnyItem: принимает всё, что лежит", any.Count == 4);
            Check("пустой сундук ничего не принимает", SortRules.AcceptedKeys(new List<SlotItem>(), MatchMode.FirstItem).Count == 0);
        }

        private static void Targets()
        {
            var chests = new List<ChestTarget>
            {
                new ChestTarget { Index = 0, Distance = 12f, Accepts = { "$item_wood" } },
                new ChestTarget { Index = 1, Distance = 4f, Accepts = { "$item_stone" } },
                new ChestTarget { Index = 2, Distance = 3f, Accepts = { "$item_wood" } },
                new ChestTarget { Index = 3, Distance = 3f, Accepts = { "$item_wood" } },
            };
            List<int> wood = SortRules.TargetsFor("$item_wood", chests);
            Check("дерево: ближние первыми, при равенстве — по порядку", Seq(wood, 2, 3, 0));
            Check("камень: один сундук", Seq(SortRules.TargetsFor("$item_stone", chests), 1));
            Check("смола: некуда — остаётся в распределяющем сундуке", SortRules.TargetsFor("$item_resin", chests).Count == 0);
            Check("AcceptsAny: да", SortRules.AcceptsAny(chests[1], new[] { "$item_resin", "$item_stone" }));
            Check("AcceptsAny: нет", !SortRules.AcceptsAny(chests[1], new[] { "$item_resin" }));
        }

        private static void Loading()
        {
            var s = new LoadSettings();
            Check("материал — берём", SortRules.ShouldLoad("Wood", true, false, 2, s));
            Check("слиток — материал, берём", SortRules.ShouldLoad("Iron", true, false, 1, s));
            Check("не материал (еда) — нет", !SortRules.ShouldLoad("CookedMeat", false, false, 1, s));
            Check("надетое — никогда", !SortRules.ShouldLoad("Wood", true, true, 1, s));
            Check("ряд быстрого доступа берём по умолчанию", SortRules.ShouldLoad("Wood", true, false, 0, s));

            var skip = new LoadSettings { SkipHotbar = true };
            Check("SkipHotbar: ряд 0 не трогаем", !SortRules.ShouldLoad("Wood", true, false, 0, skip));
            Check("SkipHotbar: остальные ряды берём", SortRules.ShouldLoad("Wood", true, false, 1, skip));

            var lists = new LoadSettings
            {
                Extra = SortRules.ParseNames("TrophySkeleton"),
                Excluded = SortRules.ParseNames("surtlingcore, Wood"),
            };
            Check("Extra: трофей берём", SortRules.ShouldLoad("TrophySkeleton", false, false, 1, lists));
            Check("Excluded (без учёта регистра)", !SortRules.ShouldLoad("SurtlingCore", true, false, 1, lists));
            Check("Excluded сильнее Extra", !SortRules.ShouldLoad("Wood", true, false, 1, new LoadSettings
            {
                Extra = SortRules.ParseNames("Wood"),
                Excluded = SortRules.ParseNames("Wood"),
            }));
            Check("Materials=false: только Extra", !SortRules.ShouldLoad("Stone", true, false, 1, new LoadSettings { Materials = false }));
        }

        private static void Parsing()
        {
            var errors = new List<string>();
            List<KeyValuePair<string, int>> cost = SortRules.ParseCost("TrophySkeleton:1, SurtlingCore:2 ;GreydwarfEye:5, Bad:x, Lone", errors);
            Check("рецепт: 4 позиции", cost.Count == 4);
            Check("рецепт: количество", cost[1].Key == "SurtlingCore" && cost[1].Value == 2);
            Check("рецепт: без количества — 1", cost[3].Key == "Lone" && cost[3].Value == 1);
            Check("рецепт: ошибка запомнена", errors.Count == 1 && errors[0] == "Bad:x");
            Check("пустой рецепт", SortRules.ParseCost("  ").Count == 0);

            List<KeyValuePair<string, int>> merged = SortRules.MergeCost(
                SortRules.ParseCost("FineWood:10,Iron:2"),
                SortRules.ParseCost("TrophySkeleton:1,SurtlingCore:2,GreydwarfEye:5,iron:1"));
            Check("сложение рецептов: порядок и суммы",
                merged.Count == 5 && merged[0].Key == "FineWood" && merged[1].Key == "Iron" && merged[1].Value == 3 && merged[4].Key == "GreydwarfEye");
        }

        // ------------------------------------------------------------------ вспомогательное

        private static bool Seq(List<int> actual, params int[] expected)
        {
            if (actual.Count != expected.Length)
            {
                return false;
            }
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] != expected[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static void Check(string name, bool ok)
        {
            if (ok)
            {
                s_passed++;
            }
            else
            {
                s_failed++;
                Console.WriteLine("FAIL: " + name);
            }
        }

        /// <summary>
        /// RGBA32, строки снизу вверх (как в Texture2D) → PNG: на тёмном фоне, чтобы было видно свечение,
        /// или (alpha) как есть, с прозрачностью.
        /// </summary>
        private static void WritePng(string path, byte[] rgba, int w, int h, bool alpha = false)
        {
            int channels = alpha ? 4 : 3;
            var raw = new byte[(w * channels + 1) * h];
            for (int y = 0; y < h; y++)
            {
                int src = (h - 1 - y) * w * 4;
                int dst = y * (w * channels + 1);
                raw[dst] = 0;
                if (alpha)
                {
                    Buffer.BlockCopy(rgba, src, raw, dst + 1, w * 4);
                    continue;
                }
                for (int x = 0; x < w; x++)
                {
                    // Альфа не домножена: обычное смешивание поверх фона (20, 24, 30), как у Sprites/Default.
                    int a = rgba[src + x * 4 + 3];
                    for (int c = 0; c < 3; c++)
                    {
                        int bg = c == 0 ? 20 : (c == 1 ? 24 : 30);
                        raw[dst + 1 + x * 3 + c] = (byte)((bg * (255 - a) + rgba[src + x * 4 + c] * a) / 255);
                    }
                }
            }
            using var fs = File.Create(path);
            fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var ihdr = new byte[13];
            BigEndian(ihdr, 0, w);
            BigEndian(ihdr, 4, h);
            ihdr[8] = 8;
            ihdr[9] = (byte)(alpha ? 6 : 2);
            Chunk(fs, "IHDR", ihdr);
            using (var ms = new MemoryStream())
            {
                using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true))
                {
                    z.Write(raw, 0, raw.Length);
                }
                Chunk(fs, "IDAT", ms.ToArray());
            }
            Chunk(fs, "IEND", Array.Empty<byte>());
        }

        private static void Chunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4];
            BigEndian(len, 0, data.Length);
            s.Write(len);
            byte[] t = System.Text.Encoding.ASCII.GetBytes(type);
            s.Write(t);
            s.Write(data);
            uint crc = Crc(t, 0xFFFFFFFFu);
            crc = Crc(data, crc) ^ 0xFFFFFFFFu;
            var c = new byte[4];
            BigEndian(c, 0, (int)crc);
            s.Write(c);
        }

        private static uint Crc(byte[] data, uint crc)
        {
            foreach (byte b in data)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++)
                {
                    crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
                }
            }
            return crc;
        }

        private static void BigEndian(byte[] b, int o, int v)
        {
            b[o] = (byte)(v >> 24);
            b[o + 1] = (byte)(v >> 16);
            b[o + 2] = (byte)(v >> 8);
            b[o + 3] = (byte)v;
        }
    }
}
