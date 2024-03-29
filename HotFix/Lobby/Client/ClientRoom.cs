﻿using System.Collections.Generic;
using UnityEngine;
using kcp2k.Examples;

namespace HotFix
{
    public class ClientRoom : BaseRoom
    {
        public ClientRoom(BaseRoomData data) : base(data)
        {
            m_PlayerDic = new Dictionary<int, BasePlayer>();
            for (int i = 0; i < data.Players.Count; i++)
            {
                var playerData = data.Players[i];
                var clientPlayer = new ClientPlayer(playerData);
                m_PlayerDic.Add(playerData.SeatId, clientPlayer);
                clientPlayer.SetRoomID(RoomID).SetSeatID(i).SetStatus(PlayerStatus.ROOM);
            }
        }

        // 人员进出，更新房间信息
        public void UpdateData(BaseRoomData data)
        {
            for (int i = 0; i < data.Players.Count; i++)
            {
                var playerData = data.Players[i];
                var clientPlayer = new ClientPlayer(playerData);
                m_PlayerDic[playerData.SeatId] = clientPlayer;
            }
        }

        /// <summary>
        /// 游戏逻辑
        /// </summary>

        public static CardLib lib;

        // 查询某乌龟当前所在格子
        //key:乌龟颜色, value:格子ID
        Dictionary<TurtleColor, int> TurtlePos;

        // 记录每个格子中的乌龟，及顺序（从下到上）
        //key:格子ID, value:乌龟颜色及顺序
        public Dictionary<int, List<TurtleColor>> GridData;

        public TurtleColor myTurtleColor; //我的颜色
        public List<Card> HandCardDatas; //key:顺序, value:我的手牌（数据）
        public int NextTurn; //下回出牌的座位号
        public TurtleAnime gameStatus { get; protected set; } //流程控制
        public void SetStatus(TurtleAnime state)
        {
            if (gameStatus == TurtleAnime.End)
            {
                Debug.LogError("结束了，无法再设置");
            }
            //Debug.Log($"<color=yellow>{gameStatus}==========>>{state}</color>");
            gameStatus = state;
        }

        // 初始化
        private void Init()
        {
            lib = new CardLib();

            TurtlePos = new Dictionary<TurtleColor, int>
            {
                { TurtleColor.RED, 0 },
                { TurtleColor.YELLOW, 0 },
                { TurtleColor.GREEN, 0 },
                { TurtleColor.BLUE, 0 },
                { TurtleColor.PURPLE, 0 }
            };

            GridData = new Dictionary<int, List<TurtleColor>>();
            GridData.Add(0, new List<TurtleColor> { (TurtleColor)0, (TurtleColor)1, (TurtleColor)2, (TurtleColor)3, (TurtleColor)4 });
            for (int i = 1; i < 10; i++)
            {
                GridData.Add(i, new List<TurtleColor>());
            }

            myTurtleColor = TurtleColor.NONE; //空，等待指定
            HandCardDatas = new List<Card>(); //空，等待发牌
            NextTurn = 0; //从房主开始
            SetStatus(TurtleAnime.Wait); //初始化
        }
        // 在房间等待，收到消息，跳转比赛
        public void OnGameStart_Client(S2C_GameStartPacket packet)
        {
            this.Init();

            this.myTurtleColor = (TurtleColor)packet.Color;
            for (int i = 0; i < packet.Cards.Count; i++)
            {
                int cardid = packet.Cards[i];
                Card card = lib.library[cardid];
                this.HandCardDatas.Add(card);
            }
            this.NextTurn = 0;
        }
        // 某人出牌，推进逻辑
        // 输入卡牌(packet)，返回要移动的棋子组(List<int>)
        public List<int> OnGamePlay_Client(S2C_PlayCardPacket packet)
        {
            List<int> moveChessList = new List<int>();

            // 解析牌型
            TurtleColor colorId = (TurtleColor)packet.Color;
            Card card = lib.library[packet.CardID];
            bool colorful = card.cardColor == CardColor.COLOR || card.cardColor == CardColor.SLOWEST;
            TurtleColor colorKey = colorful ? colorId : (TurtleColor)card.cardColor; //哪只乌龟
            int step = (int)card.cardNum; //走几步

            // 如果是自己出的，移除手牌
            if (packet.SeatID == KcpChatClient.m_PlayerManager.LocalPlayer.SeatId)
            {
                //PrintHandCards();
                HandCardDatas.Remove(card);
                //PrintHandCards();
            }

            // 走棋子
            int curPos = TurtlePos[colorKey]; //某颜色棋子当前位置
            int dstPos = Mathf.Clamp(curPos + step, 0, 9); //前往位置
            if (curPos > 0) //非起点
            {
                // 考虑叠起来的情况。
                List<TurtleColor> temp = new List<TurtleColor>();
                List<TurtleColor> curGrid = GridData[curPos];
                Debug.Log($"移动棋子{colorKey}，格子{curPos}上叠了{curGrid.Count}层");

                int index = curGrid.IndexOf(colorKey);
                Debug.Log($"目标棋子{colorKey}在格子{curPos}的第{index}层");

                for (int i = 0; i < curGrid.Count; i++)
                {
                    TurtleColor chess = curGrid[i];
                    //Debug.Log($"{i}---格子{curPos}上，第{i}层是{chess} / {curGrid.Count}");

                    if (i >= index)
                    {
                        TurtlePos[chess] = dstPos;

                        //GridData[curPos].Remove(chess); //遍历中不能移除
                        temp.Add(chess);
                        GridData[dstPos].Add(chess);

                        moveChessList.Add((int)chess);
                        Debug.Log($"<color=white>移动棋子：格子{curPos}，第{i}层{chess}</color>");
                    }
                }
                for (int i = 0; i < temp.Count; i++)
                {
                    TurtleColor chess = temp[i];
                    curGrid.Remove(chess);
                    //Debug.Log($"---------从格子{curPos}移除{chess}");
                }
            }
            else //起点
            {
                TurtlePos[colorKey] = dstPos; //起点不堆叠

                //Debug.Log($"起点不堆叠，从{curPos}移除{colorKey}");
                GridData[curPos].Remove(colorKey);
                //Debug.Log($"把{colorKey}添加到{dstPos}");
                GridData[dstPos].Add(colorKey);

                moveChessList.Add((int)colorKey);
                //Debug.Log($"<color=white>移动棋子：{colorKey}。" +
                //    $"\n移动后，上个格子[{curPos}]{GridData[curPos].Count}层。" +
                //    $"这个格子[{dstPos}]{GridData[dstPos].Count}层。</color>");
            }

            return moveChessList;
        }
        // 自己出牌后，收到新的手牌
        public void OnGameDeal_Client(Card card)
        {
            HandCardDatas.Add(card);
            //PrintHandCards();
        }
        // 结算
        public void OnGameResult_Client()
        {
            SetStatus(TurtleAnime.End); //只操作对自身变量有影响的，其他操作归UI
        }
        // 获取最慢的牌
        public List<TurtleColor> GetSlowest()
        {
            for (int i = 0; i < GridData.Count; i++)
            {
                var grid = GridData[i]; //从起点开始找
                if (grid.Count > 0) //格子里有就返回
                    return grid;
            }
            return null;
        }
        // 获取排名
        public List<int> GetRank()
        {
            var turtles = new List<int>();
            for (int i = GridData.Count - 1; i >= 0; i--) //从终点开始遍历
            {
                var grid = GridData[i];
                for (int t = 0; t < grid.Count; t++)
                {
                    int chess = (int)grid[t];
                    turtles.Add(chess);
                }
            }
            return turtles;
        }

        void PrintHandCards()
        {
            string handStr = $"{HandCardDatas.Count}张：";
            for (int i = 0; i < HandCardDatas.Count; i++)
            {
                var hand_card = HandCardDatas[i];
                if (hand_card != null)
                {
                    handStr += $"{i}--[{hand_card.id}]、";
                }
                else
                {
                    handStr += $"{i}--NULL、";
                }
            }
            Debug.Log(handStr);
        }
    }
}