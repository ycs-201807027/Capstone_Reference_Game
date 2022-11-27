﻿using Capstone_Reference_Game.Client;
using Capstone_Reference_Game.Manager;
using Capstone_Reference_Game_Module;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Capstone_Reference_Game.Form
{
    public partial class ReferenceGame_Form : System.Windows.Forms.Form
    {
        // 메인 매니저
        public GameManager GameManager { get; private set; }

        // 현재 보여지고 있는 퀴즈
        public QuizBase? ShowingQuiz { get; private set; }

        // 유저 캐릭터
        public ClientCharacter? UserCharacter { get; private set; }

        // 다른 클라이언트들의 캐릭터
        public ConcurrentDictionary<int, ClientCharacter> Clients { get; } = new ConcurrentDictionary<int, ClientCharacter>();

        // 동기화를 위해 자신의 좌표를 서버에게 알려주는 타이머
        private System.Threading.Timer? syncTimer;

        // 시작 여부
        public bool IsStart { get; set; }

        public ReferenceGame_Form()
        {
            InitializeComponent();
            GameManager = new GameManager(this);
            
            if(true)
            {
                UserCharacter = new ClientCharacter(-1, 0);
                TimerCallback tc = new TimerCallback(LocationSync);
                syncTimer = new System.Threading.Timer(tc,null,Timeout.Infinite, Timeout.Infinite);
            }

            KeyPreview = true;
        }

        private void ReferecneGame_Form_Load(object sender, EventArgs e)
        {
            GameManager.Start();

            GameStart();
            
        }

        // 게임 시작
        public void GameStart()
        {
            IsStart = true;
            syncTimer?.Change(0, 500);

            OXQuiz oXQuizForm = new OXQuiz(UserCharacter, Clients);
            oXQuizForm.SetTitle("sdfdf");
            ChangeScreen(oXQuizForm);
            oXQuizForm.Start();
        }

        // 보여질 화면 변경
        public void ChangeScreen(QuizBase newScreen)
        {
            this.Controls.Clear();
            this.Controls.Add(newScreen);
            ShowingQuiz = newScreen;
        }
        
        private void LocationSync(object? o)
        {
            if (UserCharacter != null)
            {
                MessageGenerator generator = new MessageGenerator(Protocols.C_LOCATION_SYNC);
                generator.AddInt(UserCharacter.Location.X);
                generator.AddInt(UserCharacter.Location.Y);

                GameManager.SendMessage(generator.Generate());
            }
        }

        #region Input Process

        // 키가 눌렸을 때
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 관전자 모드일경우 키처리 X
            if (IsStart && UserCharacter != null)
            {
                // 메시지 생성기 생성
                MessageGenerator generator = new MessageGenerator(Protocols.C_KEY_INPUT);

                byte downKey;
                switch (keyData)
                {
                    case Keys.Left:
                        downKey = Keyboard.LEFT;
                        break;
                    case Keys.Right:
                        downKey = Keyboard.RIGHT;
                        break;
                    case Keys.Up:
                        downKey = Keyboard.UP;
                        break;
                    case Keys.Down:
                        downKey = Keyboard.DOWN;
                        break;
                    default:
                        return base.ProcessCmdKey(ref msg, keyData);
                }

                if (UserCharacter.Keys[downKey] == false)
                {
                    UserCharacter.Keys[downKey] = true;
                    generator.AddByte(downKey);
                    generator.AddBool(true);

                    // 눌린 키를 서버에 알려줌
                    GameManager.SendMessage(generator.Generate());
                }
            }
            return true;
        }

        // 키가 떼어졌을 때
        private void Form_KeyUp(object sender, KeyEventArgs e)
        {
            // 관전자 일경우 키처리 X
            if (IsStart && UserCharacter != null)
            {
                // 메시지 생성기 생성
                MessageGenerator generator = new MessageGenerator(Protocols.C_KEY_INPUT);

                byte downKey;
                switch (e.KeyData)
                {
                    case Keys.Left:
                        downKey = Keyboard.LEFT;
                        break;
                    case Keys.Right:
                        downKey = Keyboard.RIGHT;
                        break;
                    case Keys.Up:
                        downKey = Keyboard.UP;
                        break;
                    case Keys.Down:
                        downKey = Keyboard.DOWN;
                        break;
                    default:
                        return;
                }

                if (UserCharacter.Keys[downKey] == true)
                {
                    UserCharacter.Keys[downKey] = false;
                    generator.AddByte(downKey);
                    generator.AddBool(false);

                    // 떼진 키를 서버에 알려줌
                    GameManager.SendMessage(generator.Generate());
                }
            }
            
        }

        // 폼의 포커스가 풀리면 ( 알트 탭, 다른 윈도우 선택시 ) 이벤트 발생
        private void Form_Deactivate(object sender, EventArgs e)
        {
            if (UserCharacter != null)
            {
                MessageGenerator generator = new MessageGenerator(Protocols.C_KEY_INPUT);
                // 입력중인 키 모두 해제
                for(byte i = 0; i < UserCharacter.Keys.Length; i++)
                {
                    UserCharacter.Keys[i] = false;
                    generator.AddByte(i);
                    generator.AddBool(false);

                    // 서버에 전송
                    GameManager.SendMessage(generator.Generate());
                    generator.Clear();
                }
            }
        }


        #endregion Input Process
    }
}