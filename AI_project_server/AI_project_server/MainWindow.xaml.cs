﻿using System.Windows;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using MySql.Data.MySqlClient;

namespace AI_project_server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string bindIP = "10.10.20.113";
        private const int bindPort = 9195;
        private TcpListener server = null;
        private IPEndPoint localAddress;
        private TcpClient client;
        private NetworkStream stream;
        private NetworkStream rev_stream;

        //db용
        private MySqlConnection sql;

        private object lockObject = new object();
        FileStream read_file, send_to_python;
        string[] divide;//구분용
        string msg;//받아온 데이터 변환용
        //파이썬 스트림용
        string to_python, line_name;

        int fileLength;//파일 길이용
        byte[] buffer;//파일 데이터용

        int numbering = 0, num_test = 0;//테스트용

        //listview용 리스트
        string date;//listview 날짜용
        List<string> pass_result = new List<string>();
        List<string> defect_result = new List<string>();

        //클라구분용
        Dictionary<string, NetworkStream> client_Distinguish = new Dictionary<string, NetworkStream>();

        public Dictionary<string, NetworkStream> Client_Distinguish { get => client_Distinguish; set => client_Distinguish = value; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Open_btn_Click(object sender, RoutedEventArgs e)
        {
            //서버열기
            //listen
            try
            {
                localAddress = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);
                server = new TcpListener(localAddress);
                //server.Start();
                MessageBox.Show("Server Open");
                //Thread t1 = new Thread(new ThreadStart(Connect));
                //t1.Start();
                //Connect();
                string connectstring = "UID=root; PWD=1234; Server=127.0.0.1; port=3306; Database=image_result";
                sql = new MySqlConnection(connectstring);
                sql.Open();//db 열고
                Handle_client();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류:{ex.Message}");
                server.Stop();
            }
        }
        private async void Handle_client()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    server.Start();
                    await Task.Delay(1000);
                    client = server.AcceptTcpClient();
                    //stream = client.GetStream();
                    IPEndPoint getIP = (IPEndPoint)client.Client.RemoteEndPoint;
                    string IP = getIP.Address.ToString();
                    Dispatcher.Invoke(() =>
                    {
                        IPstate.AppendText("입장 아이피 : " + IP + "\n");
                    });
                    Thread t1 = new Thread(new ThreadStart(Connected_client));
                    t1.Start();

                }
            });
        }
        //스레드라서 그런가
        private void Connected_client()
        {
            while (true)
            {
                int length;
                byte[] data = new byte[1024];

                while (true)//파일크기 받고 파일받기
                {
                    try
                    {
                        length = stream.Read(data, 0, data.Length);

                        //데이터 받고 구분자로 파이썬, WPF 구분하기
                        msg = Encoding.Default.GetString(data);
                        //MessageBox.Show(msg);
                        divide = msg.Split('/');
                        //stream.Write(data, 0, data.Length);
                        if (divide[0] == "1")
                        {
                            //WPF client 정보저장하기
                            Client_Distinguish.Add(divide[1], stream);
                            line_name = divide[1];
                            //db연동 테스트
                            //string qry = "INSERT INTO result(NO, Time, P/F, Cause)" +
                            //            "VALUES(" + "@NO, @Time, @P/F, @Cause" + ");";
                            //using (MySqlCommand cmd = sql.CreateCommand())
                            //{
                            //    cmd.CommandText = qry;
                            //    cmd.Parameters.Add("@NO", MySqlDbType.Int32);
                            //    cmd.Parameters.Add("@Time", MySqlDbType.VarChar);
                            //    cmd.Parameters.Add("@[/F", MySqlDbType.VarChar);
                            //    cmd.Parameters.Add("@Cause", MySqlDbType.VarChar);

                            //    cmd.Parameters["@NO"].Value = "1";
                            //    cmd.Parameters["@NO"].Value = "2";
                            //    cmd.Parameters["@NO"].Value = "3";
                            //    cmd.Parameters["@NO"].Value = "4";

                            //    cmd.ExecuteNonQuery();//?
                            //}
                            //파일수신 및 파이썬한테 전송하기
                            rev_stream = client.GetStream();
                        }
                        else if (divide[0] == "3")//계속 3이 들어와서
                        {
                            //여기?
                            //Image_file(rev_stream);
                            //MessageBox.Show("3들어옴");//파이썬 연결되면 안들어옴 stream이 파이썬이랑 연결되서 그런가
                            byte[] size = new byte[4];
                            rev_stream.Read(size, 0, size.Length);
                            fileLength = BitConverter.ToInt32(size, 0);
                            //MessageBox.Show(fileLength.ToString() + "wpf클라 파일 수신확인용");
                            read_file = new FileStream("../../WPF_read_image/" + numbering + ".png", FileMode.Create, FileAccess.Write);
                            numbering++;
                            //¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿
                            BinaryWriter write = new BinaryWriter(read_file);

                            buffer = new byte[fileLength];
                            rev_stream.Read(buffer, 0, buffer.Length);
                            write.Write(buffer, 0, buffer.Length);
                            read_file.Close();
                            Array.Clear(buffer, 0, buffer.Length);
                            
                        }
                        else if (divide[0] == "5")
                        {
                            //python 정보저장하기
                            Client_Distinguish.Add(divide[1], stream);
                        }
                        else if (divide[0] == "6")//검사결과 수신//pass일 때
                        {
                            //디비 저장할 거
                            //번호/시간(초까지)/검사결과(P/F)/불량인이유 없으면 x
                            //string qry = "INSERT INTO result("+"NO, Time, P/F, Cause"+")" +
                            //    "         VALUES("+"@NO, @Time, @P/F, @Cause"+");";
                            //using (MySqlCommand cmd = sql.CreateCommand())
                            //{
                            //    cmd.CommandText = qry;
                            //    cmd.Parameters.Add("@NO", MySqlDbType.Int32);
                            //    cmd.Parameters.Add("@Time", MySqlDbType.VarChar);
                            //    cmd.Parameters.Add("@[/F", MySqlDbType.VarChar);
                            //    cmd.Parameters.Add("@Cause", MySqlDbType.VarChar);

                            //    cmd.Parameters["@NO"].Value ="1";
                            //    cmd.Parameters["@NO"].Value ="2";
                            //    cmd.Parameters["@NO"].Value ="3";
                            //    cmd.Parameters["@NO"].Value ="4";

                            //    cmd.ExecuteNonQuery();
                            //}
                            MessageBox.Show("6번 진입");
                            //리스트뷰에 띄우기
                            //ui 건드리는 스레드가 아닌 애가 건드리려고 해서 오류남
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                //Result_Log.Items.Insert(numbering, test);
                            }));
                            numbering++;
                            //검사결과 및 파일수신하기, DB저장하기

                            //결과 wpf 클라한테 보내주기
                            byte[] result = new byte[128];
                            result = Encoding.Default.GetBytes(divide[1]);//결과
                            //stream.Write(result, 0, result.Length);
                        }
                        else if (divide[0] == "7")//검사결과 불량일 때 사진도 받고 이미지에 사진 띄우기
                        {
                            MessageBox.Show("7번 진입");//??
                            //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            //{
                            //    Result_Log.Items.Insert(numbering, test);
                            //}));
                            numbering++;
                            //결과 wpf 클라한테 보내주기
                            byte[] result = new byte[128];
                            result = Encoding.Default.GetBytes(divide[1]);//결과
                            //stream.Write(result, 0, result.Length);

                        }
                        else if (divide[0] == "2")
                        {
                            //MessageBox.Show("파이썬 중복 확인 50번 진입");
                            Client_Distinguish.Add(divide[1], stream);
                            to_python = divide[1];
                            Send_python();
                            //Thread send_p = new Thread(new ThreadStart(Send_python));
                            //send_p.Start();
                        }
                    }
                    catch (Exception s)
                    {
                        MessageBox.Show(s.ToString());
                    }
                }
            }
        }

        private async void Image_file(NetworkStream wpf_Stream)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(1000);
                
            });
        }

        private async void Send_python()//저장된 파일 열어서 보내주기
        {
            NetworkStream stream_python = Client_Distinguish[to_python];
            try
            {
                while (true)
                {
                    await Task.Delay(10000);
                    // 파일 크기 전송
                    byte[] file_size = new byte[4];
                    send_to_python = new FileStream("../../WPF_read_image/" + num_test + ".png", FileMode.Open, FileAccess.Read);//
                    num_test++;
                    //int test_length = (int)send_to_python.Length;
                    int test_length = (int)send_to_python.Length;
                    //string str_length = test_length.ToString();
                    file_size = BitConverter.GetBytes(test_length);
                    //file_size = Encoding.Default.GetBytes(str_length);
                    stream_python.Write(file_size, 0, file_size.Length);
                    MessageBox.Show(test_length.ToString() + "파일크기용");

                    byte[] file_data = new byte[test_length];//outofmemory??

                    send_to_python.Read(file_data, 0, file_data.Length);//파일읽어서 배열에 넣고
                    stream_python.Write(file_data, 0, file_data.Length);//송신
                    MessageBox.Show("파일전송완료");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 전송 오류: " + ex.ToString());
            }
        }

        private async void Pass_receive()
        {
            await Task.Run(async () =>
            {
                await Task.Delay(1000);
                NetworkStream stream_python = client_Distinguish[to_python];
                byte[] result = new byte[1024];
                stream_python.Read(result, 0, result.Length);
                //여기서 받아서 리스트뷰 채우기
                //pass defect 구별하고

            });
        }

        //private async void Defect_receive()//얘는 이미지도
        //{
        //    await Task.Run(async () =>
        //    {
        //        await Task.Delay(1000);
        //        NetworkStream stream_python = client_Distinguish[to_python];
        //        byte[] result = new byte[1024];
        //        stream_python.Read(result, 0, result.Length);//결과 먼저 수신받고 파일받기
        //        //결과 수신받고 리스트뷰 채우기


        //        //파일 사이즈용
        //        byte[] image_size = new byte[4];
        //        stream_python.Read(image_size, 0, image_size.Length);
        //        int file_size = BitConverter.ToInt32(image_size, 0);

        //        //파일 만들고
        //        FileStream defect_image = new FileStream("../../defect_image/" + numbering + ".png", FileMode.Create, FileAccess.Write);
        //        //수신
        //        byte[] file_data = new byte[file_size];
        //        BinaryWriter write = new BinaryWriter(defect_image);
        //        stream_python.Read(file_data, 0, file_data.Length);
        //        write.Write(file_data, 0, file_data.Length);
        //        //이미지 수신하고 image에 사진 넣어주기
        //    });
        //}

    }
}