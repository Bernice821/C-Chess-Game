using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Client
{
    public partial class Form1 : Form
    {

        #region Gglobal Variable
        Socket socketClient;
        Thread listenThread;
        IPEndPoint EP;
        byte[] data = new byte[10024];
        bool IsConnect;
        bool isMessage2ALL = true;
        String Client_ID = "" + Guid.NewGuid();
        int[] playGround = new int[42];
        Image[] image_list = new Image[3];
        PictureBox[] pb = new PictureBox[42];
        Image defaultImg = Image.FromFile(Application.StartupPath + "\\hole.png");
        int game_time = 0;
        int dropRow = 0;
        int player_color = 1;
        int[] col_chessNumber = { 0, 0, 0, 0, 0, 0, 0 };
        ArrayList save_chess_msg = new ArrayList();
        #endregion

        #region Form Evens
        public Form1()
        {
            InitializeComponent();

            for (int i = 0; i < 42; i++)
            {
                pb[i] = new PictureBox();
                pb[i].Width = 60;
                pb[i].Height = 60;
                pb[i].BackColor = Color.Black;
                pb[i].Visible = true;
                pb[i].BorderStyle = BorderStyle.Fixed3D;
                pb[i].BackgroundImage = defaultImg;
                pb[i].BackgroundImageLayout = ImageLayout.Stretch;
                pb[i].Top = 420 - (i / 7) * 80;
                pb[i].Left = 370 + (i % 7) * 80;
                //pb[i].Click += new EventHandler(pb_Click);
                pb[i].Name = i.ToString();
                this.Controls.Add(pb[i]);

                //solve[i] = false;
            }

            image_list[0] = Image.FromFile(Application.StartupPath + "\\hole.png");
            image_list[1] = Image.FromFile(Application.StartupPath + "\\red.png");
            image_list[2] = Image.FromFile(Application.StartupPath + "\\yellow.png");
        }
        /*------------------------------------圖片欄------------------------------------------------------*/
        private void clientForm_Load(object sender, EventArgs e)
        {
            ListBox.CheckForIllegalCrossThreadCalls = false;
        }
        /*-------------------------------------------------------------------------------------*/
        private void clientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (entry_Btn.Enabled == false)
            {
                socketSend("logout," + nickname_TB.Text + ",");
                socketClient.Close();
            }
        }
        #endregion

        #region gameFunction
        private void entry_Btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (serverIP_TB.Text != "" && port_TB.Text != "" && nickname_TB.Text != "")      //checking TB != ""
                {
                    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    EP = new IPEndPoint(IPAddress.Parse(serverIP_TB.Text), int.Parse(port_TB.Text));
                    socketClient.Connect(EP);
                    listenThread = new Thread(socketReceive);
                    listenThread.IsBackground = true;
                    listenThread.Start();
                    IsConnect = true;
                    socketSend("login," + nickname_TB.Text + "," + Client_ID + ",");

                    serverIP_TB.Enabled = false;
                    port_TB.Enabled = false;
                    nickname_TB.Enabled = false;
                    entry_Btn.Enabled = false;
                    //gBox1.Visible = false;
                    gBox2.Visible = true;
                    btn_send.Enabled = false;
                }
                else
                {
                    MessageBox.Show("請輸入完整資訊");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void update_playground(String playground_Msg)
        {
            char token = ' ';
            String[] picData = playground_Msg.Split(token);
            for (int i =0; i<playGround.Length; i++)
            {
                playGround[i] = int.Parse(picData[i]);
            }
            //playGround = Array.ConvertAll(picData, int.Parse);
            show_update_playground();
        }

        private void show_update_playground()
        {
            for(int i = 0; i< playGround.Length; i++)
            { 
                pb[i].BackgroundImage = image_list[playGround[i]];
            }
        }

        void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb == null)
            {
                MessageBox.Show("Sender is not a RadioButton");
                return;
            }

            // Ensure that the RadioButton.Checked property
            // changed to true.
            if (rb.Checked)
            {
                // Keep track of the selected RadioButton by saving a reference
                // to it.
                switch (rb.Name)
                {
                    case "radioButton1":
                        dropRow = 0;
                        break;
                    case "radioButton2":
                        dropRow = 1;
                        break;
                    case "radioButton3":
                        dropRow = 2;
                        break;
                    case "radioButton4":
                        dropRow = 3;
                        break;
                    case "radioButton5":
                        dropRow = 4;
                        break;
                    case "radioButton6":
                        dropRow = 5;
                        break;
                    case "radioButton7":
                        dropRow = 6;
                        break;
                }
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            if (playGround[5 * 7 + dropRow] == 0)
            {
                MessageBox.Show("您投了第" + (dropRow + 1) + "行");
                for (int i = 0; i < 6; i++)
                {
                    int index = i * 7 + dropRow;
                    if (playGround[index] == 0)
                    {
                        playGround[index] = player_color;
                        show_update_playground();
                        socketSend("select," + nickname_TB.Text + "," + index + "," + player_color + ",");
                        break;
                    }
                }
                btn_send.Enabled = false;
            }
            else
            {
                MessageBox.Show("此行欄位已滿,請試別行!");
            }
        }
        #endregion

        #region receive
        public void Receive(string recvData) //接收並判斷資料
        {
            try
            {
                char token = ',';
                string[] pt = recvData.Split(token);        //將接收資料用 , 切割並存入陣列
                switch (pt[0])      //使用陣列中第一筆資料溝通
                {
                    case "refresh":
                        update_playground(pt[1]);
                        break;
                    case "yourTurn":
                        btn_send.Enabled = true;
                        break;
                    case "message"://///////////////////////////////////////////////////////////
                        if (pt[1].Equals(" 遊戲開始")) {
                            MessageBox.Show("請選擇投入的行");
                            timer1.Enabled = true;
                        }
                        if (pt[1].Equals(" 遊戲重新開始"))
                        {
                            MessageBox.Show("請選擇投入的行");
                            btn_send.Enabled = true;
                        }
                        if (pt[1].Equals("FinalResult"))
                        {
                            if (pt[2].Equals("no winner"))
                            {
                                socketSend("平手，請再比一次");
                                btn_send.Enabled = false;
                                timer1.Enabled = false;
                            }
                            else { socketSend("message2All, 玩家" + pt[2] + "獲勝,"); MessageBox.Show("玩家" + pt[2] + "獲勝\n可以請求重新開始"); btn_send.Enabled = false;
                                timer1.Enabled = false;
                            }
                        }
                        break;
                    case "color":
                        player_color = int.Parse(pt[1]);
                        playGround = new int[42];
                        show_update_playground();
                        break;
                    case "DM":
                        //log_LB.Items.Add(pt[1]);
                        break;
                    case "deny":
                        serverIP_TB.Enabled = true;
                        port_TB.Enabled = true;
                        nickname_TB.Enabled = true;
                        entry_Btn.Enabled = true;
                        break;


                }
                Array.Clear(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion

        #region Socket
        public void socketReceive()
        {
            int recvLength = 0;
            try
            {
                do
                {
                    recvLength = socketClient.Receive(data);
                    if (recvLength > 0)
                    {
                        Receive(Encoding.Default.GetString(data).Trim());       //將接收到的 byte 資料 轉換成 string 並呼叫 receive 方法
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public void socketSend(string sendData)
        {
            if (IsConnect)
            {
                try
                {
                    socketClient.Send(Encoding.Default.GetBytes(sendData));     //將要傳送的 string 資料 轉換成 byte 傳送出去
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                //log_LB.Items.Add("遊戲已斷線");
            }
        }
        #endregion

        private void btnagain_Click(object sender, EventArgs e) //再來一次
        {
            socketSend("again," + nickname_TB.Text + " :請求再比一次,");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            game_time++;
            label1.Text = "遊戲時間: " + game_time + " 秒";
        }

        private void btn_logout_Click(object sender, EventArgs e)
        {
            serverIP_TB.Enabled = true;
            port_TB.Enabled = true;
            nickname_TB.Enabled = true;
            entry_Btn.Enabled = true;
            //gBox1.Visible = false;
            gBox2.Visible = false;
            btn_send.Enabled = true;
        }
    }
}