using System.Collections;
using System.Collections.Generic;
using SortingChest.Logic;
using UnityEngine;

namespace SortingChest
{
    /// <summary>
    /// Раскладка содержимого распределяющего сундука по сундукам вокруг. Запускается у того, кто его закрыл
    /// (он — владелец его ZDO), и идёт корутиной: чужие сундуки сначала надо попросить у их владельцев.
    /// </summary>
    internal static class Sorter
    {
        /// <summary>Сколько ждать ответа владельцев и прихода данных сундуков, с.</summary>
        private const float OwnershipTimeout = 3f;

        /// <summary>Машина ли постройка — одинаково для всех построек одного префаба, поэтому запоминаем.</summary>
        private static readonly Dictionary<string, bool> s_machineByPrefab = new Dictionary<string, bool>();

        public static void Begin(Container distributor, DistributingChest chest)
        {
            if (chest.IsSorting || distributor.GetInventory() == null || distributor.GetInventory().NrOfItems() == 0)
            {
                return;
            }
            chest.StartCoroutine(Run(distributor, chest));
        }

        private static IEnumerator Run(Container distributor, DistributingChest chest)
        {
            chest.IsSorting = true;
            // Замок: пока руны работают, сундук «открыт» — его не отдадут другому игроку (RPC_RequestOpen откажет)
            // и не перезапишут из ZDO. Ставим поле напрямую: SetInUse проиграл бы звук открытия.
            ContainerAccess.InUse(distributor) = true;
            var requested = new List<Container>();
            try
            {
                chest.Surge();
                Inventory source = distributor.GetInventory();
                ZNetView distributorView = ContainerAccess.NView(distributor);
                SortSettings settings = ChestConfig.SortSettings();
                long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
                Vector3 origin = distributor.transform.position;

                var sourceKeys = new HashSet<string>();
                foreach (ItemDrop.ItemData item in source.GetAllItems())
                {
                    sourceKeys.Add(Key(item));
                }

                // 1. Подходящие сундуки, которые (по последним известным данным) принимают хоть что-то из наших вещей.
                var wanted = new List<Container>();
                foreach (Container c in ContainerAccess.Snapshot())
                {
                    if (c == distributor || !IsCandidate(c, origin, playerId, settings))
                    {
                        continue;
                    }
                    ContainerAccess.Reload(c);
                    if (SortRules.AcceptsAny(Target(c, origin, settings.Mode, 0), sourceKeys))
                    {
                        wanted.Add(c);
                    }
                }
                if (wanted.Count == 0)
                {
                    Message("$sortingchest_msg_none");
                    yield break;
                }

                // 2. Чужие сундуки просим у владельцев и ждём: и ответа, и прихода самих данных (тогда IsOwner станет true).
                foreach (Container c in wanted)
                {
                    if (!ContainerAccess.NView(c).IsOwner())
                    {
                        ContainerAccess.RequestOwnership(c, playerId);
                        requested.Add(c);
                    }
                }
                float deadline = Time.time + OwnershipTimeout;
                while (Time.time < deadline && requested.Exists(Pending))
                {
                    yield return null;
                }

                if (distributor == null || !distributorView.IsValid() || !distributorView.IsOwner())
                {
                    Message("$sortingchest_msg_lost");
                    yield break;
                }

                // 3. Свежее содержимое своих сундуков и окончательный план.
                var owned = new List<Container>();
                var targets = new List<ChestTarget>();
                int skipped = 0;
                foreach (Container c in wanted)
                {
                    ZNetView view = c != null ? ContainerAccess.NView(c) : null;
                    if (view == null || !view.IsValid() || !view.IsOwner() || c.IsInUse())
                    {
                        skipped++;
                        continue;
                    }
                    ContainerAccess.Reload(c);
                    targets.Add(Target(c, origin, settings.Mode, owned.Count));
                    owned.Add(c);
                }

                // 4. Переносим: каждый предмет — в ближайший подходящий сундук, остаток — в следующий.
                var received = new int[owned.Count];
                int moved = 0;
                foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(source.GetAllItems()))
                {
                    foreach (int index in SortRules.TargetsFor(Key(item), targets))
                    {
                        int before = item.m_stack;
                        owned[index].GetInventory().MoveItemToThis(source, item);
                        bool gone = !source.ContainsItem(item);
                        int count = gone ? before : before - item.m_stack;
                        received[index] += count;
                        moved += count;
                        if (gone)
                        {
                            break;
                        }
                    }
                }

                var destinations = new List<Vector3>();
                for (int i = 0; i < owned.Count; i++)
                {
                    if (received[i] > 0)
                    {
                        destinations.Add(owned[i].transform.position);
                    }
                }
                if (destinations.Count > 0)
                {
                    MagicFx.Broadcast(origin, destinations);
                }
                Report(moved, destinations.Count, source.NrOfItemsIncludingStacks(), skipped);
            }
            finally
            {
                foreach (Container c in requested)
                {
                    ContainerAccess.Forget(c);
                }
                if (distributor != null)
                {
                    ContainerAccess.InUse(distributor) = false;
                }
                chest.IsSorting = false;
            }
        }

        /// <summary>Ещё ждём: владелец не ответил, или ответил «да», но данные сундука ещё не пришли.</summary>
        private static bool Pending(Container c)
        {
            if (c == null)
            {
                return false;
            }
            switch (ContainerAccess.GetAnswer(c))
            {
                case ContainerAccess.Answer.Waiting:
                    return true;
                case ContainerAccess.Answer.Granted:
                    ZNetView view = ContainerAccess.NView(c);
                    return view != null && view.IsValid() && !view.IsOwner();
                default:
                    return false;
            }
        }

        private static bool IsCandidate(Container c, Vector3 origin, long playerId, SortSettings settings)
        {
            ZNetView view = ContainerAccess.NView(c);
            if (view == null || !view.IsValid() || c.GetInventory() == null)
            {
                return false;
            }
            Piece piece = c.GetComponent<Piece>();
            if (piece == null && c.m_rootObjectOverride != null)
            {
                piece = c.m_rootObjectOverride.GetComponent<Piece>();
            }
            float distance = Vector3.Distance(origin, c.transform.position);
            if (distance > settings.Radius)
            {
                return false; // дальние отсекаем сразу, не разбирая постройку
            }
            GameObject root = view.gameObject; // сама постройка (у сундука корабля — корабль), без соседних построек
            string prefab = Utils.GetPrefabName(root);
            var facts = new ChestFacts
            {
                IsSortingChest = c.GetComponent<DistributingChest>() != null,
                BuiltByPlayer = piece != null && piece.IsPlacedByPlayer(),
                IsPrivate = c.m_privacy != Container.PrivacySetting.Public,
                IsOwnPrivate = ContainerAccess.CheckAccess(c, playerId),
                IsVehicle = c.m_wagon != null || c.GetComponentInParent<Ship>() != null,
                WardBlocked = c.m_checkGuardStone && !PrivateArea.CheckAccess(c.transform.position, 0f, flash: false),
                IsMachine = IsMachine(root, prefab),
                PrefabName = prefab,
                Distance = distance,
            };
            return SortRules.IsEligible(facts, settings);
        }

        /// <summary>Хранилище машины (печь, плавильня, уничтожитель...), а не сундук: смотрим на все компоненты постройки.</summary>
        private static bool IsMachine(GameObject root, string prefab)
        {
            if (s_machineByPrefab.TryGetValue(prefab, out bool machine))
            {
                return machine;
            }
            var names = new List<string>();
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null)
                {
                    names.Add(mb.GetType().Name);
                }
            }
            machine = SortRules.LooksLikeMachine(names);
            s_machineByPrefab[prefab] = machine;
            return machine;
        }

        private static ChestTarget Target(Container c, Vector3 origin, MatchMode mode, int index)
        {
            var slots = new List<SlotItem>();
            foreach (ItemDrop.ItemData item in c.GetInventory().GetAllItems())
            {
                slots.Add(new SlotItem(Key(item), item.m_gridPos.x, item.m_gridPos.y));
            }
            return new ChestTarget
            {
                Index = index,
                Distance = Vector3.Distance(origin, c.transform.position),
                Accepts = SortRules.AcceptedKeys(slots, mode),
            };
        }

        /// <summary>Одинаковые предметы — одинаковое внутреннее имя ($item_wood...), качество не важно.</summary>
        private static string Key(ItemDrop.ItemData item) => item.m_shared.m_name;

        private static void Report(int moved, int chests, int left, int skipped)
        {
            if (moved == 0)
            {
                Message(skipped > 0 ? "$sortingchest_msg_busy" : "$sortingchest_msg_none");
                return;
            }
            string text = ChestLocalization.Format("$sortingchest_msg_sorted", moved, chests);
            if (left > 0)
            {
                text += "\n" + ChestLocalization.Format("$sortingchest_msg_left", left);
            }
            if (skipped > 0)
            {
                text += "\n" + ChestLocalization.Format("$sortingchest_msg_busy");
            }
            Message(text);
        }

        private static void Message(string text)
        {
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, ChestLocalization.Format(text));
            }
        }
    }
}
