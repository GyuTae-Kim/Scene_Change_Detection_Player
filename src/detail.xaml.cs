using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace WpfApp1
{
    /// <summary>
    /// detail.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class detail : System.Windows.Window
    {
        MainWindow mainWindow;
        // VideoWriter recodeSeparate;
        DispatcherTimer timer = new DispatcherTimer();
        Process process;

        public detail()
        {
            InitializeComponent();
        }

        public detail(MainWindow w)
        {
            InitializeComponent();
            if (w.Media.IsOpen)
            {
                FrameMoveBtn.IsEnabled = true;
                GetFrameBtn.IsEnabled = true;
                SaveMediaBtn.IsEnabled = true;
                CheckBranchBtn.IsEnabled = true;
            }
            else
            {
                FrameMoveBtn.IsEnabled = false;
                GetFrameBtn.IsEnabled = false;
                SaveMediaBtn.IsEnabled = false;
                CheckBranchBtn.IsEnabled = false;
            }

            if (w.frameArray.Count == 0)
            {
                SaveMediaBtn.IsEnabled = false;
                NextBranchBtn.IsEnabled = false;
                BeforeBranchBtn.IsEnabled = false;
            }
            mainWindow = w;
        }

        private void FrameMoveBtn_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan newTimeSpanas = new TimeSpan(0, 0, 0, 0, mainWindow.nowMillis(Int32.Parse(FrameTb.Text)));
            var milliSec = newTimeSpanas.TotalMilliseconds;
            var maximum = mainWindow.Media.MediaInfo.Duration.TotalMilliseconds;

            if (mainWindow.progressSlider.Maximum < milliSec)
            {
                mainWindow.progressSlider.Maximum = milliSec + 5000;

                if (mainWindow.progressSlider.Maximum > maximum)
                {
                    mainWindow.progressSlider.Maximum = maximum;
                }

                mainWindow.RenewProgressLabel();
                mainWindow.RenewSliderPoint();
            }

            else if (mainWindow.progressSlider.Minimum > milliSec)
            {
                mainWindow.progressSlider.Minimum = milliSec - 5000;

                if (mainWindow.progressSlider.Minimum < 0)
                {
                    mainWindow.progressSlider.Minimum = 0;
                }

                mainWindow.RenewProgressLabel();
                mainWindow.RenewSliderPoint();
            }

            mainWindow.progressSlider.Value = milliSec;
            mainWindow.Media.Position = newTimeSpanas;
            mainWindow.ReleaseNowFrame();
        }

        private void getFrame()
        {
            FileInfo del = new FileInfo("frames.ini");
            del.Delete();

            string path = "python practice.py " + mainWindow.filePath + " " + Threshold1.Text + " " + value.Text;
            ProcessStartInfo cmd = new ProcessStartInfo();
            process = new Process();
            cmd.FileName = @"cmd";
            cmd.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.CreateNoWindow = true;

            cmd.UseShellExecute = false;
            cmd.RedirectStandardInput = true;

            process.EnableRaisingEvents = false;
            process.StartInfo = cmd;
            process.Start();
            process.StandardInput.Write(path + Environment.NewLine);

            timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(checkEnd);

            timer.Start();
        }

        private void checkEnd(object sender, EventArgs e)
        {
            if (File.Exists("frames.ini"))
            {
                timer.Stop();
                setFramePick();
                process.Close();
                SaveMediaBtn.IsEnabled = true;
                NextBranchBtn.IsEnabled = true;
                BeforeBranchBtn.IsEnabled = true;
                CheckBranchBtn.IsEnabled = true;
                MessageBox.Show("장면이 분할 되었습니다.", "Success");
                mainWindow.processingLabel.Content = "";
            }
        }

        private void setFramePick()
        {
            StreamReader sr = new StreamReader("frames.ini");
            // 다시 프레임 배열에 받아오기 ㅇㅇ
            mainWindow.frameArray.Clear();
            while (sr.Peek() >= 0)
            {
                mainWindow.frameArray.Add(sr.ReadLine());
            }
            for (int i = 0; i < mainWindow.frameArray.Count; i++)
            {
                mainWindow.MakeSliderPoint(new TimeSpan(0, 0, 0, 0, mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i]))));
            }
            sr.Close();
        }

        private void GetFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            // 이미 백그라운드 작업 중일 경우
            if (mainWindow.processingLabel.Content.ToString() != "")
            {
                MessageBox.Show("이미 다른 작업이 진행 중입니다.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 라벨로 작업 진행 중인 것을 표기
            mainWindow.processingLabel.Content = "Running OpenCV";

            mainWindow.PointCanvas.Children.Clear();
            mainWindow.sliderPointTime.Clear();
            mainWindow.sliderPoint.Clear();

            getFrame();
        }

        private void SaveMediaBtn_Click(object sender, RoutedEventArgs e)
        {
            // ffmpeg 사용 (소리 ON)
            mainWindow.separateVideo();

            /* 오픈CV 사용
            recodeSeparate = new VideoWriter("./" + "1" + ".avi", FourCC.XVID, mainWindow.nowFps(), mainWindow.nowSize());
            for(int i = 0; i < Int32.Parse(mainWindow.frameArray[0]); i++)
            {
                recodeSeparate.Write(mainWindow.getFrame(i));
            }
            */
        }

        // 다음
        private void NextBranchBtn_Click(object sender, RoutedEventArgs e)
        {
            var milliSec = 0;
            var maximum = mainWindow.Media.MediaInfo.Duration.TotalMilliseconds;

            for (int i = 0; i < mainWindow.frameArray.Count; i++)
            {
                if (mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i])) > Double.Parse(mainWindow.Media.FramePosition.TotalMilliseconds.ToString()))
                {
                    mainWindow.Media.Position = new TimeSpan(0, 0, 0, 0, mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i])));

                    // 추가: 프레임 확대기능과 충돌현상 해결
                    milliSec = mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i]));
                    if (mainWindow.progressSlider.Maximum < milliSec)
                    {
                        mainWindow.progressSlider.Maximum = milliSec + 5000;
                        if (mainWindow.progressSlider.Maximum > maximum)
                        {
                            mainWindow.progressSlider.Maximum = maximum;
                        }
                        mainWindow.RenewProgressLabel();
                        mainWindow.RenewSliderPoint();
                    }
                    mainWindow.progressSlider.Value = milliSec;
                    mainWindow.ReleaseNowFrame();

                    return;
                }

                if (i == mainWindow.frameArray.Count - 1) MessageBox.Show("마지막 분기점입니다.", "Error");
            }
        }

        // 이전
        private void BeforeBranchBtn_Click(object sender, RoutedEventArgs e)
        {
            var milliSec = 0;

            for (int i = mainWindow.frameArray.Count - 1; i >= 0; i--)
            {
                if (mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i])) < Double.Parse(mainWindow.Media.FramePosition.TotalMilliseconds.ToString()))
                {
                    mainWindow.Media.Position = new TimeSpan(0, 0, 0, 0, mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i])));

                    // 추가: 프레임 확대기능과 충돌현상 해결
                    milliSec = mainWindow.nowMillis(Int32.Parse(mainWindow.frameArray[i]));
                    if (mainWindow.progressSlider.Minimum > milliSec)
                    {
                        mainWindow.progressSlider.Minimum = milliSec - 5000;
                        if (mainWindow.progressSlider.Minimum < 0)
                        {
                            mainWindow.progressSlider.Minimum = 0;
                        }
                        mainWindow.RenewProgressLabel();
                        mainWindow.RenewSliderPoint();
                    }
                    mainWindow.progressSlider.Value = milliSec;
                    mainWindow.ReleaseNowFrame();

                    return;
                }

                if (i == 0) MessageBox.Show("첫 분기점입니다.", "Error");
            }
        }

        private void window_Activated(object sender, EventArgs e)
        {
            mainWindow.Topmost = true;
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            mainWindow.Topmost = false;
            Topmost = false;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // VGG Keras Use
        private void CheckBranchBtn_Click(object sender, RoutedEventArgs e)
        {
            // 이미 진행 중인 작업이 있을 경우
            if (mainWindow.processingLabel.Content.ToString() != "")
            {
                MessageBox.Show("이미 다른 작업이 진행 중입니다.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 진행 상황 라벨로 표시
            mainWindow.processingLabel.Content = "Running VGG";

            mainWindow.PointCanvas.Children.Clear();
            mainWindow.sliderPointTime.Clear();
            mainWindow.sliderPoint.Clear();

            FileInfo del = new FileInfo("frames.ini");
            del.Delete();

            string path = "python practice2.py " + mainWindow.filePath + " " + Threshold2.Text + " " + value2.Text;
            ProcessStartInfo cmd = new ProcessStartInfo();
            process = new Process();
            cmd.FileName = @"cmd";
            cmd.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.CreateNoWindow = true;

            cmd.UseShellExecute = false;
            cmd.RedirectStandardInput = true;

            process.EnableRaisingEvents = false;
            process.StartInfo = cmd;
            process.Start();
            process.StandardInput.Write(path + Environment.NewLine);

            timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(checkEnd);

            timer.Start();
        }

        private void BranchMakebtn_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.frameArray.Add(mainWindow.getNowFrame().ToString());
            mainWindow.frameArray.Sort();
            mainWindow.MakeSliderPoint(new TimeSpan(0, 0, 0, 0, mainWindow.nowMillis(mainWindow.getNowFrame())));
        }

        private void Savebtn_Click(object sender, RoutedEventArgs e)
        {
            FileInfo del = new FileInfo("frames.ini");
            del.Delete();

            StreamWriter wr = new StreamWriter("frames.ini");
            for(int i = 0; i < mainWindow.frameArray.Count; i++) wr.WriteLine(mainWindow.frameArray[i]);
            wr.Close();
        }
    }
}