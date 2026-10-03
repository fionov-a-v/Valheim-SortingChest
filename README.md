# Распределяющий сундук (Sorting Chest) — мод для Valheim

Автор: Fionov Alexander · исходники: https://github.com/fionov-a-v/Valheim-SortingChest · [English below](#english)

Распределяющий сундук — армированный сундук, скреплённый рунами. Сложите в него добычу и закройте: руны сами
разнесут вещи по сундукам поблизости, где первым лежит такой же предмет. То, чему не нашлось места, останется внутри. Сделано для Valheim 1.0.16 на BepInEx 5 и Jötunn 2.30.
Интерфейс и описание — на русском и английском (по языку игры).

## Установка

Нужны [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) и
[Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Через r2modman/Thunderstore Mod Manager —
установить SortingChest, зависимости поставятся сами. Вручную — положить `SortingChest.dll` в `BepInEx/plugins`.

Мод нужен **всем** игрокам и серверу (у кого его нет — Jötunn не пустит на сервер). Настройки игры
(радиус, рецепт, правила) приходят с сервера, менять их может только администратор.

## Постройка

Молот → «Мебель» → **Распределяющий сундук**, рядом с верстаком (как армированный сундук).

| Стоимость | |
|---|---|
| Армированный сундук | 10 × Качественная древесина, 2 × Железо (берётся из самой игры) |
| Сверху | 1 × Трофей: Скелет (череп скелета), 2 × Ядро суртлинга, 5 × Глаз грейдворфа |

Ячеек — как у армированного сундука: 6 × 4 = 24.

Чтобы его было видно среди обычных сундуков: железо с голубым отливом, на полу под ним медленно вращается
светящийся круг из 24 рун старшего футарка (сундук — в центре круга), мерцает свет, от круга поднимаются искры.
Пока сундук ставят, на земле видно кольцо радиуса сортировки (как у охранного камня), а потом — при наведении на него.

## Как он раскладывает

1. Положите вещи в распределяющий сундук и **закройте** его.
2. Он ищет сундуки в радиусе **20 м** (настраивается) и смотрит на **первый** предмет в каждом — тот,
   что в верхней левой занятой ячейке.
3. Каждый предмет уходит в сундук, где первым лежит такой же: сначала в ближайший, когда он заполнится —
   в следующий. Круг рун раскручивается и вспыхивает, к каждому сундуку, куда что-то легло, летит огонёк (видят все игроки рядом).
4. Что не нашло сундука — остаётся внутри. В центре экрана — сколько разнесено, по скольким сундукам и сколько осталось.

Какие сундуки не трогает:

- **личные** сундуки — по умолчанию (можно включить, но и тогда только свои: чужой личный не открыть);
- сундуки под **чужим охранным камнем**, к которому у вас нет доступа;
- сундуки тележек и кораблей (можно включить);
- **хранилища машин** — печей, плавилен, бродилен, ванильного уничтожителя, в том числе из других модов
  (например, угольная печь BetterStations сжигает дерево из своего бака). Если какая-то постройка с хранилищем
  не распознаётся как машина — впишите её имя в `NeverSortInto`;
- другие распределяющие сундуки (иначе вещи ходили бы по кругу);
- пустые сундуки, сундуки подземелий и могилы.

Сундук, который в эту секунду открыт другим игроком, пропускается (об этом будет сообщение). Пока руны
работают — обычно долю секунды — распределяющий сундук нельзя открыть.

### Мультиплеер

Менять сундук может только тот, кто «владеет» им в сети. Распределяющий сундук, как и ванильное «Переместить похожие»,
просит владельца каждого нужного сундука отдать его, ждёт, пока данные сундука реально придут, перечитывает
содержимое и только тогда перекладывает вещи. Так вещи не теряются и не удваиваются, даже если рядом играют несколько человек.

## Кнопка «Загрузить ресурсы»

Справа от окна распределяющего сундука. Переносит из вашего инвентаря **все материалы**: дерево, камень, руду, слитки
металлов, шкуры, смолу, уголь, ядра, перья и т.д. (всё с типом «материал»). Еда, зелья, оружие, броня, инструменты
и надетое не трогаются. Дальше закройте распределяющий сундук — и всё разъедется по сундукам.

Можно добавить предметы (`AlsoLoad`, например трофеи), исключить (`NeverLoad`) или не трогать панель быстрого
доступа (`SkipHotbar`). Если кнопка мешает другому моду — её можно сдвинуть (`LoadButtonOffset`).

## Настройки

Файл `BepInEx/config/sortingchest.cfg` (или F1 — Configuration Manager). Правка файла во время игры применяется сразу.

| Раздел | Настройка | По умолчанию | Что делает |
|---|---|---|---|
| 1 - Sorting | `Radius` | 20 | Радиус поиска сундуков, м (1–100) |
| | `IncludePrivateChests` | false | Раскладывать и в свои личные сундуки |
| | `IncludeCartsAndShips` | false | Раскладывать и в тележки и корабли |
| | `MatchMode` | FirstItem | `FirstItem` — по первому предмету сундука; `AnyItem` — по любому предмету в сундуке |
| | `NeverSortInto` | — | Постройки, в которые не раскладывать никогда (имена префабов через запятую) |
| 2 - Load button | `LoadMaterials` | true | Кнопка берёт все материалы |
| | `AlsoLoad` | — | И эти предметы (имена префабов через запятую) |
| | `NeverLoad` | — | Никогда эти предметы |
| | `SkipHotbar` | false | Не брать из панели быстрого доступа |
| 3 - Recipe | `Enabled` | true | Можно ли строить |
| | `ExtraCost` | `TrophySkeleton:1,SurtlingCore:2,GreydwarfEye:5` | Сколько сверх армированного сундука |
| | `FullCostOverride` | — | Если задано — полная стоимость вместо «сундук + ExtraCost» |
| | `CraftingStation` | — | Станок (пусто — как у армированного сундука) |
| 4 - Client | `ShowEffects` | true | Показывать магию (круг рун, свет, искры, огоньки) |
| | `LoadButtonOffset` | 0, 0 | Сдвиг кнопки «Загрузить ресурсы», пиксели |

Разделы 1–3 синхронизируются с сервера, раздел 4 — у каждого свой.

## Сборка из исходников

Нужен .NET SDK 8. Путь к игре задан в `SortingChest.csproj` (`ValheimDir`, по умолчанию `~/ValheimWin` — ссылка
на папку игры на Windows-машине, открытую по SMB). Другой путь: `dotnet build -c Release -p:ValheimDir=/путь/к/Valheim`.

```
dotnet build -c Release
```

После сборки DLL сама копируется в `BepInEx/plugins/SortingChest.dll` (собрать без копирования:
`-p:SkipDeploy=true`). Если в этот момент игра запущена, Windows держит файл занятым — будет только предупреждение,
скопируйте после выхода из игры. Архив для Thunderstore — `./package.sh` (появится в `dist/`).

Тесты правил раскладки и загрузки (заодно выгружают PNG текстур рун в указанную папку):

```
dotnet run -c Release --project tests/LogicTests -- out
```

Иконка (`package/icon.png`, 256×256) и баннер для страницы мода (`media/banner.png`, 1300×372) собираются из этих
текстур: `python3 tests/icon.py out package/icon.png` и та же команда с `media/banner.png --banner`.

## Лицензия

[PolyForm Noncommercial License 1.0.0](LICENSE.md). Кратко (полный текст — в `LICENSE.md`):

- мод можно бесплатно использовать, изменять и распространять, в том числе переделки;
- продавать мод или использовать его в коммерческих целях нельзя;
- при любой передаче мода или его переделки нужно приложить текст лицензии (или ссылку на него) и сохранить строку:

```
Required Notice: Copyright Fionov Alexander (https://github.com/fionov-a-v/Valheim-SortingChest)
```

---

<a id="english"></a>
# Sorting Chest — mod for Valheim

The sorting chest is a reinforced chest bound with runes. Put your haul in and close the lid: the runes carry every
item into the nearby chests whose first item is the same. Whatever finds no home stays inside. Made for Valheim 1.0.16 with BepInEx 5 and Jötunn 2.30. English and Russian in-game text.

**Install:** needs BepInExPack Valheim and Jötunn. Every player and the server must have the mod; game settings are
synced from the server and only admins can change them.

**Build:** Hammer → Furniture → **Sorting Chest**, next to a workbench. Cost: the reinforced chest's cost (10 Finewood,
2 Iron — read from the game) **plus** 1 Skeleton trophy, 2 Surtling cores and 5 Greydwarf eyes. 6 × 4 = 24 slots, like the
reinforced chest. It looks enchanted: blue-tinted iron, a glowing circle of the 24 Elder Futhark runes lying on the floor
and slowly turning with the chest at its center, a soft light and rising sparks. While you place it (and when you look
at it) the sorting radius is drawn on the ground, like a ward's.

**Sorting:** when you close it, it looks for chests within **20 m** (configurable) and checks the **first** item of each
(the top-left occupied slot). Every item goes to the nearest chest whose first item is the same; when that one is full, the
rest goes to the next. Leftovers stay inside. Wisps of light fly to every chest that received something (all players nearby
see them). Skipped: personal chests (optional, and then only your own), chests under someone else's ward, carts and ships
(optional), other sorting chests, the storage of machines — kilns, smelters, fermenters, the obliterator, also from other
mods (e.g. the BetterStations charcoal kiln burns wood from its bin; add unrecognized ones to `NeverSortInto`), empty chests,
dungeon chests and tombstones. A chest another player has open right now is
skipped. In multiplayer it asks each chest's network owner for it (like vanilla "Place stacks"), waits until the chest's
data actually arrives, reloads its contents and only then moves items, so nothing is lost or duplicated.

**"Load resources" button** (to the right of the sorting chest window): moves **all materials** from your inventory — wood,
stone, ore, metal bars, hides, resin, coal, cores, feathers… (everything of the Material type). Food, potions, weapons,
armor, tools and equipped items are left alone. Then close the sorting chest and everything flies off to its chests.

**Config** (`BepInEx/config/sortingchest.cfg`, or F1 Configuration Manager): `Radius`, `IncludePrivateChests`,
`IncludeCartsAndShips`, `MatchMode` (`FirstItem` / `AnyItem`), `NeverSortInto`, `LoadMaterials`, `AlsoLoad`, `NeverLoad`, `SkipHotbar`,
`Enabled`, `ExtraCost`, `FullCostOverride`, `CraftingStation` (synced from the server); `ShowEffects`,
`LoadButtonOffset` (per client). See the table above.

**Source:** https://github.com/fionov-a-v/Valheim-SortingChest

**License:** [PolyForm Noncommercial License 1.0.0](LICENSE.md) — free to use, modify and share, no commercial use;
keep the line `Required Notice: Copyright Fionov Alexander (https://github.com/fionov-a-v/Valheim-SortingChest)` when redistributing.

## Changelog

- **1.0.0** — first release. Machines with storage (kilns, smelters, fermenters, obliterator — vanilla and from mods
  such as BetterStations) are never sorted into.
