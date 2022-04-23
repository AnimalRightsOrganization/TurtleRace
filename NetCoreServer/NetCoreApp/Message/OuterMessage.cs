using ET;
using ProtoBuf;
using System.Collections.Generic;
namespace ET
{
//option csharp_namespace = "HotFix";
// 错误码
	[Message(OuterOpcode.ErrorPacket)]
	[ProtoContract]
	public partial class ErrorPacket: Object
	{
		[ProtoMember(1)]
		public int Code { get; set; }

		[ProtoMember(2)]
		public string Message { get; set; }

	}

//message Empty {} //注意括号不能写在一行，工具不认。
	[Message(OuterOpcode.EmptyPacket)]
	[ProtoContract]
	public partial class EmptyPacket: Object
	{
	}

// 聊天
	[Message(OuterOpcode.TheMsg)]
	[ProtoContract]
	public partial class TheMsg: Object
	{
		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public string Content { get; set; }

	}

	[Message(OuterOpcode.TheMsgList)]
	[ProtoContract]
	public partial class TheMsgList: Object
	{
		[ProtoMember(1)]
		public int Id { get; set; }

		[ProtoMember(2)]
		public List<string> Content = new List<string>();

	}

// 登录
	[Message(OuterOpcode.C2S_LoginPacket)]
	[ProtoContract]
	public partial class C2S_LoginPacket: Object
	{
		[ProtoMember(1)]
		public string Username { get; set; }

		[ProtoMember(2)]
		public string Password { get; set; }

	}

	[Message(OuterOpcode.S2C_LoginResultPacket)]
	[ProtoContract]
	public partial class S2C_LoginResultPacket: Object
	{
		[ProtoMember(1)]
		public int Code { get; set; }

//string PeerId;
		[ProtoMember(2)]
		public string Username { get; set; }

		[ProtoMember(3)]
		public string Nickname { get; set; }

	}

// 房间
	[Message(OuterOpcode.C2S_CreateRoomPacket)]
	[ProtoContract]
	public partial class C2S_CreateRoomPacket: Object
	{
		[ProtoMember(1)]
		public string RoomName { get; set; }

		[ProtoMember(2)]
		public string RoomPwd { get; set; }

		[ProtoMember(3)]
		public int LimitNum { get; set; }

	}

	[Message(OuterOpcode.C2S_JoinRoomPacket)]
	[ProtoContract]
	public partial class C2S_JoinRoomPacket: Object
	{
		[ProtoMember(1)]
		public int RoomID { get; set; }

		[ProtoMember(2)]
		public string RoomPwd { get; set; }

	}

	[Message(OuterOpcode.S2C_RoomInfo)]
	[ProtoContract]
	public partial class S2C_RoomInfo: Object
	{
		[ProtoMember(1)]
		public RoomInfo Room { get; set; }

	}

	[Message(OuterOpcode.S2C_GetRoomList)]
	[ProtoContract]
	public partial class S2C_GetRoomList: Object
	{
		[ProtoMember(1)]
		public List<RoomInfo> Rooms = new List<RoomInfo>();

	}

	[Message(OuterOpcode.C2S_OperateSeatPacket)]
	[ProtoContract]
	public partial class C2S_OperateSeatPacket: Object
	{
		[ProtoMember(1)]
		public int SeatID { get; set; }

		[ProtoMember(2)]
		public int Operate { get; set; }

//int32 Level 	= 3; //操作内容（0.机器人难度，1.给房主）
	}

//工具不支持使用enum
//enum OperateType
//{
//	ADD_BOT 		= 0; //Proto3中，首成员必须是0。
//	KICK_PLAYER 	= 1;
//}
//房间信息
	[Message(OuterOpcode.RoomInfo)]
	[ProtoContract]
	public partial class RoomInfo: Object
	{
		[ProtoMember(1)]
		public int RoomID { get; set; }

		[ProtoMember(2)]
		public string RoomName { get; set; }

		[ProtoMember(3)]
		public int LimitNum { get; set; }

		[ProtoMember(4)]
		public List<PlayerInfo> Players = new List<PlayerInfo>();

	}

//房间内玩家信息
	[Message(OuterOpcode.PlayerInfo)]
	[ProtoContract]
	public partial class PlayerInfo: Object
	{
		[ProtoMember(1)]
		public string UserName { get; set; }

		[ProtoMember(2)]
		public string NickName { get; set; }

		[ProtoMember(3)]
		public int SeatID { get; set; }

	}

//出牌
	[Message(OuterOpcode.C2S_PlayCard)]
	[ProtoContract]
	public partial class C2S_PlayCard: Object
	{
		[ProtoMember(1)]
		public int CardID { get; set; }

	}

	[Message(OuterOpcode.S2C_PlayCard)]
	[ProtoContract]
	public partial class S2C_PlayCard: Object
	{
		[ProtoMember(1)]
		public int CardID { get; set; }

		[ProtoMember(2)]
		public int SeatID { get; set; }

	}

//发牌
	[Message(OuterOpcode.S2C_Deal)]
	[ProtoContract]
	public partial class S2C_Deal: Object
	{
		[ProtoMember(1)]
		public int CardID { get; set; }

		[ProtoMember(2)]
		public int SeatID { get; set; }

	}

//比赛结果
	[Message(OuterOpcode.S2C_GameResult)]
	[ProtoContract]
	public partial class S2C_GameResult: Object
	{
	}

}
