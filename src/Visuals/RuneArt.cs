using System;

namespace SortingChest.Visuals
{
    /// <summary>
    /// Процедурные текстуры рун (без Unity: массив RGBA32, строки снизу вверх — как ждёт Texture2D).
    /// Альфа не домножена (шейдер Sprites/Default домножает сам): фон прозрачный, руны — светящиеся линии
    /// с мягким ореолом (ореол — та же бирюза с меньшей альфой, сердцевина почти белая).
    /// </summary>
    public static class RuneArt
    {
        /// <summary>Отрезки руны в клетке [0,1]×[0,1] (x вправо, y вверх): x1, y1, x2, y2, ...</summary>
        public static readonly float[][] Futhark =
        {
            new[] { .35f, 0f, .35f, 1f, .35f, .5f, .75f, .75f, .35f, .75f, .75f, 1f },                       // ᚠ fehu
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .7f, .7f, .7f, .7f, .7f, 0f },                                 // ᚢ uruz
            new[] { .35f, 0f, .35f, 1f, .35f, .75f, .7f, .5f, .7f, .5f, .35f, .25f },                         // ᚦ thurisaz
            new[] { .35f, 0f, .35f, 1f, .35f, 1f, .72f, .72f, .35f, .72f, .72f, .44f },                       // ᚨ ansuz
            new[] { .32f, 0f, .32f, 1f, .32f, 1f, .7f, .78f, .7f, .78f, .32f, .55f, .32f, .55f, .72f, 0f },   // ᚱ raido
            new[] { .65f, 1f, .35f, .5f, .35f, .5f, .65f, 0f },                                               // ᚲ kaunan
            new[] { .2f, 0f, .8f, 1f, .2f, 1f, .8f, 0f },                                                     // ᚷ gebo
            new[] { .35f, 0f, .35f, 1f, .35f, 1f, .68f, .78f, .68f, .78f, .35f, .55f },                       // ᚹ wunjo
            new[] { .25f, 0f, .25f, 1f, .75f, 0f, .75f, 1f, .25f, .7f, .75f, .3f },                           // ᚺ hagalaz
            new[] { .5f, 0f, .5f, 1f, .3f, .65f, .7f, .35f },                                                 // ᚾ naudiz
            new[] { .5f, 0f, .5f, 1f },                                                                       // ᛁ isa
            new[] { .45f, .95f, .2f, .7f, .2f, .7f, .45f, .45f, .55f, .55f, .8f, .3f, .8f, .3f, .55f, .05f }, // ᛃ jera
            new[] { .5f, 0f, .5f, 1f, .5f, 1f, .75f, .8f, .5f, 0f, .25f, .2f },                               // ᛇ eihwaz
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .68f, .72f, .68f, .72f, .68f, .92f, .3f, 0f, .68f, .28f, .68f, .28f, .68f, .08f }, // ᛈ perthro
            new[] { .5f, 0f, .5f, 1f, .5f, .55f, .2f, .9f, .5f, .55f, .8f, .9f },                             // ᛉ algiz
            new[] { .65f, 1f, .3f, .6f, .3f, .6f, .7f, .4f, .7f, .4f, .35f, 0f },                             // ᛋ sowilo
            new[] { .5f, 0f, .5f, 1f, .5f, 1f, .2f, .7f, .5f, 1f, .8f, .7f },                                 // ᛏ tiwaz
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .7f, .75f, .7f, .75f, .3f, .5f, .3f, .5f, .7f, .25f, .7f, .25f, .3f, 0f }, // ᛒ berkanan
            new[] { .2f, 0f, .2f, 1f, .8f, 0f, .8f, 1f, .2f, 1f, .5f, .7f, .5f, .7f, .8f, 1f },               // ᛖ ehwaz
            new[] { .2f, 0f, .2f, 1f, .8f, 0f, .8f, 1f, .2f, 1f, .8f, .55f, .8f, 1f, .2f, .55f },             // ᛗ mannaz
            new[] { .38f, 0f, .38f, 1f, .38f, 1f, .72f, .72f },                                               // ᛚ laguz
            new[] { .5f, 1f, .25f, .5f, .25f, .5f, .5f, 0f, .5f, 0f, .75f, .5f, .75f, .5f, .5f, 1f },         // ᛜ ingwaz
            new[] { .15f, 0f, .15f, 1f, .85f, 0f, .85f, 1f, .15f, 1f, .85f, 0f, .15f, 0f, .85f, 1f },         // ᛞ dagaz
            new[] { .5f, 1f, .25f, .7f, .5f, 1f, .75f, .7f, .25f, .7f, .8f, 0f, .75f, .7f, .2f, 0f },         // ᛟ othala
        };

        // Индексы в Futhark.
        public const int Gebo = 6;
        public const int Ansuz = 3;
        public const int Laguz = 20;
        public const int Dagaz = 22;
        public const int Raido = 4;

        /// <summary>«ᚷᚨᛚᛞᚱ» — galdr, заклинание (для иконки мода, tests/icon.py).</summary>
        public static readonly int[] GaldrWord = { Gebo, Ansuz, Laguz, Dagaz, Raido };

        // Цвета свечения (0..1): ореол бирюзовый, сердцевина почти белая.
        private static readonly float[] s_glow = { 0.12f, 0.78f, 1.0f };
        private static readonly float[] s_core = { 0.82f, 1.0f, 1.0f };

        /// <summary>
        /// Магический круг: две пары колец и между ними все 24 руны старшего футарка по кругу, основанием к центру.
        /// Середина прозрачна (там стоит сундук).
        /// </summary>
        public static byte[] Circle(int size)
        {
            var px = new byte[size * size * 4];
            float half = size * 0.5f;
            float pix = 1f / half; // один пиксель в единицах радиуса
            const float runeIn = 0.745f, runeOut = 0.895f;
            const float runeW = 0.105f;
            int count = Futhark.Length;
            float slot = (float)(2.0 * Math.PI / count);
            float hw = Math.Max(0.0065f, 1.2f * pix); // полутолщина линии
            float glowR = 0.028f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f - half) / half;
                    float v = (y + 0.5f - half) / half;
                    float r = (float)Math.Sqrt(u * u + v * v);
                    if (r > 1f || r < 0.55f)
                    {
                        continue;
                    }
                    float d = float.MaxValue;
                    // Кольца: снаружи двойное, внутри двойное.
                    d = Math.Min(d, Math.Abs(r - 0.965f));
                    d = Math.Min(d, Math.Abs(r - 0.925f) * 1.6f);
                    d = Math.Min(d, Math.Abs(r - 0.715f) * 1.6f);
                    d = Math.Min(d, Math.Abs(r - 0.675f));

                    // Руна своего сектора: локальные координаты — вдоль окружности (t) и от центра (h).
                    float angle = (float)Math.Atan2(v, u);
                    if (angle < 0f)
                    {
                        angle += (float)(2.0 * Math.PI);
                    }
                    int k = (int)(angle / slot) % count;
                    float center = (k + 0.5f) * slot;
                    // Руна стоит «ногами» к центру: y клетки → наружу, x → по часовой стрелке
                    // (если «верх» руны смотрит от центра, её «право» — по часовой; иначе руны вышли бы зеркальными).
                    float t = (center - angle) * r;
                    float h = r - runeIn;
                    float[] seg = Futhark[k];
                    for (int s = 0; s + 3 < seg.Length; s += 4)
                    {
                        float ax = (seg[s] - 0.5f) * runeW, ay = seg[s + 1] * (runeOut - runeIn);
                        float bx = (seg[s + 2] - 0.5f) * runeW, by = seg[s + 3] * (runeOut - runeIn);
                        d = Math.Min(d, SegmentDistance(t, h, ax, ay, bx, by));
                    }
                    // Точки-заклёпки между кольцами напротив каждой руны.
                    float dotA = (angle - k * slot) * 0.945f;
                    d = Math.Min(d, (float)Math.Sqrt(dotA * dotA + (r - 0.945f) * (r - 0.945f)) - 0.006f);

                    Shade(px, (y * size + x) * 4, d, hw, glowR, pix, 1f);
                }
            }
            return px;
        }

        /// <summary>Атлас из одной строки клеток: в каждой — одна руна, высокая и толстая, с ореолом (для иконки мода).</summary>
        public static byte[] GlyphAtlas(int[] runes, int cell, out int width, out int height)
        {
            width = cell * runes.Length;
            height = cell;
            var px = new byte[width * height * 4];
            float pix = 1f / cell;
            const float hw = 0.04f, glowR = 0.11f;
            for (int i = 0; i < runes.Length; i++)
            {
                float[] seg = Futhark[runes[i]];
                for (int y = 0; y < cell; y++)
                {
                    for (int x = 0; x < cell; x++)
                    {
                        float u = (x + 0.5f) / cell, v = (y + 0.5f) / cell;
                        float d = float.MaxValue;
                        for (int s = 0; s + 3 < seg.Length; s += 4)
                        {
                            // Клетка руны: высота 0.76, ширина 0.5, по центру.
                            float ax = 0.25f + seg[s] * 0.5f, ay = 0.12f + seg[s + 1] * 0.76f;
                            float bx = 0.25f + seg[s + 2] * 0.5f, by = 0.12f + seg[s + 3] * 0.76f;
                            d = Math.Min(d, SegmentDistance(u, v, ax, ay, bx, by));
                        }
                        Shade(px, (y * width + i * cell + x) * 4, d, hw, glowR, pix, 1f);
                    }
                }
            }
            return px;
        }

        /// <summary>Пиксель по расстоянию до ближайшей линии: сердцевина со сглаженным краем плюс ореол.</summary>
        private static void Shade(byte[] px, int i, float d, float hw, float glowR, float pix, float strength)
        {
            float core = SmoothStep(hw + pix, hw - pix, d);
            float g = Math.Max(0f, d - hw) / glowR;
            float glow = (float)Math.Exp(-g * g * 2.2) * 0.55f;
            float a = Math.Min(1f, Math.Max(core, glow)) * strength;
            if (a <= 0.002f)
            {
                return;
            }
            for (int c = 0; c < 3; c++)
            {
                px[i + c] = ToByte(s_glow[c] + (s_core[c] - s_glow[c]) * core);
            }
            px[i + 3] = ToByte(a);
        }

        /// <summary>Мягкая белая точка для частиц и «огоньков»: альфа спадает от центра к краю.</summary>
        public static byte[] Dot(int size)
        {
            var px = new byte[size * size * 4];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f - half) / half, v = (y + 0.5f - half) / half;
                    float r = (float)Math.Sqrt(u * u + v * v);
                    float a = r >= 1f ? 0f : (float)Math.Pow(1f - r, 2.2) * 0.85f + SmoothStep(0.35f, 0f, r) * 0.15f;
                    int i = (y * size + x) * 4;
                    px[i] = px[i + 1] = px[i + 2] = 255;
                    px[i + 3] = ToByte(a);
                }
            }
            return px;
        }

        public static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float len2 = dx * dx + dy * dy;
            float t = len2 > 1e-12f ? ((px - ax) * dx + (py - ay) * dy) / len2 : 0f;
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            float cx = ax + t * dx - px, cy = ay + t * dy - py;
            return (float)Math.Sqrt(cx * cx + cy * cy);
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = (x - edge0) / (edge1 - edge0);
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return t * t * (3f - 2f * t);
        }

        private static byte ToByte(float f)
        {
            int v = (int)(f * 255f + 0.5f);
            return (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
        }
    }
}
