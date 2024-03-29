﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using kcp2k.Examples;

namespace HotFix
{
    // 处理子线程的推送
    public class EventManager : MonoBehaviour
    {
        public static EventManager Get;

        public Queue<byte[]> queue;

        void Awake()
        {
            Get = this;

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

        void LateUpdate()
        {
            KcpChatClient.client.Tick();
        }

        void Handle(byte[] buffer)
        {
            // 解析msgId
            byte msgId = buffer[0];
            byte[] body = new byte[buffer.Length - 1];
            Array.Copy(buffer, 1, body, 0, buffer.Length - 1);

            PacketType type = (PacketType)msgId;
            MemoryStream stream = new MemoryStream(body, 0, body.Length);
            Debug.Log($"<color=yellow>msgType={type}, recv len:{buffer.Length}</color>");
            switch (type)
            {
                case PacketType.Connected:
                    {
                        Debug.Log("<color=green>与服务器连接成功</color>");
                        var packet = new EmptyPacket();
                        NetPacketManager.Trigger(type, packet);
                        SendLoginByToken(); //连接成功，使用IPC获取的Token登录
                        break;
                    }
                case PacketType.Disconnect: //被动断开（服务器没开等原因）
                    {
                        Debug.Log("<color=red>与服务器断开连接</color>");
                        var dialog = UIManager.Get.Push<UI_Dialog>();
                        dialog.Show("与服务器断开连接", () =>
                        {
                            UIManager.Get.PopAll();
                            UIManager.Get.Push<UI_Login>();
                        }, "确定");
                        break;
                    }
                case PacketType.Reconnect:
                    {
                        Debug.Log("<color=orange>重连中</color>");
                        UIManager.Get.Push<UI_Connect>(1);
                        break;
                    }
                case PacketType.S2C_ErrorOperate:
                    {
                        var packet = ProtobufHelper.Deserialize<ErrorPacket>(stream);
                        var toast = UIManager.Get.Push<UI_Toast>(1);
                        toast.Show(packet.Message);
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
                        NetPacketManager.Trigger(type, packet);
                        OnLogoutResult(packet);
                        break;
                    }
                case PacketType.S2C_RoomList:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_GetRoomList>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_RoomInfo:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_RoomInfo>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_LeaveRoom:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_LeaveRoomPacket>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_Chat:
                    {
                        var packet = ProtobufHelper.Deserialize<TheMsg>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_GameStart:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_GameStartPacket>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_YourTurn:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_NextTurnPacket>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_GamePlay:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_PlayCardPacket>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_GameDeal:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_DealPacket>(stream);
                        NetPacketManager.Trigger(type, packet);
                        break;
                    }
                case PacketType.S2C_GameResult:
                    {
                        var packet = ProtobufHelper.Deserialize<S2C_GameResultPacket>(stream);
                        NetPacketManager.Trigger(type, packet);
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
                            KcpChatClient.m_PlayerManager.LocalPlayer.ResetToLobby();
                        }
                        break;
                    }
                case PacketType.S2C_GameStart:
                    {
                        KcpChatClient.m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.GAME);
                        break;
                    }
                case PacketType.S2C_GameResult:
                    {
                        KcpChatClient.m_PlayerManager.LocalPlayer.ResetToLobby();
                        break;
                    }
            }
            UserEventManager.Trigger(KcpChatClient.m_PlayerManager.LocalPlayer.Status); //通知给UI
        }

        // 自己登出
        void OnLogoutResult(object reader)
        {
            Debug.Log($"<color=red>[C] {KcpChatClient.m_PlayerManager.LocalPlayer.UserName}登出重置</color>");
            KcpChatClient.m_PlayerManager.Reset();
        }

        void OnErrorOperate(object reader)
        {
            ErrorPacket packet = (ErrorPacket)reader;
            Debug.Log($"错误操作：{(ErrorCode)packet.Code}");

            var toast = UIManager.Get.Push<UI_Toast>();
            toast.Show($"{(ErrorCode)packet.Code}");
        }

        public async void SendLoginByToken()
        {
            string token = Client.GameManager.Token;
            Debug.Log($"连接服务器成功，尝试读取Token：'{token}'");

            var connect = UIManager.Get.Push<UI_Connect>(1);
            await Task.Delay(500);

            // 使用Token登录
            if (!string.IsNullOrEmpty(token))
            {
                KcpChatClient.SendLogin(token);
            }
            else
            {
                connect.Pop();
            }
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