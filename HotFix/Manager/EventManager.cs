﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ET;

namespace HotFix
{
    // 处理子线程的推送
    public class EventManager : MonoBehaviour
    {
        static EventManager _instance;
        public static EventManager Get()
        {
            return _instance;
        }

        public Queue<byte[]> queue;

        void Awake()
        {
            _instance = this;
            queue = new Queue<byte[]>();
        }

        void Update()
        {
            if (queue.Count > 0)
            {
                var data = queue.Dequeue();
                Handle(data);
            }
        }

        void Handle(byte[] buffer)
        {
            // 解析msgId
            byte msgId = buffer[0];
            byte[] body = new byte[buffer.Length - 1];
            Array.Copy(buffer, 1, body, 0, buffer.Length - 1);

            PacketType type = (PacketType)msgId;
            MemoryStream stream = new MemoryStream(body, 0, body.Length);
            //Debug.Log($"PacketType={type}");
            switch (type)
            {
                case PacketType.Connected:
                    break;
                case PacketType.Disconnect:
                    {
                        // 销毁所有UI，返回登录页
                        //UIManager.Get().PopAll();
                        //UIManager.Get().Push<UI_Login>();
                        break;
                    }
                case PacketType.S2C_LoginResult:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_LoginResultPacket>(stream); //解包
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                case PacketType.S2C_LogoutResult:
                    {
                        EmptyPacket packet = new EmptyPacket();
                        NetPacketManager.Trigger(type, packet); //派发
                        OnLogoutResult(packet);
                        break;
                    }
                case PacketType.S2C_RoomList:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_GetRoomList>(stream); //解包
                        if (packet.Rooms.Count > 0)
                        {
                            Debug.Log($"Room.0={packet.Rooms[0].RoomID}");
                        }
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                case PacketType.S2C_RoomInfo:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_RoomInfo>(stream); //解包
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                case PacketType.S2C_LeaveRoom:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_LeaveRoomPacket>(stream);
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                case PacketType.S2C_Chat:
                    {
                        var packet = ProtobufHelper.Deserialize<TheMsg>(stream); //解包
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                case PacketType.S2C_GameStart:
                    {
                        EmptyPacket packet = new EmptyPacket();
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                case PacketType.S2C_GamePlay:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_PlayCard>(stream); //解包
                        NetPacketManager.Trigger(type, packet); //派发
                        break;
                    }
                default:
                    Debug.LogError($"Handle:无法识别的消息: {type}");
                    break;
            }
        }

        // 统一处理用户状态变化，并派发出去
        void OnUserStatusChanged(PacketType type, object reader)
        {
            switch (type)
            {
                case PacketType.S2C_LoginResult:
                    {
                        var packet = (S2C_LoginResultPacket)reader;
                        if (packet.Code == 0)
                        {
                            //ReconnectTimes = 2; //登录成功，补充重连次数
                            TcpChatClient.m_PlayerManager.LocalPlayer.ResetToLobby();
                        }
                        break;
                    }
                case PacketType.S2C_GameReady:
                    {
                        break;
                    }
                case PacketType.S2C_GameStart:
                    {
                        TcpChatClient.m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.AtBattle);
                        break;
                    }
                case PacketType.S2C_GameResult:
                    {
                        TcpChatClient.m_PlayerManager.LocalPlayer.ResetToLobby();
                        break;
                    }
            }
            UserEventManager.Trigger(TcpChatClient.m_PlayerManager.LocalPlayer.Status); //通知给UI
        }

        // 自己登出
        void OnLogoutResult(object reader)
        {
            Debug.Log($"<color=red>[C] {TcpChatClient.m_PlayerManager.LocalPlayer.UserName}登出重置</color>");
            TcpChatClient.m_PlayerManager.Reset();
        }

        void OnErrorOperate(object reader)
        {
            ErrorPacket packet = (ErrorPacket)reader;
            Debug.Log($"错误操作：{(ErrorCode)packet.Code}");

            var toast = UIManager.Get().Push<UI_Toast>();
            toast.Show($"{(ErrorCode)packet.Code}");
        }
    }

    public class NetPacketManager
    {
        public delegate void EventHandler(PacketType t, object packet);
        public static event EventHandler Event;
        public static void RegisterEvent(EventHandler action)
        {
            Event += action;
        }
        public static void UnRegisterEvent(EventHandler action)
        {
            Event -= action;
        }
        public static void Trigger(PacketType type, object packet)
        {
            Event?.Invoke(type, packet);
        }
    }

    public class UserEventManager
    {
        public delegate void EventHandler(PlayerStatus t);
        public static event EventHandler Event;
        public static void RegisterEvent(EventHandler action)
        {
            Event += action;
        }
        public static void UnRegisterEvent(EventHandler action)
        {
            Event -= action;
        }
        public static void Trigger(PlayerStatus type)
        {
            Event?.Invoke(type);
        }
    }
}