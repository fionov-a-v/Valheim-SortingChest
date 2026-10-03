using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace SortingChest
{
    internal static class ChestLocalization
    {
        public static void Register()
        {
            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "sortingchest_piece", "Sorting Chest" },
                { "sortingchest_desc", "A reinforced chest bound with runes. Close its lid and the runes carry every item " +
                                       "into the nearby chests whose first item is the same; whatever finds no home stays inside. " +
                                       "The \"Load resources\" button moves all raw materials and metal bars from your bag into it." },
                { "sortingchest_load", "Load resources" },
                { "sortingchest_hover", "Sorts into chests within {0} m" },

                { "sortingchest_msg_sorted", "Sorting chest: {0} items carried into {1} chests" },
                { "sortingchest_msg_left", "Left inside: {0} — no chest takes them" },
                { "sortingchest_msg_none", "Sorting chest: no chest nearby takes these items" },
                { "sortingchest_msg_busy", "Some chests are in use — they were skipped" },
                { "sortingchest_msg_working", "The runes are still at work…" },
                { "sortingchest_msg_lost", "Sorting chest: someone else took the chest — sorting stopped" },
                { "sortingchest_msg_loaded", "Loaded: {0}" },
                { "sortingchest_msg_nothing", "Nothing to load" },
                { "sortingchest_msg_full", "The sorting chest is full" },
            });

            loc.AddTranslation("Russian", new Dictionary<string, string>
            {
                { "sortingchest_piece", "Распределяющий сундук" },
                { "sortingchest_desc", "Армированный сундук, скреплённый рунами. Закройте крышку, и руны разнесут вещи " +
                                       "по сундукам поблизости, где первым лежит такой же предмет; то, чему не нашлось места, останется внутри. " +
                                       "Кнопка «Загрузить ресурсы» перекладывает в него из сумки всё добытое сырьё и слитки металлов." },
                { "sortingchest_load", "Загрузить ресурсы" },
                { "sortingchest_hover", "Раскладывает по сундукам в радиусе {0} м" },

                { "sortingchest_msg_sorted", "Распределяющий сундук: разнесено предметов — {0}, сундуков — {1}" },
                { "sortingchest_msg_left", "Осталось внутри: {0} — для них нет сундука" },
                { "sortingchest_msg_none", "Распределяющий сундук: ни один сундук поблизости не принимает эти вещи" },
                { "sortingchest_msg_busy", "Часть сундуков сейчас открыта — их пропустили" },
                { "sortingchest_msg_working", "Руны ещё работают…" },
                { "sortingchest_msg_lost", "Распределяющий сундук перехватил другой игрок — сортировка прервана" },
                { "sortingchest_msg_loaded", "Загружено: {0}" },
                { "sortingchest_msg_nothing", "Нечего загружать" },
                { "sortingchest_msg_full", "Распределяющий сундук полон" },
            });
        }

        /// <summary>Перевести токен ($sortingchest_...) и подставить числа.</summary>
        public static string Format(string token, params object[] args)
        {
            string text = Localization.instance != null ? Localization.instance.Localize(token) : token;
            return args.Length == 0 ? text : string.Format(text, args);
        }
    }
}
