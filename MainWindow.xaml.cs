using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DirectShowLib;
using Emgu;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.Util;
using Emgu.CV.Structure;
using Brushes = System.Windows.Media.Brushes;

namespace SCOI_3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ImgBinary imgBinary;

        VideoCapture videoCapture = null;
        DsDevice[] devices = null;
        int cameraSelected = 0;

        System.Windows.Threading.Dispatcher dispatcher;
        
        public BinaryOption binaryOption { get; set; }

        bool _flagFirstStart = true;

        double param;

        public MainWindow()
        {
            InitializeComponent();
            imgBinary = new ImgBinary(new Bitmap(10, 10));
            Picture.Source = imgBinary.BitmapSourseOrig;

            Methods.SelectedIndex = 1;
            Methods.SelectionChanged += MethodChanged;

            this.SizeChanged += MainWindow_SizeChanged;
            Apply.Checked += Apply_Checked;
            Apply.Unchecked += Apply_Unchecked;
            Apply.IsChecked = true;

            aRect.Delay = 50;
            aRect.ValueChanged += ARect_ValueChanged;
            UploadNewPicture();
            aRect.TickFrequency = 3;
            aRect.Value = 3;

            Param.Delay = 50;
            Param.MouseMove += Param_MouseMove;

            ValueParam.KeyUp += ValueParam_KeyUp;
            ValueParam.TextChanged += ValueParam_TextChanged;

            SaveMat.Checked += OptimizeChange;
            SaveMat.Unchecked += OptimizeChange;
            SaveMatSqr.Checked += OptimizeChange;
            SaveMatSqr.Unchecked += OptimizeChange;

            devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for(int i = 0; i < devices.Length; i++)
            {
                devicesBox.Items.Add(devices[i].Name);
            }
            devicesBox.SelectionChanged += DevicesBox_SelectionChanged;
            deviceConnect.Checked += DeviceConnect_Checked;
            deviceConnect.Unchecked += DeviceConnect_Unchecked;

            dispatcher = Picture.Dispatcher;
            binaryOption = BinaryOption.Otsu;
        }

        private void DeviceConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            videoCapture.Stop();
        }

        private void DeviceConnect_Checked(object sender, RoutedEventArgs e)
        {
            videoCapture = new VideoCapture(cameraSelected);

            videoCapture.ImageGrabbed += VideoCapture_ImageGrabbed;

            videoCapture.Start();
        }

        private void VideoCapture_ImageGrabbed(object sender, EventArgs e)
        {
            Mat mat = new Mat();
            videoCapture.Retrieve(mat);

            dispatcher.Invoke(() => {
                double param = 0.2;
                try
                {
                    param = double.Parse(ValueParam.Text);
                }
                catch
                {
                    
                }
                if (Apply.IsChecked.Value == true)
                {
                    imgBinary = new ImgBinary(mat.ToImage<Bgra, byte>().Flip(Emgu.CV.CvEnum.FlipType.Horizontal).Bitmap.ToBitmapSource(true));
                    Picture.Source = imgBinary.Binary(binaryOption, (int)aRect.Value, param);
                }
                else
                {
                    Picture.Source = mat.ToImage<Bgra, byte>().Flip(Emgu.CV.CvEnum.FlipType.Horizontal).Bitmap.ToBitmapSource(true);
                }
                UploadNewPicture();
            });
        }

        private void DevicesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cameraSelected = ((ComboBox)sender).SelectedIndex;
        }


        private void OptimizeChange(object sender, RoutedEventArgs e)
        {
            ResetSaveOption();
        }

        private void Param_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                ValueParam.Text = Math.Round(((Slider)sender).Value, 2).ToString("0.00");
        }

        private void ValueParam_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Delete && e.Key != System.Windows.Input.Key.Return)
                try
                {
                    param = double.Parse(ValueParam.Text);
                    Param.Value = param;
                }
                catch
                {
                    
                }
        }

        private void ValueParam_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                param = double.Parse(ValueParam.Text);
                Picture.Source = imgBinary.Binary(binaryOption, (int)aRect.Value, param);
            }
            catch
            {

            }
        }

        private void ARect_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ValueRectangle.Content = (int)e.NewValue;
            Picture.Source = imgBinary.Binary(binaryOption, (int)e.NewValue, param);
        }

        private void Apply_Unchecked(object sender, RoutedEventArgs e)
        {
            Picture.Source = imgBinary.BitmapSourseOrig;
        }

        private void Apply_Checked(object sender, RoutedEventArgs e)
        {
            var start = DateTime.UtcNow;
            BinaryOption binaryOption = (BinaryOption)Methods.SelectedIndex;
            switch (binaryOption)
            {
                case BinaryOption.GLobal:
                    Picture.Source = imgBinary.BinaryGlobal();
                    break;
                case BinaryOption.Otsu:
                    Picture.Source = imgBinary.BinaryOtsu();
                    break;
                case BinaryOption.Niblek:
                    try
                    {
                        double param = double.Parse(ValueParam.Text);
                        Picture.Source = imgBinary.BinaryNiblek((int)aRect.Value, param);
                        break;
                    }
                    catch
                    {
                        WriteLog("Param error", Brushes.Red);
                        break;
                    }
                case BinaryOption.Sauvola:
                    try
                    {
                        double param = double.Parse(ValueParam.Text);
                        Picture.Source = imgBinary.BinarySauvola((int)aRect.Value, param);
                        break;
                    }
                    catch
                    {
                        WriteLog("Param error", Brushes.Red);
                        break;
                    }
                case BinaryOption.KristianWolf:
                    try
                    {
                        double param = double.Parse(ValueParam.Text);
                        Picture.Source = imgBinary.BinaryKristianWolf((int)aRect.Value, param);
                        break;
                    }
                    catch
                    {
                        WriteLog("Param error", Brushes.Red);
                        break;
                    }
                case BinaryOption.Bradley:
                    try
                    {
                        double param = double.Parse(ValueParam.Text);
                        Picture.Source = imgBinary.BinaryBradley((int)aRect.Value, param);
                        break;
                    }
                    catch
                    {
                        WriteLog("Param error", Brushes.Red);
                        break;
                    }
            }
            WriteLog("Обработано за " + (DateTime.UtcNow - start).TotalMilliseconds + "ms", System.Windows.Media.Brushes.DarkGreen);
        }




        //Scale window
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_flagFirstStart)
            {
                var deltaHeight = e.NewSize.Height - e.PreviousSize.Height;
                var deltaWidth = e.NewSize.Width - e.PreviousSize.Width;
                Picture.Height += deltaHeight;
                Picture.Width += deltaWidth;
                NavBar.Width += deltaWidth;
                Log.Margin = new Thickness(Log.Margin.Left + deltaWidth, Log.Margin.Top, 0, 0);
                Log.Height += deltaHeight;
            }
            else
            {
                _flagFirstStart = false;
            }
        }


        //Кнопочки
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            OpenFileDialog fileManager = new OpenFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            if (item != "")
            {
                imgBinary = new ImgBinary(new Bitmap(item).To24bppRgb());
                Picture.Source = imgBinary.BitmapSourseOrig;
                WriteLog("Загружено", Brushes.DarkGreen);
            }
        }

        private void FileIsDropped(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData("FileDrop");
            try
            {
                foreach (var item in paths)
                {
                    var start = DateTime.UtcNow;
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    imgBinary = new ImgBinary(new BitmapImage(new Uri(item, UriKind.RelativeOrAbsolute)));
                    if (Apply.IsChecked.Value)
                        Picture.Source = imgBinary.Binary((BinaryOption)Methods.SelectedIndex);
                    else
                        Picture.Source = imgBinary.BitmapSourseOrig;
                    UploadNewPicture();
                    WriteLog("Загружено " + (DateTime.UtcNow - start).TotalMilliseconds, Brushes.DarkGreen);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, Brushes.Red);
            }
        }


        private void SaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileManager = new SaveFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            try
            {
                if (item != "")
                {
                    Picture.Source.Save(item);
                }
                WriteLog("Файл " + item + " успешно сохранен", Brushes.DarkBlue);
            }
            catch
            {
                WriteLog("Не удалось сохранить в указанный файл", Brushes.Red);
            }
        }

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(Picture.Source as BitmapSource);
            WriteLog("Скопировано", Brushes.DarkOrange);
        }

        private void CutClick(object sender, RoutedEventArgs e)
        {

        }

        private void PasteClick(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                imgBinary = new ImgBinary(Clipboard.GetImage());
                Picture.Source = imgBinary.BinaryOtsu();
                UploadNewPicture();
                WriteLog("Загружено", Brushes.DarkGreen);
            }
            else
            {
                WriteLog("Не картинка", Brushes.Red);
            }
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void WriteLog(string message, System.Windows.Media.SolidColorBrush color = null)
        {
            if (color == null)
                color = System.Windows.Media.Brushes.Black;
            var text = new TextBlock() { Text = message, Foreground = color };
            Log.Items.Add(text);
            Log.ScrollIntoView(text);
            Log.SelectedItem = text;
        }

        private void MemoryLog(object sender, RoutedEventArgs e)
        {
            //Показать память
            Process process = Process.GetCurrentProcess();
            long memoryAmount = process.WorkingSet64;
            WriteLog("Памяти скушано - " + (memoryAmount / (1024 * 1024)).ToString(), Brushes.Purple);

            double param = double.Parse(ValueParam.Text);
            Picture.Source = imgBinary.TreshHold((int)aRect.Value, param);
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            Log.Items.Clear();
        }



        private void MethodChanged(object sender, SelectionChangedEventArgs e)
        {
            binaryOption = (BinaryOption)((ComboBox)sender).SelectedIndex;
            ResetParam();
            if (Apply.IsChecked.Value)
            {
                var start = DateTime.UtcNow;
                switch (binaryOption)
                {
                    case BinaryOption.GLobal:
                        Picture.Source = imgBinary.BinaryGlobal();
                        break;
                    case BinaryOption.Otsu:
                        Picture.Source = imgBinary.BinaryOtsu();
                        break;
                    case BinaryOption.Niblek:
                        Picture.Source = imgBinary.BinaryNiblek((int)aRect.Value);
                        break;
                    case BinaryOption.Sauvola:
                        Picture.Source = imgBinary.BinarySauvola((int)aRect.Value);
                        break;
                    case BinaryOption.KristianWolf:
                        Picture.Source = imgBinary.BinaryKristianWolf((int)aRect.Value);
                        break;
                    case BinaryOption.Bradley:
                        Picture.Source = imgBinary.BinaryBradley((int)aRect.Value);
                        break;
                }
                WriteLog("Обработано за " + (DateTime.UtcNow - start).TotalMilliseconds + "ms", System.Windows.Media.Brushes.DarkGreen);
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }   
        }

        private void ResetSaveOption()
        {
            imgBinary.RememberIntegralMatrix = SaveMat.IsChecked.Value;
            imgBinary.RememberIntegralSqrMatrix = SaveMatSqr.IsChecked.Value;
            imgBinary.CheckAndDeleteMatrix();
        }

        private void UploadNewPicture()
        {
            if (Picture.Source.Width > Picture.Source.Height)
                aRect.Maximum = Picture.Source.Width - (Picture.Source.Width % 3);
            else
                aRect.Maximum = Picture.Source.Height - (Picture.Source.Height % 3);
            //if (aRect.Maximum > 100)
            //    aRect.Maximum = 100;
        }

        private void ResetParam()
        {
            switch (binaryOption)
            {
                case BinaryOption.Niblek:
                    Param.Value = -0.2;
                    Param.Minimum = -1;
                    Param.Maximum = 1;
                    Param.TickFrequency = 0.01;
                    break;
                case BinaryOption.Sauvola:
                    Param.Value = 0.2;
                    Param.Minimum = -0.5;
                    Param.Maximum = 0.5;
                    Param.TickFrequency = 0.01;
                    break;
                case BinaryOption.KristianWolf:
                    Param.Value = 0.5;
                    Param.Minimum = 0;
                    Param.Maximum = 1;
                    Param.TickFrequency = 0.01;
                    break;
                case BinaryOption.Bradley:
                    Param.Value = 0.15;
                    Param.Minimum = 0;
                    Param.Maximum = 1;
                    Param.TickFrequency = 0.01;
                    break;
            }
            ValueParam.Text = Math.Round(Param.Value, 2).ToString("0.00");
        }
    }
}
