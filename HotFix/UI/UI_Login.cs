﻿using UnityEngine;
using UnityEngine.UI;
using kcp2k.Examples;

namespace HotFix
{
    // 外部通过窗口句柄传入登录Token，转圈，验证后直接跳转界面。
    // 如果没有Token，则留在本页面，让用户手动登录。
    public class UI_Login : UIBase
    {
        #region 界面组件
        public GameObject m_Foreground; //前景，登录后隐藏
        public Text m_VersionText;

        public GameObject m_Login_Panel;
        public InputField m_Login_UserInput;
        public InputField m_Login_PwdInput;
        public Button m_CloseLogin;
        public Button m_LoginBtn;
        public Button m_GoSignUpBtn;

        public GameObject m_SignUp_Panel;
        public InputField m_SignUp_UserInput;
        public InputField m_SignUp_PwdInput;
        public InputField m_SignUp_Pwd2Input;
        public Button m_CloseSignUp;
        public Button m_SignUpBtn;
        public Button m_GoLoginBtn;

        public Button m_OAuthBtn;
        #endregion

        #region 内置方法
        void Awake()
        {
            m_Foreground = transform.Find("Foreground").gameObject;
            m_VersionText = transform.Find("Foreground/Version").GetComponent<Text>();

            m_Login_Panel = transform.Find("LoginPanel").gameObject;
            m_Login_UserInput = transform.Find("LoginPanel/UserInput").GetComponent<InputField>();
            m_Login_PwdInput = transform.Find("LoginPanel/PwdInput").GetComponent<InputField>();
            m_CloseLogin = transform.Find("LoginPanel/CloseBtn").GetComponent<Button>();
            m_LoginBtn = transform.Find("LoginPanel/LoginBtn").GetComponent<Button>();
            m_GoSignUpBtn = transform.Find("LoginPanel/GoSignUpBtn").GetComponent<Button>();
            m_CloseLogin.onClick.AddListener(OnCloseLoginPanel);
            m_LoginBtn.onClick.AddListener(OnLoginBtnClick);
            m_GoSignUpBtn.onClick.AddListener(OnGoSignUpBtnClick);

            m_SignUp_Panel = transform.Find("SignUpPanel").gameObject;
            m_SignUp_UserInput = transform.Find("SignUpPanel/UserInput").GetComponent<InputField>();
            m_SignUp_PwdInput = transform.Find("SignUpPanel/PwdInput").GetComponent<InputField>();
            m_SignUp_Pwd2Input = transform.Find("SignUpPanel/Pwd2Input").GetComponent<InputField>();
            m_CloseSignUp = transform.Find("SignUpPanel/CloseBtn").GetComponent<Button>();
            m_SignUpBtn = transform.Find("SignUpPanel/SignUpBtn").GetComponent<Button>();
            m_GoLoginBtn = transform.Find("SignUpPanel/GoLoginBtn").GetComponent<Button>();
            m_CloseSignUp.onClick.AddListener(OnCloseSignUpPanel);
            m_SignUpBtn.onClick.AddListener(OnSignUpBtnClick);
            m_GoLoginBtn.onClick.AddListener(OnGoLoginBtnClick);

            m_OAuthBtn = transform.Find("OAuthBtn").GetComponent<Button>();
            m_OAuthBtn.onClick.AddListener(OnOAuthBtnClick);

            m_Login_Panel.SetActive(false);
            m_SignUp_Panel.SetActive(false);
        }
        void OnEnable()
        {
            NetPacketManager.RegisterEvent(OnNetCallback);

            m_VersionText.text = $"v{Client.GameManager.present.app_version}.{Client.GameManager.present.res_version}";
            m_OAuthBtn.gameObject.SetActive(true);
        }
        void OnDisable()
        {
            NetPacketManager.UnRegisterEvent(OnNetCallback);

            m_Login_Panel.SetActive(false);
        }
        #endregion

        #region 按钮事件
        void OnCloseSignUpPanel()
        {
            m_SignUp_Panel.SetActive(false);
        }
        void OnCloseLoginPanel()
        {
            m_Login_Panel.SetActive(false);
        }
        void OnLoginBtnClick()
        {
            string username = m_Login_UserInput.text;
            string password = m_Login_PwdInput.text;
            KcpChatClient.SendLogin(username, password);
        }
        void OnGoLoginBtnClick()
        {
            m_Login_Panel.SetActive(true);
            m_SignUp_Panel.SetActive(false);
        }
        void OnGoSignUpBtnClick()
        {
            m_Login_Panel.SetActive(false);
            m_SignUp_Panel.SetActive(true);
        }
        void OnSignUpBtnClick()
        {
            if (string.IsNullOrEmpty(m_SignUp_UserInput.text) || 
                string.IsNullOrEmpty(m_SignUp_PwdInput.text) ||
                string.IsNullOrEmpty(m_SignUp_Pwd2Input.text))
            {
                var ui_toast = UIManager.Get.Push<UI_Toast>();
                ui_toast.Show("用户名和密码不能为空");
                return;
            }
            if (m_SignUp_PwdInput.text.Equals(m_SignUp_Pwd2Input.text) == false)
            {
                var ui_toast = UIManager.Get.Push<UI_Toast>();
                ui_toast.Show("两次密码输入不一致");
                return;
            }
            if (m_SignUp_PwdInput.text.Length < 6)
            {
                var ui_toast = UIManager.Get.Push<UI_Toast>();
                ui_toast.Show("密码长度过短");
                return;
            }
            string username = m_SignUp_UserInput.text;
            string password = m_SignUp_Pwd2Input.text;
            KcpChatClient.SendSignUp(username, password);
        }
        void OnOAuthBtnClick()
        {
            if (KcpChatClient.IsConnected() == false && Main.trying == false)
            {
                Main.TryConnect(() => { m_OAuthBtn.gameObject.SetActive(false); });
                return;
            }

            //#if Channel_102 //热更工程里没有宏，不能继承Unity的宏
            //bool checkChannel = ConstValue.CHANNEL_NAME == "Channel_102";
            bool checkChannel = Main.CHANNEL_NAME == "Channel_102";
            bool checkInstall = HookManager.Get.CheckInstall();
            Debug.Log($"检查渠道(Main:{Main.CHANNEL_NAME}/ConstValue:{ConstValue.CHANNEL_NAME}):{checkChannel}, 大厅:{checkInstall}");
            if (checkChannel && checkInstall)
            {
                // 判断渠道号（根据平台和包名）。弹出默认登录或三方SDKView。
                Debug.Log("主动请求");
                HookManager.Get.JumpActivity();
            }
            else
            {
                m_Login_Panel.SetActive(true);
                m_SignUp_Panel.SetActive(false);
            }
        }
        #endregion

        #region 网络事件
        void OnNetCallback(PacketType type, object reader)
        {
            switch (type)
            {
                case PacketType.Connected:
                    OnConnected();
                    break;
                case PacketType.S2C_LoginResult:
                    OnLoginResult(reader);
                    break;
            }
        }
        void OnConnected()
        {
            var connect = UIManager.Get.GetUI<UI_Connect>();
            connect.Pop();
            m_OAuthBtn.gameObject.SetActive(true);
        }
        void OnLoginResult(object reader)
        {
            var packet = (S2C_LoginResultPacket)reader;
            Debug.Log($"code={packet.Code}, usr={packet.Username}");
            var playerData = new BasePlayerData
            {
                UserName = packet.Username,
                NickName = packet.Nickname,
            }; //没填的都是默认值
            var clientPlayer = new ClientPlayer(playerData);
            clientPlayer.ResetToLobby();
            KcpChatClient.m_PlayerManager.AddClientPlayer(clientPlayer, true);
            UIManager.Get.Push<UI_Main>();

            var connect = UIManager.Get.GetUI<UI_Connect>();
            if (connect != null)
                connect.Pop();

            this.Pop();
        }
        #endregion
    }
}