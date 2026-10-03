using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SortingChest
{
    /// <summary>
    /// Все сундуки мира рядом с игроком и безопасная передача владения ими.
    /// Менять содержимое сундука может только владелец его ZDO, поэтому перед переноской вещей распределяющий сундук просит
    /// владельца отдать сундук — так же, как это делает ванильное «Переместить похожие» (Place stacks, RPC_RequestStack): владелец
    /// проверяет, что сундук не открыт и доступен, отдаёт владение и отвечает. Ответ приходит раньше самих данных ZDO,
    /// поэтому сортировщик дополнительно ждёт, пока сундук станет «своим» и у него.
    /// </summary>
    internal static class ContainerAccess
    {
        private const string RpcRequest = "SortingChest_RequestOwnership";
        private const string RpcResponse = "SortingChest_OwnershipResponse";

        public enum Answer
        {
            Waiting,
            Granted,
            Denied,
        }

        public static readonly AccessTools.FieldRef<Container, bool> InUse = AccessTools.FieldRefAccess<Container, bool>("m_inUse");
        public static readonly AccessTools.FieldRef<Container, ZNetView> NView = AccessTools.FieldRefAccess<Container, ZNetView>("m_nview");

        private static readonly Func<Container, bool> s_load =
            AccessTools.MethodDelegate<Func<Container, bool>>(AccessTools.Method(typeof(Container), "Load"));

        private static readonly List<Container> s_all = new List<Container>();
        private static readonly Dictionary<Container, Answer> s_answers = new Dictionary<Container, Answer>();

        /// <summary>Из Container.Awake: запомнить сундук и научить его отвечать на просьбу распределяющего сундука.</summary>
        public static void OnAwake(Container container)
        {
            ZNetView nview = NView(container);
            if (nview == null || nview.GetZDO() == null)
            {
                return; // призрак при строительстве, превью иконки и т.п.
            }
            s_all.Add(container);
            try
            {
                nview.Register<long>(RpcRequest, (sender, playerId) => RPC_Request(container, nview, sender, playerId));
                nview.Register<bool>(RpcResponse, (_, granted) => RPC_Response(container, granted));
            }
            catch (ArgumentException)
            {
                // Второй сундук на том же сетевом объекте (бывает у модов кораблей): RPC уже есть у первого.
                // Такой сундук просто не отдадут — у сортировщика сработает тайм-аут, и его пропустят.
            }
        }

        /// <summary>Живые сундуки (уничтоженные выбрасываются из списка).</summary>
        public static List<Container> Snapshot()
        {
            s_all.RemoveAll(c => c == null);
            return new List<Container>(s_all);
        }

        /// <summary>Перечитать содержимое из ZDO (ванильная проверка раз в секунду может ещё не успеть).</summary>
        public static void Reload(Container container)
        {
            if (!InUse(container))
            {
                s_load(container);
            }
        }

        public static void RequestOwnership(Container container, long playerId)
        {
            s_answers[container] = Answer.Waiting;
            NView(container).InvokeRPC(RpcRequest, playerId);
        }

        public static Answer GetAnswer(Container container) =>
            s_answers.TryGetValue(container, out Answer a) ? a : Answer.Waiting;

        public static void Forget(Container container) => s_answers.Remove(container);

        /// <summary>Те же права, что у ванильного сундука: общий — всем, личный — только создателю.</summary>
        public static bool CheckAccess(Container container, long playerId)
        {
            switch (container.m_privacy)
            {
                case Container.PrivacySetting.Public:
                    return true;
                case Container.PrivacySetting.Private:
                    Piece piece = container.GetComponent<Piece>();
                    return piece != null && piece.GetCreator() == playerId;
                default:
                    return false;
            }
        }

        private static void RPC_Request(Container container, ZNetView nview, long sender, long playerId)
        {
            if (container == null || !nview.IsValid() || !nview.IsOwner())
            {
                return; // не владелец — молчим, у просящего сработает тайм-аут
            }
            bool inUse = container.IsInUse() || (container.m_wagon != null && container.m_wagon.InUse());
            if ((inUse && sender != ZNet.GetUID()) || !CheckAccess(container, playerId))
            {
                nview.InvokeRPC(sender, RpcResponse, false);
                return;
            }
            ZDOMan.instance.ForceSendZDO(sender, nview.GetZDO().m_uid);
            nview.GetZDO().SetOwner(sender);
            nview.InvokeRPC(sender, RpcResponse, true);
        }

        private static void RPC_Response(Container container, bool granted)
        {
            if (container != null && s_answers.ContainsKey(container))
            {
                s_answers[container] = granted ? Answer.Granted : Answer.Denied;
            }
        }
    }
}
