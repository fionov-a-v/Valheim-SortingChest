"""Иконка мода 256×256: армированный сундук с рунной пластиной ᚷ, стоящий в центре круга рун на полу.
Текстуры рун — те же, что рисует мод (RuneArt), выгружает их тест:

dotnet run --project tests/LogicTests -c Release -- out
python3 tests/icon.py out package/icon.png                 иконка 256×256
python3 tests/icon.py out media/banner.png --banner        баннер 1300×372 (шапка страницы мода)
"""
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

S = 1024  # рисуем крупно и уменьшаем — сглаживание
TEAL = (77, 224, 255)
SHIFT_UP = 120


def radial_background():
    bg = Image.new("RGB", (S, S))
    px = bg.load()
    for y in range(S):
        for x in range(S):
            d = math.hypot(x - S * 0.5, y - S * 0.56) / (S * 0.72)
            t = max(0.0, 1.0 - d)
            px[x, y] = (int(6 + 14 * t), int(10 + 34 * t), int(16 + 44 * t))
    return bg


def glow(layer, radius, strength):
    """Размытая копия слоя, сложенная с ним самим, — ореол."""
    blurred = layer.filter(ImageFilter.GaussianBlur(radius))
    if strength != 1.0:
        r, g, b, a = blurred.split()
        a = a.point(lambda v: min(255, int(v * strength)))
        blurred = Image.merge("RGBA", (r, g, b, a))
    out = Image.new("RGBA", layer.size)
    out.alpha_composite(blurred)
    out.alpha_composite(layer)
    return out


def ground_circle(tex_dir):
    circle = Image.open(os.path.join(tex_dir, "circle_rgba.png")).convert("RGBA")
    # Круг лежит на земле: сжат по вертикали (вид сверху-спереди).
    w, h = int(S * 0.96), int(S * 0.96 * 0.36)
    flat = circle.resize((w, h), Image.LANCZOS)
    layer = Image.new("RGBA", (S, S))
    layer.alpha_composite(flat, ((S - w) // 2, int(S * 0.80) - h // 2))
    return glow(layer, 10, 1.4)


def chest(plate=True):
    layer = Image.new("RGBA", (S, S))
    d = ImageDraw.Draw(layer)
    wood, wood_dark = (98, 66, 38, 255), (66, 43, 25, 255)
    iron, iron_hi = (44, 54, 66, 255), (112, 128, 146, 255)

    x0, x1 = 262, 762          # корпус
    body_top, body_bot = 520, 815
    lid_top = 372

    # Корпус и доски.
    d.rectangle((x0, body_top, x1, body_bot), fill=wood)
    for i in range(1, 5):
        y = body_top + (body_bot - body_top) * i / 5
        d.line((x0, y, x1, y), fill=wood_dark, width=6)
    # Крышка: скруглённый верх.
    d.rounded_rectangle((x0 - 10, lid_top, x1 + 10, body_top + 18), radius=70, fill=wood)
    for i in range(1, 4):
        y = lid_top + 40 + (body_top - lid_top - 40) * i / 4
        d.line((x0, y, x1, y), fill=wood_dark, width=6)
    # Железные полосы: по углам, по стыку крышки и посередине крышки.
    for bx in (x0, x1 - 52):
        d.rectangle((bx, lid_top + 34, bx + 52, body_bot), fill=iron)
        d.line((bx + 6, lid_top + 40, bx + 6, body_bot - 6), fill=iron_hi, width=4)
    d.rectangle((x0 - 14, body_top - 6, x1 + 14, body_top + 30), fill=iron)
    d.line((x0 - 8, body_top, x1 + 8, body_top), fill=iron_hi, width=4)
    d.rectangle((x0 - 6, body_bot - 34, x1 + 6, body_bot), fill=iron)
    # Заклёпки.
    for bx in (x0 + 26, x1 - 26):
        for by in (body_top + 70, body_top + 160, body_top + 250):
            d.ellipse((bx - 9, by - 9, bx + 9, by + 9), fill=iron_hi)
    # Замок.
    d.rounded_rectangle((470, body_top - 30, 554, body_top + 80), radius=10, fill=iron, outline=iron_hi, width=5)
    d.ellipse((498, body_top + 10, 526, body_top + 38), fill=(20, 24, 30, 255))
    # Рунная пластина на лицевой стороне (только у распределяющего сундука).
    if plate:
        d.rounded_rectangle((404, body_top + 86, 620, body_bot - 40), radius=14, fill=(26, 34, 44, 255), outline=iron_hi, width=5)
    return layer


def plate_rune(tex_dir):
    glyphs = Image.open(os.path.join(tex_dir, "glyphs_rgba.png")).convert("RGBA")
    cell = glyphs.height
    gebo = glyphs.crop((0, 0, cell, cell)).resize((190, 190), Image.LANCZOS)
    layer = Image.new("RGBA", (S, S))
    layer.alpha_composite(gebo, (512 - 95, 690 - 95))
    return glow(layer, 14, 1.6)


def sparks():
    layer = Image.new("RGBA", (S, S))
    d = ImageDraw.Draw(layer)
    rnd = random.Random(7)
    for _ in range(40):
        a = rnd.uniform(0, math.tau)
        x = 512 + math.cos(a) * rnd.uniform(330, 470)
        y = 820 + math.sin(a) * rnd.uniform(110, 170) - rnd.uniform(0, 420)
        r = rnd.uniform(4, 11)
        if 230 < x < 794 and 340 < y < 830:
            continue  # не поверх сундука
        d.ellipse((x - r, y - r, x + r, y + r), fill=TEAL + (int(rnd.uniform(120, 230)),))
    return glow(layer, 8, 1.8)


def banner(tex_dir, out):
    """Ночная база: в центре распределяющий сундук в круге рун, по сторонам обычные сундуки, к ним летят огоньки."""
    W, H = 2600, 744  # вдвое больше итоговых 1300×372
    img = Image.new("RGB", (W, H))
    px = img.load()
    for y in range(H):
        t = y / (H - 1)
        for x in range(W):
            glow_c = max(0.0, 1.0 - math.hypot((x - W / 2) / (W * 0.45), (y - H * 0.78) / (H * 0.9)))
            px[x, y] = (int(6 + 8 * t + 10 * glow_c), int(10 + 16 * t + 30 * glow_c), int(18 + 18 * t + 38 * glow_c))
    img = img.convert("RGBA")

    # Звёзды и гряды гор вдали, как на баннере воздушных шаров.
    stars = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(stars)
    rnd = random.Random(3)
    for _ in range(140):
        x, y, r = rnd.uniform(0, W), rnd.uniform(0, H * 0.55), rnd.uniform(1, 2.6)
        d.ellipse((x - r, y - r, x + r, y + r), fill=(200, 230, 255, int(rnd.uniform(60, 170))))
    img.alpha_composite(stars)
    for layer_i, (base, amp, col) in enumerate([(470, 60, (16, 34, 44, 255)), (530, 45, (11, 24, 32, 255)), (585, 30, (8, 16, 22, 255))]):
        hills = Image.new("RGBA", (W, H))
        pts = [(0, H)]
        for x in range(0, W + 20, 20):
            y = base - amp * (0.55 * math.sin(x / 210 + layer_i * 1.7) + 0.3 * math.sin(x / 97 + layer_i) + 0.15 * math.sin(x / 41))
            pts.append((x, y))
        pts.append((W, H))
        ImageDraw.Draw(hills).polygon(pts, fill=col)
        img.alpha_composite(hills)

    def place(layer, scale, cx, base_y):
        """Слой 1024×1024 (сундук стоит низом на y=815) — уменьшить и поставить низом на base_y по центру cx."""
        size = int(S * scale)
        small = layer.resize((size, size), Image.LANCZOS)
        img.alpha_composite(small, (int(cx - size / 2), int(base_y - 815 * scale)))

    # Обычные сундуки: дальние меньше и выше, ближние крупнее.
    plain = chest(plate=False)
    targets = [(330, 600, 0.30), (700, 660, 0.40), (1900, 660, 0.40), (2270, 600, 0.30)]
    shadows = Image.new("RGBA", (W, H))
    for cx, base_y, scale in targets:
        w = 560 * scale
        ImageDraw.Draw(shadows).ellipse((cx - w, base_y - 22 * scale, cx + w, base_y + 40 * scale), fill=(0, 0, 0, 150))
    img.alpha_composite(shadows.filter(ImageFilter.GaussianBlur(10)))
    for cx, base_y, scale in targets:
        place(plain, scale, cx, base_y)

    # Распределяющий сундук в круге рун — в центре.
    center = Image.new("RGBA", (S, S))
    center.alpha_composite(ground_circle(tex_dir))
    light = Image.new("RGBA", (S, S))
    ImageDraw.Draw(light).ellipse((200, 640, 824, 900), fill=TEAL + (90,))
    center.alpha_composite(light.filter(ImageFilter.GaussianBlur(60)))
    body = chest()
    edge = body.filter(ImageFilter.GaussianBlur(26))
    tint = Image.new("RGBA", (S, S), TEAL + (0,))
    tint.putalpha(ImageChops.multiply(edge.split()[3], Image.new("L", (S, S), 110)))
    center.alpha_composite(tint)
    center.alpha_composite(body)
    center.alpha_composite(plate_rune(tex_dir))
    center_base = 600
    place(center, 0.62, W / 2, center_base)

    # Огоньки: дуги искр от крышки распределяющего сундука к крышкам обычных, яркая голова у конца.
    wisps = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(wisps)
    start = (W / 2, center_base - 470 * 0.62)
    for i, (cx, base_y, scale) in enumerate(targets):
        end = (cx, base_y - 440 * scale)
        ctrl = ((start[0] + end[0]) / 2, min(start[1], end[1]) - 120 - abs(end[0] - start[0]) * 0.06)
        progress = (0.86, 0.95, 0.92, 0.82)[i]  # огоньки в полёте, почти у цели
        steps = 70
        for k in range(steps):
            t = progress * k / (steps - 1)
            u = 1 - t
            x = u * u * start[0] + 2 * u * t * ctrl[0] + t * t * end[0]
            y = u * u * start[1] + 2 * u * t * ctrl[1] + t * t * end[1]
            a = int(40 + 190 * (k / steps) ** 2)
            r = 2 + 6 * (k / steps) ** 2
            d.ellipse((x - r, y - r, x + r, y + r), fill=TEAL + (a,))
        d.ellipse((x - 16, y - 16, x + 16, y + 16), fill=(220, 255, 255, 255))
    img.alpha_composite(glow(wisps, 12, 1.8))

    img.convert("RGB").resize((W // 2, H // 2), Image.LANCZOS).save(out)
    print(out)


def main():
    tex_dir, out = sys.argv[1], sys.argv[2]
    if "--banner" in sys.argv[3:]:
        banner(tex_dir, out)
        return
    scene = Image.new("RGBA", (S, S))
    scene.alpha_composite(ground_circle(tex_dir))
    # Мягкий голубой отсвет под сундуком.
    light = Image.new("RGBA", (S, S))
    ImageDraw.Draw(light).ellipse((200, 640, 824, 900), fill=TEAL + (90,))
    scene.alpha_composite(light.filter(ImageFilter.GaussianBlur(60)))
    body = chest()
    shadow_edge = body.filter(ImageFilter.GaussianBlur(26))
    tint = Image.new("RGBA", (S, S), TEAL + (0,))
    tint.putalpha(ImageChops.multiply(shadow_edge.split()[3], Image.new("L", (S, S), 110)))
    scene.alpha_composite(tint)
    scene.alpha_composite(body)
    scene.alpha_composite(plate_rune(tex_dir))
    scene.alpha_composite(sparks())
    # Сундук с кругом — по центру кадра: сцена нарисована ниже середины, поднимаем её.
    img = radial_background().convert("RGBA")
    img.alpha_composite(scene, (0, -SHIFT_UP))
    img.convert("RGB").resize((256, 256), Image.LANCZOS).save(out)
    print(out)


if __name__ == "__main__":
    main()
