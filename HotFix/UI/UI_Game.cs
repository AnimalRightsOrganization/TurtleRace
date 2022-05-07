﻿using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ET;

namespace HotFix
{
    public class UI_Game : UIBase
    {
        #region 界面组件
        public Button m_CloseBtn;
        public Transform[] m_MapPoints; //地图

        public RectTransform NextIcon;
        public Image[] m_Seats; //所有玩家座位
        public Text[] m_SeatNames; //所有玩家名字
        public Dictionary<string, Sprite> idSprites; //身份图集
        public Image m_SelfIdentify; //本人身份色卡

        public int handIndex; //选中手牌，数组Id
        public Item_Card otherCard; //动画牌（别人出牌、我收到的新牌）
        public List<Item_Card> myCards; //手牌
        public Item_Chess[] gameChess; //棋子（按颜色索引存、取）

        public int selectedCardId; //选中手牌，出列
        public GameObject m_PlayPanel; //选中手牌，出牌或选颜色
        public Button m_CancelBtn; //点击背景，取消选中
        public Button m_PlayBtn; //出牌
        System.Action CancelAction;
        System.Action PlayAction;
        System.Action GameEndAction;
        public int selectedCardColor; //彩色龟，所选颜色
        public GameObject m_ColorPanel; //彩色龟，选颜色面板
        public Button[] m_ColorBtns; //彩色龟，选颜色按钮
        public RectTransform m_ColorSelected; //彩色龟，选中框

        public ClientRoom m_Room; //游戏房间（逻辑）
        private ClientPlayer m_LocalPlayer; //缓存数据，方便读取
        public bool IsMyTurn
        {
            get { return m_Room.NextTurn == m_LocalPlayer.SeatId; }
        }
        #endregion

        #region 内置方法
        void Awake()
        {
            m_CloseBtn = transform.Find("CloseBtn").GetComponent<Button>();
            m_CloseBtn.onClick.AddListener(OnCloseBtnClick);

            // 地图
            var mapRoot = transform.Find("MapPoints");
            m_MapPoints = new Transform[mapRoot.childCount];
            for (int i = 0; i < mapRoot.childCount; i++)
            {
                m_MapPoints[i] = mapRoot.GetChild(i);
            }

            // 成员
            NextIcon = transform.Find("NextIcon").GetComponent<RectTransform>();
            var SeatPanel = transform.Find("SeatPanel");
            int length = SeatPanel.childCount;
            m_Seats = new Image[length];
            m_SeatNames = new Text[length];
            for (int i = 0; i < length; i++)
            {
                var child = SeatPanel.GetChild(i);
                m_Seats[i] = child.GetComponent<Image>();
                m_SeatNames[i] = child.GetComponentInChildren<Text>();
            }
            // 身份色卡
            idSprites = ResManager.LoadSprite("Sprites/identify");
            m_SelfIdentify = transform.Find("Identify").GetComponent<Image>();

            // 动画牌
            var cardPrefab = ResManager.LoadPrefab("Prefabs/Card");
            var otherHandObj = Instantiate(cardPrefab, transform);
            otherHandObj.name = "OtherCard";
            otherCard = otherHandObj.AddComponent<Item_Card>(); //屏幕中央
            otherCard.m_Rect.anchoredPosition3D = new Vector3(-320, 555, 0);
            otherCard.UnBind(); //设为不可交互
            // 手牌
            handIndex = 0;
            var handCardPanel = transform.Find("HandCardPanel");
            myCards = new List<Item_Card>();
            for (int i = 0; i < 5; i++)
            {
                var handObj = Instantiate(cardPrefab, handCardPanel);
                handObj.name = $"HandCard_{i}";
                var handScript = handObj.AddComponent<Item_Card>();
                myCards.Add(handScript);
            }
            // 棋子
            gameChess = new Item_Chess[5];
            var chessPrefab = ResManager.LoadPrefab("Prefabs/Chess");
            for (int i = 0; i < 5; i++)
            {
                var chessObj = Instantiate(chessPrefab, m_MapPoints[0]);
                chessObj.name = $"Chess_{(ChessColor)i}";
                var chessScript = chessObj.AddComponent<Item_Chess>();
                gameChess[i] = chessScript;
                chessScript.InitData(i);
            }

            // 出牌面板
            selectedCardId = 0;
            CancelAction = null;
            PlayAction = null;
            GameEndAction = null;
            m_PlayPanel = transform.Find("PlayPanel").gameObject;
            m_PlayPanel.SetActive(false);
            m_CancelBtn = m_PlayPanel.GetComponent<Button>();
            m_PlayBtn = transform.Find("PlayPanel/PlayBtn").GetComponent<Button>();
            m_CancelBtn.onClick.AddListener(HidePlayPanel);
            m_PlayBtn.onClick.AddListener(OnPlayBtnClick);
            // 彩色选项
            selectedCardColor = 0;
            m_ColorPanel = transform.Find("PlayPanel/ColorPanel").gameObject;
            m_ColorPanel.SetActive(false);
            m_ColorBtns = transform.Find("PlayPanel/ColorPanel").GetComponentsInChildren<Button>();
            m_ColorSelected = transform.Find("PlayPanel/Selected").GetComponent<RectTransform>();
            for (int i = 0; i < m_ColorBtns.Length; i++)
            {
                int index = i;
                Button btn = m_ColorBtns[index];
                btn.onClick.AddListener(() =>
                {
                    selectedCardColor = index;
                    Debug.Log($"彩色龟，选颜色{(ChessColor)selectedCardColor}");
                    m_ColorSelected.SetParent(btn.transform);
                    m_ColorSelected.anchoredPosition = Vector2.zero;
                });
            }
        }
        void Start()
        {
            NetPacketManager.RegisterEvent(OnNetCallback);
        }
        void OnDestroy()
        {
            NetPacketManager.UnRegisterEvent(OnNetCallback);

            NextIcon.SetParent(m_SeatNames[0].transform);
            NextIcon.anchoredPosition = Vector3.zero;
        }
        #endregion

        #region 按钮事件
        public void UpdateUI()
        {
            GameEndAction = null;
            m_Room = TcpChatClient.m_ClientRoom;
            //m_Room.PrintRoom(); //打印本人手牌
            m_LocalPlayer = TcpChatClient.m_PlayerManager.LocalPlayer;

            // 绘制成员头像、昵称
            var players = m_Room.Players;
            for (int i = 0; i < m_Seats.Length; i++)
            {
                int index = i;
                var seat = m_Seats[index];
                var seatName = m_SeatNames[index];
                if (index >= players.Count)
                {
                    seat.gameObject.SetActive(false);
                }
                else
                {
                    seat.gameObject.SetActive(true);
                    var player = players[index];
                    seatName.text = player.NickName;
                }
            }

            // 绘制本人身份色卡(红0,黄1,绿2,蓝3,紫4)
            int colorId = (int)m_Room.chessColor;
            m_SelfIdentify.sprite = idSprites[$"identify_{colorId}"];
            //Debug.Log($"本人颜色={m_Room.chessColor}");

            // 绘制手牌
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                var card = m_Room.handCards[i];
                myCards[i].InitData(card);
                myCards[i].Index = index;
            }
        }
        void OnCloseBtnClick()
        {
            Debug.Log("退出游戏");
            TcpChatClient.SendLeaveRoom();
        }
        public void ShowPlayPanel(int carcdid, System.Action noAction, System.Action yesAction)
        {
            selectedCardColor = -1;

            //Debug.Log($"ShowPlayPanel: id={carcdid}");
            selectedCardId = carcdid;
            CancelAction = noAction;
            PlayAction = yesAction;
            m_PlayPanel.SetActive(true);
            Card card = ClientRoom.lib.library[selectedCardId];
            //Debug.Log($"显示：{card.Log()}");
            if (card.cardColor == CardColor.COLOR)
            {
                Debug.Log($"显示颜色选择");
                m_ColorPanel.SetActive(true);
            }
            else if (card.cardColor == CardColor.SLOWEST)
            {
                Debug.Log($"显示最慢选择");
                List<ChessColor> slowestArray = m_Room.GetSlowest(); //0~4
                for (int i = 0; i < m_ColorBtns.Length; i++)
                {
                    var btn = m_ColorBtns[i];
                    bool available = slowestArray.Contains((ChessColor)i);
                    btn.gameObject.SetActive(available);
                    if (selectedCardColor == -1 && available)
                    {
                        selectedCardColor = i;
                        m_ColorSelected.SetParent(btn.transform);
                        m_ColorSelected.anchoredPosition = Vector2.zero;
                    }
                }
                m_ColorPanel.SetActive(true);
            }
            else
            {
                m_ColorPanel.SetActive(false);
            }
        }
        public void HidePlayPanel()
        {
            CancelAction?.Invoke();
            m_PlayPanel.SetActive(false);
        }
        void OnPlayBtnClick()
        {
            Card card = ClientRoom.lib.library[selectedCardId];
            Debug.Log($"点击出牌：{card.Log()}");
            TcpChatClient.SendGamePlay(card.id, selectedCardColor);
        }
        #endregion

        #region 网络事件
        void OnNetCallback(PacketType type, object reader)
        {
            switch (type)
            {
                case PacketType.S2C_YourTurn:
                    OnYourTurn(reader);
                    break;
                case PacketType.S2C_GamePlay:
                    OnPlay(reader);
                    break;
                case PacketType.S2C_GameDeal:
                    OnDeal(reader);
                    break;
                case PacketType.S2C_GameResult:
                    OnGameResult(reader);
                    break;
            }
        }
        // 提示出牌
        void OnYourTurn(object reader)
        {
            var packet = (S2C_NextTurnPacket)reader;
            Debug.Log($"[S2C_YourTurn] 下轮出牌的是{packet.SeatID}");
            NextIcon.SetParent(m_SeatNames[packet.SeatID].transform);
            NextIcon.anchoredPosition = Vector3.zero;
        }
        // 出牌消息
        async void OnPlay(object reader)
        {
            var packet = (S2C_PlayCardPacket)reader;
            Debug.Log($"[S2C] 收到出牌，座位#{packet.SeatID}出牌{packet.CardID}-{packet.Color}，是第{handIndex}张手牌");

            // ①解析牌型
            int colorId = packet.Color;
            Card card = ClientRoom.lib.library[packet.CardID];
            //Debug.Log(card.Log());
            var moveChessList = m_Room.OnGamePlay_Client(packet);

            // ②出牌动画
            // 放大0.2f，移动0.3f，停留1.0f，消失0.5f => 2.0f
            if (packet.SeatID != m_LocalPlayer.SeatId)
            {
                //Debug.Log($"是别人出牌，座位#{packet.SeatID}");
                var srcSeat = m_Seats[packet.SeatID].transform;
                Vector3 src = srcSeat.position;
                otherCard.transform.position = src;
                otherCard.m_Group.alpha = 1;
                otherCard.InitData(card);
                otherCard.PlayCardAnime();
            }
            else
            {
                //Debug.Log("是自己出牌，执行委托Item_Card.PlayCardAnime()");
                PlayAction?.Invoke();
                m_PlayPanel.SetActive(false);
            }

            //Debug.Log("等待2秒......START");
            await Task.Delay(2000);
            string chessStr = string.Empty;
            for (int i = 0; i < moveChessList.Count; i++)
                chessStr += $"{moveChessList[i]}、";
            Debug.Log($"等待2秒........END：移动{moveChessList.Count}个：{chessStr}");

            // 有堆叠，颜色是计算得到的数组
            //bool colorful = card.cardColor == CardColor.COLOR || card.cardColor == CardColor.SLOWEST;
            //ChessColor colorKey = colorful ? (ChessColor)colorId : (ChessColor)card.cardColor; //哪只乌龟
            int step = (int)card.cardNum; //走几步

            // ③动画控制走棋子，考虑叠起来
            for (int i = 0; i < moveChessList.Count; i++)
            {
                int index = moveChessList[i];
                var chess = gameChess[index];
                Debug.Log($"移动棋子{index}：{(ChessColor)index}---{chess.mColor}");
                chess.Move((ChessColor)index, step);
            }

            if (m_Room.gameStatus == ChessStatus.End)
            {
                await Task.Delay(1500);
                GameEndAction?.Invoke();
            }

            // ④下一轮出牌者
            m_Room.NextTurn = packet.SeatID + 1;
            if (m_Room.NextTurn >= m_Room.RoomLimit)
                m_Room.NextTurn = 0;
            if (m_Room.NextTurn == m_LocalPlayer.SeatId)
            {
                // 解除手牌锁定
            }
        }
        // 发牌消息
        async void OnDeal(object reader)
        {
            var packet = (S2C_DealPacket)reader;
            Debug.Log($"[S2C_GameDeal] 收到新的牌: Card:{packet.CardID}, to#{packet.SeatID}");

            // 解析牌型
            Card card = ClientRoom.lib.library[packet.CardID];
            Debug.Log($"发牌：{card.Log()}");
            //Debug.Log($"发牌：{Card.LogColor((ChessColor)card.cardColor, (int)card.cardNum)}");
            m_Room.OnGameDeal_Client(card);

            // 发牌动画
            await Task.Delay(3000); //等待出牌和走棋动画
            myCards[handIndex].transform.SetAsLastSibling();
            myCards[handIndex].gameObject.SetActive(true);

            otherCard.InitData(card);
            otherCard.transform.position = new Vector3(Screen.width, Screen.height) / 2;
            otherCard.m_Group.alpha = 1;

            Vector3 dst = myCards[myCards.Count - 1].transform.position; //该位置放一堆牌
            //Debug.Log($"移动到：{dst}");
            Tweener tw1 = otherCard.transform.DOMove(dst, 0.3f);
            await Task.Delay(1000);

            otherCard.m_Group.DOFade(0, 0.5f);
            myCards[handIndex].InitData(card);
            myCards[handIndex].m_Group.alpha = 1;
        }
        // 结算消息
        void OnGameResult(object reader)
        {
            Debug.Log("[S2C_GameResult] 结算消息");
            var packet = (S2C_GameResultPacket)reader;

            // 等待棋子走完，再弹出。
            m_Room.OnGameResult_Client();
            GameEndAction = () =>
            {
                Debug.Log("结算委托");
                var ui_result = UIManager.Get().Push<UI_GameResult>();
                ui_result.UpdateUI(packet.Rank);
            };
        }
        #endregion
    }
}