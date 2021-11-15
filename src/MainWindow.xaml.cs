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
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public string filePath;
        VideoCapture captured;
        public double cvFrame;
        bool isMediaOpened = false;
        bool isRun = false;
        bool isAutoMove = false;
        detail DetailForm;

        public List<string> frameArray = new List<string>();
        public List<Rectangle> sliderPoint = new List<Rectangle>();
        public List<TimeSpan> sliderPointTime = new List<TimeSpan>();
        DispatcherTimer timer = new DispatcherTimer();          // For Process Slider
        DispatcherTimer timer2 = new DispatcherTimer();         // For Animation
        Image frameContainer = new Image();

        DoubleAnimation fadeout = new DoubleAnimation();                            //    21,49,50,106,107,140~,249~ 줄 수정<컨트롤박스 사라지는거 페이드인,아웃으로 변경>
        DoubleAnimation fadein = new DoubleAnimation();

        public MainWindow()
        {
            InitializeComponent();
            Unosquare.FFME.Library.FFmpegDirectory = @"c:\ffmpeg" + (Environment.Is64BitProcess ? @"\x64" : string.Empty);
        }
        
        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isMediaOpened && !isRun)
            {
                Media.Play();
                PlayBtnIcon.Data = Geometry.Parse("M18.51899,0L29.981999,0 29.981999,32 18.51899,32z M0,0L11.464992,0 11.464992,32 0,32z");
                timer.Start();
                isRun = true;
                ReleaseNowFrame();
            }

            else if (isMediaOpened && isRun)
            {
                Media.Pause();
                PlayBtnIcon.Data = Geometry.Parse("M0,0L22.652,15.996998 0,31.999996z");
                timer.Stop();
                isRun = false;
                ReleaseNowFrame();
            }

        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearMediaData(true);
            ReleaseNowFrame();
            RenewProgressLabel();
            RenewSliderPoint();
            OpenInfoButton.IsChecked = false;
            if (!(DetailForm is null))
            {
                DetailForm.Close();
                DetailForm = null;
            }
        }
        
        private void Media_MediaOpened(object sender, Unosquare.FFME.Common.MediaOpenedEventArgs e)
        {
            ClearMediaData(false);
            fadeout = new DoubleAnimation(0, TimeSpan.FromMilliseconds(1500));
            fadein = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));//페이드인 페이드아웃 추가

            frameArray.Clear();
            isMediaOpened = true;

            OpenInfoButton.IsEnabled = true;
            OpenInfoButtonIcon.Opacity = 1;
            OpenInfoButton.IsChecked = false;
            if (!(DetailForm is null))
            {
                DetailForm.Close();
                DetailForm = null;
            }

            int bef = sliderPoint.Count;
            foreach (Rectangle p in sliderPoint)
                PointCanvas.Children.Remove(p);

            sliderPoint.Clear();
            sliderPointTime.Clear();

            progressSlider.Maximum = captured.FrameCount / captured.Fps * 1000;
            RenewProgressLabel();
            ReleaseNowFrame();
            Media.Volume = VolumeSlider.Value / 100;

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(progressSliderMove);
        }

        void hideControl(object sender, EventArgs e)
        {

            ControlBoxGrid.BeginAnimation(OpacityProperty, fadeout);
            SliderBoxGrid.BeginAnimation(OpacityProperty, fadeout);
            controlBackground.BeginAnimation(OpacityProperty, fadeout);

            timer2.Stop();
        }

        private void DoubleAnimation_Completed(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Media_MediaReady(object sender, EventArgs e)
        {
            NowFrameLabel.Content = cvFrame;
            timer2.Interval = TimeSpan.FromMilliseconds(5000);
            timer2.Tick += new EventHandler(hideControl);
            timer2.Start();
        }

        private void LoadFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog()
            {
                DefaultExt = ".avi",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                filePath = dlg.FileName;
                Media.Source = new Uri(filePath);
                captured = new VideoCapture(filePath);
                cvFrame = captured.FrameCount;
            }
        }

        private void FrameTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        void progressSliderMove(object sender, EventArgs e)
        {
            TimeSpan newTimeSpan = Media.Position;
            double newVal = newTimeSpan.TotalMilliseconds;
            double max = progressSlider.Maximum;

            if (newVal > progressSlider.Maximum)
            {
                max += 1000;

                if (max > Media.MediaInfo.Duration.TotalMilliseconds)
                {
                    max = Media.MediaInfo.Duration.TotalMilliseconds;
                }
                RenewProgressLabel();
            }
            progressSlider.Maximum = max;
            isAutoMove = true;
            progressSlider.Value = newTimeSpan.TotalMilliseconds;
            isAutoMove = false;
            progressLabel.Content = newTimeSpan.ToString(@"hh\:mm\:ss");
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan newTimeSpan = new TimeSpan(0, 0, 0, 0, (int)progressSlider.Value);
            progressLabel.Content = newTimeSpan.ToString(@"hh\:mm\:ss");
        }

        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan newTimeSpan = new TimeSpan(0, 0, 0, 0, (int)progressSlider.Value);
            if (!isAutoMove)
            {
                Media.Position = newTimeSpan;
            }
            progressLabel.Content = newTimeSpan.ToString(@"hh\:mm\:ss");
            ReleaseNowFrame();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {

            ControlBoxGrid.BeginAnimation(OpacityProperty, fadein);
            SliderBoxGrid.BeginAnimation(OpacityProperty, fadein);
            controlBackground.BeginAnimation(OpacityProperty, fadein);

            if (timer2 != null)
            {
                timer2.Stop();
                timer2.Start();
            }
        }
        
        private void Window_Closed(object sender, EventArgs e)
        {
            ClearMediaData(true);
            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            this.Close();
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            RenewSliderPoint();
        }
        
        private void OpenInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpenInfoButton.IsChecked == true)
            {
                DetailForm = new detail(this);
                DetailForm.Closed += DetailForm_Closed;
                DetailForm.Show();
                MoveDetailFormLocation();
                DetailFormDataSetting();

                progressSlider.Minimum = 0;
                progressSlider.Maximum = Media.MediaInfo.Duration.TotalMilliseconds;
                RenewProgressLabel();
                RenewSliderPoint();
            }

            else
            {
                DetailForm.Close();
            }
        }

        private void DetailForm_Closed(object sender, EventArgs e)
        {
            DetailForm = null;
            OpenInfoButton.IsChecked = false;
        }
        
        private void progressSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Point mousePos;
            double min, max, posInTime;
            double deltaMin, deltaMax;
            double aftMin, aftMax;

            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && isMediaOpened)
            {
                mousePos = Mouse.GetPosition(progressSlider);
                min = progressSlider.Minimum;
                max = progressSlider.Maximum;
                posInTime = (max - min) / progressSlider.ActualWidth * mousePos.X + min;
                deltaMin = (posInTime - min) * 0.1;
                deltaMax = (max - posInTime) * 0.1;
                // Wheel Up(Zoom)
                if (e.Delta > 0)
                {
                    aftMin = min + deltaMin;
                    aftMax = max - deltaMax;

                    if (aftMin > progressSlider.Value)
                    {
                        aftMax -= progressSlider.Value - aftMin;
                        aftMin = progressSlider.Value;

                        if (aftMax > Media.MediaInfo.Duration.TotalMilliseconds)
                        {
                            aftMax = Media.MediaInfo.Duration.TotalMilliseconds;
                        }
                    }
                    else if (aftMax < progressSlider.Value)
                    {
                        aftMin += aftMax - progressSlider.Value;
                        aftMax = progressSlider.Value;

                        if (aftMin < 0)
                        {
                            aftMin = 0;
                        }
                    }

                    progressSlider.Minimum = aftMin;
                    progressSlider.Maximum = aftMax;
                }
                // Wheel Down
                else if (e.Delta < 0)
                {
                    aftMin = min - deltaMin;
                    aftMax = max + deltaMax;

                    if (aftMin < 0)
                    {
                        aftMin = 0;
                    }
                    if (aftMax > Media.MediaInfo.Duration.TotalMilliseconds)
                    {
                        aftMax = Media.MediaInfo.Duration.TotalMilliseconds;
                    }

                    progressSlider.Minimum = aftMin;
                    progressSlider.Maximum = aftMax;
                }
                RenewProgressLabel();
                RenewSliderPoint();
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            MoveDetailFormLocation();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MoveDetailFormLocation();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!(DetailForm is null))
            {
                DetailForm.Close();
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isMediaOpened)
            {
                Media.Volume = VolumeSlider.Value / 100;
                VolumeLabel.Text = VolumeSlider.Value.ToString();
            }
        }

        private void VolumeSlider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Media.Volume = VolumeSlider.Value / 100;
            VolumeLabel.Text = VolumeSlider.Value.ToString();
        }

        private void LeftFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            captured.PosMsec = System.Convert.ToInt32(progressSlider.Value);
            captured.PosFrames--;
            if (captured.PosFrames < 0)
            {
                captured.PosFrames = 0;
            }
            progressSlider.Value = captured.PosMsec;
        }

        private void RightFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            captured.PosMsec = System.Convert.ToInt32(progressSlider.Value);
            captured.PosFrames++;
            if (captured.PosFrames > captured.FrameCount)
            {
                captured.PosFrames = captured.FrameCount;
            }
            progressSlider.Value = captured.PosMsec;
        }

        private void progressSlider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            TimeSpan newTimeSpan = new TimeSpan(0, 0, 0, 0, (int)progressSlider.Value);
            Media.Position = new TimeSpan((int)newTimeSpan.Days, (int)newTimeSpan.Hours, (int)newTimeSpan.Minutes, (int)newTimeSpan.Seconds, (int)newTimeSpan.Milliseconds);
            progressLabel.Content = newTimeSpan.ToString(@"hh\:mm\:ss");
        }

        public int nowMillis(int nowFrame)
        {
            captured.PosFrames = nowFrame;
            return captured.PosMsec;
        }

        public Double nowFps()
        {
            return captured.Fps;
        }

        public OpenCvSharp.Size nowSize()
        {
            return new OpenCvSharp.Size(captured.FrameWidth, captured.FrameHeight);
        }

        public int getNowFrame()
        {
            captured.PosMsec = (int)(Convert.ToDouble(Media.FramePosition.TotalMilliseconds.ToString()));
            return captured.PosFrames;
        }

        // 분할된 영상 저장 OPENCV
        public Mat getFrame(int nowFrame)
        {
            Mat frameInfo = new Mat();
            captured.PosFrames = nowFrame;
            captured.Read(frameInfo);
            return frameInfo;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (FullscreenButton.IsChecked == false)
            {
                this.ResizeMode = ResizeMode.CanResize;
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;

            }
            else
            {
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.WindowState = WindowState.Maximized;
            }
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (VolumeButton.IsChecked == true)
            {
                VolumePopup.IsOpen = true;
            }
            else
            {
                VolumePopup.IsOpen = false;
            }
        }

        // 분할된 영상 저장 FFME
        public void separateVideo()
        {
            Double startFrameMillis, endFrameMillis;
            int i;
            for (i = 0; i < frameArray.Count; i++)
            {
                captured.PosFrames = Int32.Parse(frameArray[i]);
                if (captured.PosMsec > Media.FramePosition.TotalMilliseconds) break;
            }

            if (i == 0)
            {
                endFrameMillis = captured.PosMsec;
                startFrameMillis = 0;
            }

            else
            {
                endFrameMillis = captured.PosMsec;
                captured.PosFrames = Int32.Parse(frameArray[i - 1]);
                startFrameMillis = captured.PosMsec;
                endFrameMillis -= startFrameMillis;
            }

            string path = "ffmpeg -i " + filePath + " -ss " + (startFrameMillis / 1000) + " -t " + (endFrameMillis / 1000) + " ./output.avi -y";
            ProcessStartInfo cmd = new ProcessStartInfo();
            Process process = new Process();
            cmd.FileName = @"cmd";
            cmd.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.CreateNoWindow = true;

            cmd.UseShellExecute = false;
            cmd.RedirectStandardInput = true;

            process.EnableRaisingEvents = false;
            process.StartInfo = cmd;
            process.Start();
            process.StandardInput.Write(path + Environment.NewLine);
            process.Close();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (DetailForm is null)
            {
                return;
            }

            DetailForm.Topmost = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (DetailForm is null)
            {
                return;
            }

            DetailForm.Topmost = false;
            Topmost = false;
        }
    }
}