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
using System.Text.RegularExpressions; //문자열안에 숫자 추출
using System.Diagnostics;

namespace Marble
{
    public partial class Form1 : Form
    {
        TcpListener server = null; // 서버
        TcpClient clientSocket = null; // 소켓
        static int counter = 0; // 사용자 수
        string date; // 날짜 

        // 각 클라이언트 마다 리스트에 추가

        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();

        List<string> player_game_name = new List<string>();
        //bool[] player_game_ready = new bool[4];
        int[] player_position = new int[4];
        int player_ready_total = 0;
        bool Is_Game_Start = false;
        int current_turn = 0;

        Board board;
        List<Land> lands;

        Random random = new Random();

        public Form1()
        {
            InitializeComponent();
            // 쓰레드 생성
            player_game_name.Add("");
            player_game_name.Add("");
            player_game_name.Add("");
            player_game_name.Add("");
            Thread t = new Thread(InitSocket);
            t.IsBackground = true;
            t.Start();
        }

        private void InitSocket()

        {

            server = new TcpListener(IPAddress.Any, 9999); // 서버 접속 IP, 포트

            clientSocket = default(TcpClient); // 소켓 설정

            server.Start(); // 서버 시작

            DisplayText(">> Server Started");



            while (true)

            {
                if (counter == 4)
                {

                }
                else
                {
                    try

                    {

                        clientSocket = server.AcceptTcpClient(); // client 소켓 접속 허용

                        DisplayText(">> Accept connection from client");



                        NetworkStream stream = clientSocket.GetStream();

                        byte[] buffer = new byte[1024]; // 버퍼

                        int bytes = stream.Read(buffer, 0, buffer.Length);

                        string user_name = Encoding.Unicode.GetString(buffer, 0, bytes);

                        user_name = user_name.Substring(0, user_name.IndexOf("$")); // client 사용자 명
                        player_game_name[counter] = user_name;


                        clientList.Add(clientSocket, user_name); // client 리스트에 추가

                        counter++; // Client 수 증가

                        SendMessageAll(user_name + " 님이 입장하셨습니다.", "", false); // 모든 client에게 메세지 전송



                        handleClient h_client = new handleClient(); // 클라이언트 추가

                        h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);

                        h_client.OnDisconnected += new handleClient.DisconnectedHandler(h_client_OnDisconnected);

                        h_client.startClient(clientSocket, clientList);

                    }

                    catch (SocketException se) { break; }

                    catch (Exception ex) { break; }
                }



            }



            clientSocket.Close(); // client 소켓 닫기

            server.Stop(); // 서버 종료

        }



        void h_client_OnDisconnected(TcpClient clientSocket) // client 접속 해제 핸들러

        {

            if (clientList.ContainsKey(clientSocket))

                clientList.Remove(clientSocket);

        }



        private void OnReceived(string message, string user_name) // client로 부터 받은 데이터
        {
            Debug.WriteLine(message);
            string[] parsed = message.Split(",");

            if (message.Equals("leaveChat"))
            {
                string displayMessage = "leave user : " + user_name;
                player_game_name.RemoveAt(player_game_name.FindIndex(s => s.Equals(user_name)));
                --counter;
                DisplayText(displayMessage);
                SendMessageAll("leaveChat", user_name, true);
            }
            else if (parsed[0] == "(SYSTEMCOMMAND)")
            {
                int current_account = 0;
                try
                {
                    current_account = int.Parse(parsed[3]);
                }
                catch (Exception)
                {

                }

                switch (parsed[1])
                {
                    case "ROLL":
                        {
                            if (Is_Game_Start == true)
                            {
                                if (player_game_name[current_turn] == user_name)
                                {
                                    int dice_number = random.Next(1, 13);
                                    string displayMessage = "Player : " + user_name + "님께서 주사위를 굴렸습니다.\r\n 주사위 숫자는 ... " + dice_number.ToString() + " 입니다.";

                                    int current_index = int.Parse(parsed[4]) + dice_number;
                                    DisplayText(displayMessage);

                                    SendMessageAll("(SYSTEMCOMMAND),ROLLR," + user_name + "," + parsed[3] + "," + current_index, "COMMAND", false);

                                    if (current_index / 32 == 1)
                                    {
                                        SendMessageAll(user_name + " 님께서 출발지를 통과하여 월급을 받습니다. (+300000)", "ADMIN", true);
                                        current_account += 300000;
                                        current_index -= 32;
                                    }

                                    if (lands[current_index].getOwner() != "" && lands[current_index].getOwner() != user_name)
                                    {
                                        int pay = lands[current_index].getPayPrice();
                                        SendMessageAll(user_name + " 님께서 타인의 땅을 밟아 통행료를 지불합니다. (-" + pay.ToString() + ")", "ADMIN", true);
                                        current_account -= pay;
                                        SendMessageAll("(SYSTEMCOMMAND),MONEYADD," + user_name + "," + pay, "COMMAND", false);
                                    }

                                    if (current_turn + 1 == counter)
                                    {
                                        current_turn = 0;
                                    }
                                    else
                                    {
                                        current_turn += 1;
                                    }

                                    SendMessageAll("(SYSTEMCOMMAND),TURNR," + player_game_name[current_turn], "COMMAND", false);

                                }
                                else
                                {
                                    DisplayText("주사위를 굴렸지만 현재 자신의 턴이 아닙니다.");
                                }
                            }
                            break;
                        }

                    case "READY":
                        {
                            player_ready_total += 1;
                            string displayMessage = "Player : " + user_name + "님께서 준비완료를 누르셨습니다.";
                            DisplayText(displayMessage);
                            SendMessageAll("(SYSTEMCOMMAND),READYR," + user_name, "COMMAND", false);
                            if (player_ready_total == counter)
                            {
                                string GAME_START = " - 모든 플레이어가 준비를 마쳤습니다. -\r\n - 게임을 시작합니다!!! -\r\n";
                                SendMessageAll(GAME_START, "ADMIN", false);

                                board = new Board();
                                lands = board.lands;
                                SendMessageAll("(SYSTEMCOMMAND),STARTR", "COMMAND", false);
                                for (int i = 0; i < counter; i++)
                                {
                                    SendMessageAll("게임의 순서는 " + (i + 1).ToString() + "번째 " + player_game_name[i] + " 입니다.\r\n", "ADMIN", false);
                                }
                                SendMessageAll("(SYSTEMCOMMAND),TURNR," + player_game_name[0], "COMMAND", false);
                                Is_Game_Start = true;
                            }
                            break;
                        }

                    case "CANCELL":
                        {
                            player_ready_total -= 1;
                            string displayMessage = "Player : " + user_name + "님께서 준비를 취소하셨습니다.";
                            DisplayText(displayMessage);
                            SendMessageAll("(SYSTEMCOMMAND),CANCELLR," + user_name, "COMMAND", false);
                            break;
                        }

                    default:
                        break;
                }
            }
            else
            {
                string displayMessage = "From client : " + user_name + " : " + message;

                DisplayText(displayMessage); // Server단에 출력

                SendMessageAll(message, user_name, true); // 모든 Client에게 전송
            }

        }



        public void SendMessageAll(string message, string user_name, bool flag)
        {
            foreach (var pair in clientList)
            {
                date = DateTime.Now.ToString("MM.dd. HH:mm"); // 현재 날짜 받기
                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    if (message.Equals("leaveChat"))
                        buffer = Encoding.Unicode.GetBytes(user_name + " 님이 게임을 나갔습니다." + "$");
                    else
                        buffer = Encoding.Unicode.GetBytes("[" + date + "] " + user_name + " : " + message + Environment.NewLine + "$");
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(message + "$");
                }

                stream.Write(buffer, 0, buffer.Length); // 버퍼 쓰기
                stream.Flush();
            }
        }



        private void DisplayText(string text) // Server 화면에 출력

        {

            if (textBox1.InvokeRequired)

            {

                textBox1.BeginInvoke(new MethodInvoker(delegate

                {

                    textBox1.AppendText(text + Environment.NewLine);

                }));

            }

            else

                textBox1.AppendText(text + Environment.NewLine);

        }

    }



}
