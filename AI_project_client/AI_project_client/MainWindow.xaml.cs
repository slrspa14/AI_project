using System;
using System.Windows;
using System.Net.Sockets;
using System.Threading.Tasks;
// OpenCV 사용을 위한 using
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
// Timer 사용을 위한 using
using System.Windows.Threading;
using System.Text;

namespace AI_project_client
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        VideoCapture cam;
        Mat frame;
        DispatcherTimer timer;
        bool is_initCam, is_initTimer;
        public NetworkStream stream;//통신스트림
        public TcpClient client = new TcpClient();//연결소켓
        byte[] message = new byte[10];
        //검사결과용
        private int pass_cnt = 0, defect_cnt = 0, length;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Windows_loaded(object sender, RoutedEventArgs e)
        {
            // 카메라, 타이머(0.01ms 간격) 초기화
            is_initCam = Init_camera();
            is_initTimer = Init_Timer(0.01);

            // 초기화 완료면 타이머 실행
            if (is_initTimer && is_initCam) timer.Start();
        }

        private bool Init_Timer(double interval_ms)
        {
            try
            {
                timer = new DispatcherTimer();

                timer.Interval = TimeSpan.FromMilliseconds(interval_ms);
                timer.Tick += new EventHandler(Timer_tick);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Init_camera()
        {
            try
            {
                // 0번 카메라로 VideoCapture 생성 (카메라가 없으면 안됨)
                cam = new VideoCapture(0);
                if(!cam.IsOpened())
                {
                    MessageBox.Show("웹캠을 열 수 없습니다.");
                    Environment.Exit(0);//강제종료
                }
                cam.FrameHeight = (int)Cam.Height;
                cam.FrameWidth = (int)Cam.Width;

                // 카메라 영상을 담을 Mat 변수 생성
                frame = new Mat();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Server_connect_Click(object sender, RoutedEventArgs e)
        {
            //연결소켓
            //client = new TcpClient();
            try
            {
                client.Connect("127.0.0.1", 9199);
                if (client.Connected)
                {
                    MessageBox.Show("접속 성공");
                    string information = "1/1번라인";
                    stream = client.GetStream();
                    message = Encoding.Default.GetBytes(information);
                    stream.Write(message, 0, message.Length);
                    File_send(client);
                    Receive_result(client);
                }
                else
                    MessageBox.Show("접속 실패");
            }
            catch(Exception c)
            {
                MessageBox.Show("오류");
                Console.WriteLine(c);
            }
        }

        private void Timer_tick(object sender, EventArgs e)
        {
            // 0번 장비로 생성된 VideoCapture 객체에서 frame을 읽어옴
            cam.Read(frame);
            // 읽어온 Mat 데이터를 Bitmap 데이터로 변경 후 컨트롤에 그려줌
            Cam.Source = WriteableBitmapConverter.ToWriteableBitmap(frame);
            
        }

        //실시간 캡처본
        private async void File_send(TcpClient client)
        {
            stream = client.GetStream();
            await Task.Run(async() =>
               {
                   while (true)
                   {
                       await Task.Delay(5000);
                       byte[] file_ready = new byte[4];
                       file_ready = Encoding.Default.GetBytes("3/불량검출");//보내서 서버에서 파일받을 준비하라고
                       stream.Write(file_ready, 0, file_ready.Length);
                       Mat capture_image = new Mat();
                       cam.Read(capture_image);

                       byte[] file_length = new byte[4];
                       file_length = BitConverter.GetBytes(capture_image.ToBytes().Length);
                       stream.Write(file_length, 0, file_length.Length);//파일크기 보내주고
                       //MessageBox.Show("파일크기 보내고");
                       byte[] file_data = new byte[capture_image.ToBytes().Length];//파일크기 배열 생성
                       file_data = capture_image.ToBytes();
                       stream.Write(file_data, 0, file_data.Length);
                       MessageBox.Show("WPF client -> WPF server");
                       //Array.Clear(file_ready, 0, file_ready.Length);
                       //Array.Clear(file_length, 0, file_length.Length);
                       //Array.Clear(file_data, 0, file_data.Length);
                       //break;
                   }
               });
        }
        private async void Receive_result(TcpClient client)//되려나
        {
            stream = client.GetStream();
            await Task.Run(() =>
                {
                    byte[] recv_result = new byte[256];
                    while ((length = stream.Read(recv_result, 0, recv_result.Length)) != 0)//결과만 수신할거니깐
                    {
                        //결과 라벨에 띄우기
                        string result = Encoding.Default.GetString(recv_result);
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            result_label.Content = result;//라벨
                        }));
                        //MessageBox.Show(result);
                    }
                    client.Close();
                    stream.Close();
                });
        }
    }
}