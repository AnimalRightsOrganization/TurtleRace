﻿using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;
using HotFix;
using ET;

namespace NetCoreServer
{
    /* 远程房间 */
    public class ServerRoom : BaseRoom
    {
        public ServerRoom(ServerPlayer host, BaseRoomData data) : base(data)
        {
            // 被override的才需要在这里赋值
            m_PlayerList = new Dictionary<int, BasePlayer>();
            m_PlayerList.Add(0, host); //房主座位号0
            host.SetRoomID(data.RoomID)
                .SetSeatID(0)
                .SetStatus(PlayerStatus.ROOM);
        }

        public override Dictionary<int, BasePlayer> m_PlayerList { get; protected set; }
        public int CurCount => m_PlayerList.Count;

        public bool AddPlayer(BasePlayer p, int seatId = -1)
        {
            if (ContainsPlayer(p))
            {
                Debug.Print("严重的错误，重复加入");
                return false;
            }

            int SeatID = 0;
            if (seatId == -1)
                SeatID = GetAvailableRoomID(); //真实玩家，没有赋值，自动选取空座位
            else
                SeatID = seatId; //机器人是指定的座位

            m_PlayerList.Add(SeatID, p);
            p.SetRoomID(RoomID)
                .SetSeatID(SeatID)
                .SetStatus(PlayerStatus.ROOM);

            return true;
        }
        public bool RemovePlayer(BasePlayer p)
        {
            if (ContainsPlayer(p))
            {
                m_PlayerList.Remove(p.SeatId);
                p.ResetToLobby();
                return true;
            }
            Debug.Print("严重的错误，无法移除房间");
            return false;
        }
        public bool RemoveAll()
        {
            foreach (var p in m_PlayerList)
            {
                //ServerPlayer serverPlayer = p.Value as ServerPlayer;
                RemovePlayer(p.Value);
            }
            return false;
        }
        public bool ContainsPlayer(BasePlayer p)
        {
            BasePlayer basePlayer = null;
            if (m_PlayerList.TryGetValue(p.SeatId, out basePlayer))
            {
                if (p.PeerId != basePlayer.PeerId)
                {
                    Debug.Print("严重的错误，找错人了");
                    return false;
                }
                return true;
            }
            return false;
        }
        public ServerPlayer GetPlayer(int seatId)
        {
            BasePlayer basePlayer = null;
            if (m_PlayerList.TryGetValue(seatId, out basePlayer))
            {
                return (ServerPlayer)basePlayer;
            }
            return null;
        }

        // 检查空座位
        public bool IsAvailableSeat(int seatId)
        {
            foreach (var p in m_PlayerList)
            {
                if (p.Key == seatId)
                    return false;
            }
            return true;
        }
        const int MIN_INDEX = 0;
        const int MAX_INDEX = 4;
        int GetAvailableRoomID()
        {
            int id = MIN_INDEX;

            if (m_PlayerList.Count == 0)
                return id;

            for (int i = MIN_INDEX; i <= MAX_INDEX; i++)
            {
                BasePlayer player = null;
                if (m_PlayerList.TryGetValue(i, out player) == false)
                {
                    id = i;
                    break;
                }
            }
            return id;
        }

        public void SendAsync(PacketType msgId, object cmd)
        {
            foreach(var p in m_PlayerList)
            {
                ServerPlayer serverPlayer = p.Value as ServerPlayer;
                serverPlayer.SendAsync(msgId, cmd);
            }
        }

        #region 游戏逻辑
        #endregion

        // 所有牌
        public static CardLib lib;
        List<Card> cardList;
        // 下一张下发的牌
        int nextIndex = 0;
        // 保存乌龟棋子位置
        public Dictionary<ChessColor, int> runnerPos; //长度永远是5

        void Init()
        {
            lib = new CardLib();
            runnerPos = new Dictionary<ChessColor, int>();
            runnerPos.Add(ChessColor.RED, 0);
            runnerPos.Add(ChessColor.YELLOW, 0);
            runnerPos.Add(ChessColor.GREEN, 0);
            runnerPos.Add(ChessColor.BLUE, 0);
            runnerPos.Add(ChessColor.PURPLE, 0);
            //chessColor = ChessColor.NONE; //空，等待指定
            //handCards = new List<Card>(); //空，等待发牌
        }

        // 开局消息
        public void OnGameStart_Server()
        {
            this.Init();

            // 洗牌
            cardList = lib.Clone().library;
            GameLogic.Shuffle(cardList);

            // 准备颜色随机数
            var colors = GameLogic.AllotColor();
            //for (int i = 0; i < colors.Length; i++)
            //{
            //    Debug.Print($"打印随机数：{i}---{colors[i]}");
            //}
            // 遍历分配颜色
            for (int i = 0; i < CurCount; i++)
            {
                var player = (ServerPlayer)m_PlayerList[i];
                player.Init();
                player.chessColor = (ChessColor)colors[i];
            }
            // 遍历分配手牌（发5张）
            for (int n = 0; n < 5; n++)
            {
                for (int i = 0; i < CurCount; i++)
                {
                    var card = cardList[nextIndex];
                    var player = (ServerPlayer)m_PlayerList[i];
                    player.handCards.Add(card);
                    nextIndex++;

                    // 牌发完了，洗牌
                    if (nextIndex >= cardList.Count)
                    {
                        nextIndex = 0;
                        GameLogic.Shuffle(cardList);
                    }
                }
            }

            // 发送每个客户端的数据不一样
            for (int i = 0; i < CurCount; i++)
            {
                var player = (ServerPlayer)m_PlayerList[i];
                S2C_GameStartPacket packet = new S2C_GameStartPacket
                {
                    Color = (int)player.chessColor,
                    Cards = CardToInt(player.handCards),
                };
                player.SendAsync(PacketType.S2C_GameStart, packet);
            }
        }
        static List<int> CardToInt(List<Card> cards)
        {
            var list = new List<int>();
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                list.Add(card.id);
            }
            return list;
        }
    }
}