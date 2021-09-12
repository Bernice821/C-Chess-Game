using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace Server
{
    public partial class serverForm : Form
    {
        #region Gglobal Variable
        TcpListener server;
        Hashtable HT = new Hashtable(); //宣告雜湊表
        Socket socketClient;            //建立 socket 物件
        Thread Th_Svr, Th_Clt;          //宣告兩個子執行續處理接聽
        int[] playGround = new int[42];
        int[,] Maps = new int[6, 7];
        ArrayList user_name = new ArrayList();
        ArrayList winner_is = new ArrayList();
        String return_winner = "";
        Socket[] sockets = new Socket[2];
        int playtimes = 0;
        int player_Num = 0;
        int current_player_num = 1;
        #endregion

        #region connect function
        private void ServerSub()
        {
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(ip_TB.Text), int.Parse(port_TB.Text));
            server = new TcpListener(EP);       //建立伺服器端接收器(總機)
            server.Start(100);

            while (true)
            {
                socketClient = server.AcceptSocket();
                Th_Clt = new Thread(listen);
                Th_Clt.IsBackground = true;
                Th_Clt.Start();
            }
        }

        private void listen()
        {
            Socket sck = socketClient;      //將此socket儲存起來
            Thread Th = Th_Clt;             //將此執行續儲存起來
            string id = null;               //宣告一個

            while (true)
            {
                byte[] B = new byte[1023];
                try
                {
                    int inLen = sck.Receive(B);
                    string[] Msg = Encoding.Default.GetString(B, 0, inLen).Split(',');

                    switch (Msg[0])
                    {
                        case "message2All":
                            logLBAdd("MessageAll", Msg[1]);
                            sendAll("message," + Msg[1]);
                            break;
                        case "login":
                            try
                            {
                                HT.Add(Msg[1], sck);
                                id = Msg[1];
                                user_name.Add(Msg[1]);
                                onlineList_LB.Items.Add(Msg[1]);
                                logLBAdd("Login", Msg[1]);
                                sendAll(onlineList());
                                sendTo("color,"+ onlineList_LB.Items.Count, sck);
                                player_Num++;
                            }
                            catch
                            {
                                sendTo("deny,", sck);
                            }
                            break;

                        case "logout":
                            HT.Remove(Msg[1]);
                            user_name.Remove(Msg[1]);
                            onlineList_LB.Items.Remove(Msg[1]);
                            sendAll(onlineList());
                            Th.Abort();
                            player_Num--;
                            break;

                        case "select":////////////////////////////////////////////////////////////////////////////////////////////
                            log_LB.Items.Add(Msg[1] + " 新下了一顆棋"); //選擇牌
                            playtimes++;
                            int index = int.Parse(Msg[2]);
                            int color = int.Parse(Msg[3]);
                            int col = index / 7;
                            int row = index % 7;
                            playGround[index] = color; //存入玩家所選顏色
                            sendAll("refresh," + playGround_to_Msg() + ",");
                            playground_to_2DArray();

                            int result = winner(color, col, row); //優勝
                            
                            if (result != 0)
                            {
                                sendAll("message," + "FinalResult\n玩家" + result + "獲勝,");
                                log_LB.Items.Add("玩家" + result + "獲勝");
                                return_winner = "";
                            }
                            else if (playtimes == 42)
                            {
                                sendAll("message," + "平手請再比一次,");
                                log_LB.Items.Add("平手請再比一次");
                            }
                            else
                            {
                                sendTo("yourTurn,", nextPlayer());
                            }
                            break;

                        case "math":
                            break;

                        case "again"://再來一次
                            logLBAdd("MessageAll", Msg[1]);
                            sendAll("message," + Msg[1]);
                            break;
                    }
                    
                }
                catch
                {
                    logLBAdd("Crash", id);
                    sendAll("message," + id + "logout.");
                }
            }
        }
        private String nextPlayer()
        {
            String next_player = "";
            if (current_player_num < player_Num)
            {
                current_player_num++;
            }
            else
            {
                current_player_num = 1;
            }
            next_player = (string)user_name[current_player_num-1];
            return next_player;
        }

        private String playGround_to_Msg()
        {
            String msg = "";
            for(int i=0; i<=playGround.Length-2; i++)
            {
                msg += playGround[i] + " ";
            }

            msg += playGround[playGround.Length - 1]; //最後一項不加空白
            return msg;
        }

        private void playground_to_2DArray()
        {
            for(int i=0; i<playGround.Length; i++)
            {
                int col = i / 7;
                int row = i % 7;
                Maps[col, row] = playGround[i];
            }
            
        }

        private int winner(int color, int col, int row)
        {
            //不要想什麼螢幕位置，行就是行，列就是列
            //於落子位置結算橫，豎，斜，四個位置4-1-4的長度的連子情況。
            //檢查同一橫（變列）
            int Winner = 0;
            int n = 0;//計數變數，記錄最多幾個為color類

            for (int j = col - 3; j <= col + 3; j++)
            {
                //如果超過索引則跳過
                if (j < 0 || j >= 7)
                    continue;
                //否則檢查連子情況
                if (Maps[row, j] == color)
                {
                    n++;
                }
                else
                {
                    n = 0;
                }
                if (n == 4) Winner = color;
            }

            //檢查同一豎排（變行）
            n = 0;
            for (int i = row - 3; i <= row + 3; i++)
            {
                //如果超過索引則跳過
                if (i < 0 || i >= 7)
                    continue;
                if (Maps[i, col] == color)
                {
                    n++;
                }
                else
                {
                    n = 0;
                }
                if (n == 4) Winner = color;

            }

            //檢查左上到右下斜
            n = 0;
            for (int i = row - 3, j = col - 3; i <= row + 3; i++, j++)
            {
                //如果超過索引則跳過
                if (i < 0 || i >= 6 || j < 0 || j >= 7)
                    continue;
                if (Maps[i, j] == color)
                {
                    n++;
                }
                else
                {
                    n = 0;
                }
                if (n == 4) Winner = color;
            }

            //檢查左下到右上
            //檢查左上到右下斜
            n = 0;
            for (int i = row + 3, j = col - 3; i >= row - 3; i--, j++)
            {
                //如果超過索引則跳過
                if (i < 0 || i >= 6 || j < 0 || j >= 7)
                    continue;
                if (Maps[i, j] == color)
                {
                    n++;
                }
                else
                {
                    n = 0;
                }
                if (n == 4) Winner = color;
            }
            return Winner;
        }
        #endregion

        #region Sent Events
        private string onlineList()
        {
            string list = "list,";
            for(int i = 0 ; i < onlineList_LB.Items.Count ; i++)
            {
                list += onlineList_LB.Items[i] + ",";
            }

            return list;
        }

        private void sendTo(string str, string user)
        {
            byte[] B = Encoding.Default.GetBytes(str);
            Socket sck = (Socket)HT[user];
             sck.Send(B, 0, B.Length, SocketFlags.None);
        }
        private void sendTo(string str, Socket sck)
        {
            byte[] B = Encoding.Default.GetBytes(str);
            sck.Send(B, 0, B.Length, SocketFlags.None);
        }

        private void sendAll(string str)
        {
            try
            {
                byte[] B = Encoding.Default.GetBytes(str);
                foreach (Socket s in HT.Values)
                {
                    s.Send(B, 0, B.Length, SocketFlags.None);
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("遠端主機已強制關閉一個現存的連線。");
            }
        }
        #endregion

        #region Form Events
        private void logLBAdd(string type, string ifo)
        {
            log_LB.Items.Add("[" + type + "] : " + ifo);
        }

        public serverForm()
        {
            InitializeComponent();
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Th_Svr = new Thread(ServerSub);
                Th_Svr.IsBackground = true;
                Th_Svr.Start();
                log_LB.Items.Add("伺服器Socket建立完成！");
                log_LB.Update();
                connectBtn.Enabled = false;
                disconnectBtn.Enabled = true;
                ip_TB.Enabled = false;
                port_TB.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //form 關閉前事件
        private void serverForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            breakConnection();      //呼叫關閉方法
        }

        //disconnect Btn 點擊事件
        private void disconnectBtn_Click(object sender, EventArgs e)
        {
            log_LB.Items.Add("[System] : 伺服器中斷。");
            connectBtn.Enabled = true;
            disconnectBtn.Enabled = false;
        }

        private void serverForm_Load(object sender, EventArgs e)
        {
            ListBox.CheckForIllegalCrossThreadCalls = false;
            timer1.Enabled = true;
        }

        int timerCount = 0;

        private void 開新遊戲_Click(object sender, EventArgs e)
        {
            if (onlineList_LB.Items.Count >= 2)
            {
                label1.Text = Convert.ToString(int.Parse(label1.Text) + 1);
                playGround = new int[42];
                user_name.Clear();
                winner_is.Clear();
                String return_winner = "";
                sendAll("message, 遊戲重新開始,");
            }
            else { MessageBox.Show("須兩人或以上才能重新開始"); }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = Convert.ToString(int.Parse(label1.Text) + 1);
            sendAll("time," + label1.Text + ",");

            if(onlineList_LB.Items.Count >= 2)
            {
                switch (timerCount)
                {
                    case 0:
                        sendAll("message, 遊戲即將開始,");
                        break;
                    case 1:
                        sendAll("message, 遊戲開始,");
                        sendTo("yourTurn,", nextPlayer()); //開始
                        break;
                    //case 2:
                    //    sendAll("message, 倒數3秒,");
                    //    break;
                }
                timerCount++;
            }
        }
        #endregion

        #region Connection Breaking Function
        private void breakConnection()
        {
            try
            {
                Application.ExitThread();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion



        private void Log_LB_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        
    }
}
