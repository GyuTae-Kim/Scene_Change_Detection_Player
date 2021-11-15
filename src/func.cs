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
using System.Drawing;                                   //참조추가하면서 추가된것들<수왕>
using Rectangle = System.Windows.Shapes.Rectangle;      //참조추가하면서 추가된것들
using Color = System.Windows.Media.Color;               //참조추가하면서 추가된것들

namespace WpfApp1
{
    public partial class MainWindow : System.Windows.Window
    {
        /* <-- For Main Window --> */
        public void MakeSliderPoint(TimeSpan time)
        {
            double totalMilliSec = progressSlider.Maximum - progressSlider.Minimum;     // 슬라이더의 전체 시간을 기반으로 동작
            double left = (time.TotalMilliseconds - progressSlider.Minimum) / totalMilliSec * PointCanvas.ActualWidth;

            sliderPoint.Add(new Rectangle()
            {
                Height = 3,
                Width = 6,
                Fill = new SolidColorBrush(Color.FromArgb(255, 255, 232, 0)),
            });
            sliderPoint[sliderPoint.Count - 1].MouseEnter += new MouseEventHandler(SliderPoint_MouseEnter);
            sliderPoint[sliderPoint.Count - 1].MouseLeave += new MouseEventHandler(SliderPoint_MouseLeave);
            sliderPoint[sliderPoint.Count - 1].MouseLeftButtonDown += new MouseButtonEventHandler(SliderPoint_MouseLeftButtonDown);
            sliderPoint[sliderPoint.Count - 1].MouseRightButtonDown += new MouseButtonEventHandler(SliderPoint_MouseRightButtonDown);
            sliderPoint[sliderPoint.Count - 1].MouseWheel += new MouseWheelEventHandler(SliderPoint_MouseWheel);

            PointCanvas.Children.Add(sliderPoint[sliderPoint.Count - 1]);
            Canvas.SetLeft(sliderPoint[sliderPoint.Count - 1], left);

            sliderPointTime.Add(time);
        }

        private void SliderPoint_MouseWheel(object sender, MouseWheelEventArgs e)
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
        
        private void SliderPoint_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle src = e.Source as Rectangle;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var idx = sliderPoint.IndexOf(src);
                PointCanvas.Children.Remove(src);
                sliderPointTime.RemoveAt(idx);
                sliderPoint.RemoveAt(idx);
                frameArray.RemoveAt(idx);
                frameArray.Sort();
            }
        }

        private void SliderPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle src = e.Source as Rectangle;
            Media.Position = sliderPointTime[sliderPoint.IndexOf(src)];

            RenewProgressLabel();
            progressSlider.Value = Media.Position.TotalMilliseconds;
        }

        public void RenewSliderPoint()
        {
            double totalMilliSec = progressSlider.Maximum - progressSlider.Minimum;
            double left;
            int i = 0;

            foreach (TimeSpan t in sliderPointTime)
            {
                left = (t.TotalMilliseconds - progressSlider.Minimum) / totalMilliSec * PointCanvas.ActualWidth;
                Canvas.SetLeft(sliderPoint[i++], left);
            }
        }
        
        // 삭제 성공: 1반환, 실패: 0반환
        public bool DeleteSliderPoint(TimeSpan time)
        {
            int idx = sliderPointTime.IndexOf(time);
            if (idx != -1)
            {
                PointCanvas.Children.RemoveAt(idx);
                sliderPoint.RemoveAt(idx);
                sliderPointTime.RemoveAt(idx);

                return true;
            }

            return false;
        }
        
        public void RenewProgressLabel()
        {
            TimeSpan t1 = new TimeSpan(0, 0, 0, 0, (int)progressSlider.Minimum);
            TimeSpan t2 = new TimeSpan(0, 0, 0, 0, (int)progressSlider.Maximum);
            progressMinimumLabel.Content = t1.ToString(@"hh\:mm\:ss");
            progressMaximumLabel.Content = t2.ToString(@"hh\:mm\:ss");
        }
        
        public void SliderPoint_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle rectangle = e.Source as Rectangle;
            Bitmap bmp;
            Mat image = new Mat();

            double time = sliderPointTime[sliderPoint.IndexOf(rectangle)].TotalMilliseconds;
            captured.Set(CaptureProperty.PosMsec, time);
            captured.Read(image);
            bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
            frameContainer.Height = 100;
            frameContainer.Width = 150;
            frameContainer.Source = BitmapToImageSource(bmp);
            double totalMilliSec = progressSlider.Maximum - progressSlider.Minimum;
            double left = time / totalMilliSec * (this.ActualWidth - 40);
            if (progressSlider.ActualWidth - left < 150)
            {
                left = progressSlider.ActualWidth - 150;
            }

            Canvas.SetLeft(frameContainer, left);
            FrameCanvas.Children.Add(frameContainer);
        }

        public void SliderPoint_MouseLeave(object sender, MouseEventArgs e)
        {
            FrameCanvas.Children.Remove(frameContainer);
        }
        
        private void MoveDetailFormLocation()
        {
            if (!(DetailForm is null))
            {
                if (!(this.WindowState == WindowState.Maximized || this.WindowState == WindowState.Minimized))
                {
                    DetailForm.Height = this.ActualHeight;
                    DetailForm.Top = this.Top;
                    DetailForm.Left = this.Left + this.ActualWidth;
                }

                else
                {
                    DetailForm.Visibility = Visibility.Hidden;
                    OpenInfoButton.IsChecked = false;
                }
            }
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void ClearMediaData(bool hardReset)
        {
            if (hardReset)
            {
                isMediaOpened = false;
                Media.Close();
            }

            foreach (Rectangle p in sliderPoint)
                PointCanvas.Children.Remove(p);

            int bef = sliderPoint.Count;
            sliderPoint.Clear();
            sliderPointTime.Clear();

            frameArray.Clear();
            cvFrame = 0;
            progressSlider.Value = 0;
            progressSlider.Maximum = 1;
            NowFrameLabel.Content = "0";

            progressMaximumLabel.Content = "00:00:00";
            OpenInfoButtonIcon.Opacity = 0.3;
            OpenInfoButton.IsEnabled = false;

            isRun = false;
            PlayBtnIcon.Data = Geometry.Parse("M0,0L22.652,15.996998 0,31.999996z");
            timer.Stop();
            timer2.Stop();
        }

        public void ReleaseNowFrame()
        {
            captured.PosMsec = System.Convert.ToInt32(progressSlider.Value);
            NowFrameLabel.Content = captured.PosFrames.ToString();
        }

        /* <-- For Detail Window --> */
        private void DetailFormDataSetting()
        {
            if (!isMediaOpened)
            {
                DetailFormDefaultDataSetting();
                return;
            }

            DetailForm.Media_Format.Text = Media.ContentStringFormat;
            DetailForm.Media_Size.Text = ConvertByte(Media.MediaStreamSize);
            DetailForm.Bit_Rate.Text = ConvertBit(Media.BitRate);
            DetailForm.Video_FPS.Text = Media.VideoFrameRate.ToString();
            DetailForm.Frame_Count.Text = captured.FrameCount.ToString();
            DetailForm.Video_Codec.Text = Media.VideoCodec;
            DetailForm.Video_Width.Text = Media.NaturalVideoWidth.ToString();
            DetailForm.Video_Height.Text = Media.NaturalVideoHeight.ToString();
            DetailForm.Audio_Codec.Text = Media.AudioCodec;
            DetailForm.Audio_Bit_Rate.Text = ConvertBit(Media.AudioBitRate);
            DetailForm.Audio_Channels.Text = Media.AudioChannels.ToString();
            DetailForm.Audio_Sampling.Text = Media.AudioSampleRate.ToString();
            DetailForm.Audio_Bits_Sample.Text = Media.AudioBitsPerSample.ToString();
            DetailForm.File_Path.Text = filePath;
            DetailForm.File_Path.ToolTip = filePath;
            DetailForm.File_Extension.Text = ConvertFileExtension();
        }

        private void DetailFormDefaultDataSetting()
        {
            DetailForm.Media_Format.Text = "";
            DetailForm.Media_Size.Text = "0 b";
            DetailForm.Bit_Rate.Text = "0 bits/s";
            DetailForm.Video_FPS.Text = "0";
            DetailForm.Video_Codec.Text = "";
            DetailForm.Video_Width.Text = "";
            DetailForm.Video_Height.Text = "";
            DetailForm.Audio_Codec.Text = "";
            DetailForm.Audio_Bit_Rate.Text = "0 kbits/s";
            DetailForm.Audio_Channels.Text = "0";
            DetailForm.Audio_Sampling.Text = "0";
            DetailForm.Audio_Bits_Sample.Text = "0";
            DetailForm.File_Path.Text = "";
            DetailForm.File_Extension.Text = "";
        }

        private string ConvertByte(long srcByte)
        {
            var suffix = "b";
            var output = 0d;
            const double minKiloByte = 1024;
            const double minMegaByte = 1024 * 1024;
            const double minGigaByte = 1024 * 1024 * 1024;

            var byteCount = System.Convert.ToDouble(srcByte);

            if (byteCount >= minKiloByte)
            {
                suffix = "kb";
                output = Math.Round(byteCount / minKiloByte, 2);
            }

            if (byteCount >= minMegaByte)
            {
                suffix = "mb";
                output = Math.Round(byteCount / minMegaByte, 2);
            }

            if (byteCount >= minGigaByte)
            {
                suffix = "gb";
                output = Math.Round(byteCount / minGigaByte, 2);
            }

            return suffix == "b" ?
                $"{output:0} {suffix}" :
                $"{output:0.00} {suffix}";
        }

        private string ConvertBit(long srcBit)
        {
            var suffix = "bits/s";
            var output = 0d;
            const double minKiloByte = 1000;
            const double minMegaByte = 1000 * 1000;
            const double minGigaByte = 1000 * 1000 * 1000;

            var byteCount = System.Convert.ToDouble(srcBit);

            if (byteCount >= minKiloByte)
            {
                suffix = "kbits/s";
                output = Math.Round(byteCount / minKiloByte, 2);
            }

            if (byteCount >= minMegaByte)
            {
                suffix = "mbits/s";
                output = Math.Round(byteCount / minMegaByte, 2);
            }

            if (byteCount >= minGigaByte)
            {
                suffix = "gbits/s";
                output = Math.Round(byteCount / minGigaByte, 2);
            }

            return suffix == "bits/s" ?
                $"{output:0} {suffix}" :
                $"{output:0.00} {suffix}";
        }

        private string ConvertFileExtension()
        {
            return filePath.Substring(filePath.LastIndexOf('.'));
        }
    }
}