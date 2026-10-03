using System.Collections.Generic;
using System.Reflection;
using SortingChest.Logic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SortingChest
{
    /// <summary>
    /// Кнопка «Загрузить ресурсы» в окне распределяющего сундука: переносит из инвентаря игрока все материалы
    /// (дерево, камень, руду, слитки...). Это копия ванильной кнопки «Переместить похожие» (Place stacks) — выглядит так же;
    /// стоит справа от окна сундука на высоте его заголовка (в заголовке места под третью кнопку нет).
    /// </summary>
    internal static class LoadButton
    {
        private const float Gap = 12f;
        private const float Width = 170f;

        private static readonly AccessTools.FieldRef<InventoryGui, Container> s_current =
            AccessTools.FieldRefAccess<InventoryGui, Container>("m_currentContainer");

        private static readonly MethodInfo s_setupDragItem = AccessTools.Method(typeof(InventoryGui), "SetupDragItem");

        private static Button s_button;
        private static TMP_Text s_label;
        private static RectTransform s_rect;
        private static Vector2 s_basePosition;

        /// <summary>Из InventoryGui.Update: показать кнопку, только когда открыт распределяющий сундук.</summary>
        public static void Update(InventoryGui gui)
        {
            Container container = s_current(gui);
            DistributingChest chest = container != null ? container.GetComponent<DistributingChest>() : null;
            bool show = chest != null && container.IsOwner() && gui.m_container != null && gui.m_container.gameObject.activeInHierarchy;
            if (show && s_button == null)
            {
                Create(gui);
            }
            if (s_button == null)
            {
                return;
            }
            if (s_button.gameObject.activeSelf != show)
            {
                s_button.gameObject.SetActive(show);
            }
            if (show)
            {
                s_label.text = ChestLocalization.Format("$sortingchest_load");
                s_rect.anchoredPosition = s_basePosition + ChestConfig.LoadButtonOffset.Value;
                s_button.interactable = !chest.IsSorting;
            }
        }

        private static void Create(InventoryGui gui)
        {
            GameObject source = gui.m_stackAllButton.gameObject;
            GameObject go = Object.Instantiate(source, source.transform.parent, false);
            go.name = "SortingChestLoadButton";

            // Без привязки к кнопке геймпада (иначе она срабатывала бы вместе с «Переместить похожие»)
            // и без автоперевода текста: подпись ставим сами.
            foreach (UIGamePad pad in go.GetComponentsInChildren<UIGamePad>(true))
            {
                if (pad.m_hint != null)
                {
                    Object.DestroyImmediate(pad.m_hint);
                }
                Object.DestroyImmediate(pad);
            }
            foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && mb.GetType().Name == "Localize")
                {
                    Object.DestroyImmediate(mb);
                }
            }
            var hints = new List<GameObject>();
            foreach (Transform child in go.transform)
            {
                if (child.name.StartsWith("gamepad_hint", System.StringComparison.Ordinal))
                {
                    hints.Add(child.gameObject);
                }
            }
            hints.ForEach(Object.DestroyImmediate);

            s_button = go.GetComponent<Button>();
            s_button.onClick = new Button.ButtonClickedEvent();
            s_button.onClick.AddListener(OnClick);
            s_label = go.GetComponentInChildren<TMP_Text>(true);
            if (s_label != null)
            {
                s_label.enableAutoSizing = true;
                s_label.fontSizeMax = s_label.fontSize;
                s_label.fontSizeMin = Mathf.Min(12f, s_label.fontSize);
            }

            // Справа от окна сундука, на высоте «Взять всё»: от центра окна — половина его ширины, зазор и полкнопки.
            RectTransform panel = gui.m_container;
            var takeAll = (RectTransform)gui.m_takeAllButton.transform;
            s_rect = (RectTransform)go.transform;
            s_rect.anchorMin = s_rect.anchorMax = s_rect.pivot = new Vector2(0.5f, 0.5f);
            s_rect.sizeDelta = new Vector2(Width, s_rect.sizeDelta.y);
            float y = takeAll.anchorMin == takeAll.anchorMax && takeAll.anchorMin == new Vector2(0.5f, 0.5f)
                ? takeAll.anchoredPosition.y
                : panel.rect.height * 0.5f - s_rect.sizeDelta.y * 0.5f;
            s_basePosition = new Vector2(panel.rect.width * 0.5f + Gap + Width * 0.5f, y);
            s_rect.anchoredPosition = s_basePosition;
            go.SetActive(false);
        }

        private static void OnClick()
        {
            InventoryGui gui = InventoryGui.instance;
            Player player = Player.m_localPlayer;
            Container container = gui != null ? s_current(gui) : null;
            if (player == null || container == null || !container.IsOwner() || player.IsTeleporting())
            {
                return;
            }
            DistributingChest chest = container.GetComponent<DistributingChest>();
            if (chest == null || chest.IsSorting)
            {
                return;
            }
            s_setupDragItem?.Invoke(gui, new object[] { null, null, 1 });

            Inventory from = player.GetInventory();
            Inventory to = container.GetInventory();
            LoadSettings settings = ChestConfig.LoadSettings();
            var items = new List<ItemDrop.ItemData>(from.GetAllItems());
            items.Sort((a, b) => a.m_gridPos.y != b.m_gridPos.y ? a.m_gridPos.y.CompareTo(b.m_gridPos.y) : a.m_gridPos.x.CompareTo(b.m_gridPos.x));

            int moved = 0;
            bool full = false;
            foreach (ItemDrop.ItemData item in items)
            {
                string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
                bool material = item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material;
                bool equipped = item.m_equipped || player.IsItemEquiped(item);
                if (!SortRules.ShouldLoad(prefab, material, equipped, item.m_gridPos.y, settings))
                {
                    continue;
                }
                int before = item.m_stack;
                to.MoveItemToThis(from, item);
                bool gone = !from.ContainsItem(item);
                moved += gone ? before : before - item.m_stack;
                full |= !gone;
            }

            string text;
            if (moved > 0)
            {
                text = ChestLocalization.Format("$sortingchest_msg_loaded", moved);
                if (full)
                {
                    text += "\n" + ChestLocalization.Format("$sortingchest_msg_full");
                }
                gui.m_moveItemEffects.Create(container.transform.position, Quaternion.identity);
            }
            else
            {
                text = ChestLocalization.Format(full ? "$sortingchest_msg_full" : "$sortingchest_msg_nothing");
            }
            player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
