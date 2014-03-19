using DragDropLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using DataObject = System.Windows.DataObject;


namespace DesignHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += new EventHandler(MainWindow_SourceInitialized);

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThunderStarter.HotKey Ctrl_Atl_U = new ThunderStarter.HotKey(this,
                ThunderStarter.HotKey.KeyFlags.MOD_ALT | ThunderStarter.HotKey.KeyFlags.MOD_CONTROL,
                 System.Windows.Forms.Keys.U);
            Ctrl_Atl_U.OnHotKey += Ctrl_Atl_U_OnHotKey;

        }



        private void Open_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Image|*.jpg;*.png|All Files|*.*";
            if (open.ShowDialog()==true)
            {
                OpenImage(open.FileName);
            }
        }

        private void OpenImage(string file)
        {
            string ext = System.IO.Path.GetExtension(file).ToLower();
            if (ext != ".bmp" && ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif" && ext != ".tmp")
            {
                MessageBox.Show("文件扩展名不是常见图片类型，我懒得打开。");
                return;
            }
            try
            {
                BitmapImage myBitmapImage = new BitmapImage(new Uri(file));
                OpenImage(myBitmapImage);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            // Create source
        }

        private void OpenImage(BitmapSource myBitmapImage)
        {
            this.image.Source = myBitmapImage;
            this.image.Stretch = Stretch.UniformToFill;

            this.Width = myBitmapImage.PixelWidth;
            this.Height = myBitmapImage.PixelHeight;// +this.toolbar.ActualHeight;
        }

        private void SetImageFromUri(Uri uri)
        {
            string fileName = System.IO.Path.GetTempFileName();
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += delegate
                {
                    this.OpenImage(fileName);
                };
                webClient.DownloadFileAsync(uri, fileName);
            }
            
        }
        private void CommandBinding_PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //MessageBox.Show("Clipboard operation occured!");
            if (Clipboard.ContainsImage())
            {
                this.OpenImage(Clipboard.GetImage());
            }
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region Move Window
		bool moving = false;
        double x1, y1;
        double movement = 10;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.moving = true;
                Point pos = e.GetPosition(this);
                this.x1 = pos.X;
                this.y1 = pos.Y;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                this.moving = false;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.moving && !this.Lock)
            {
                Point pos = e.GetPosition(this);
                this.Left += pos.X - this.x1;
                this.Top += pos.Y - this.y1;

            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double movement = this.Lock ? 0 : this.movement;
            switch (e.Key)
            {
                case Key.Left:
                    this.Left -= movement;
                    break;
                case Key.Right:
                    this.Left += movement;
                    break;
                case Key.Up:
                    this.Top -= movement;
                    break;
                case Key.Down:
                    this.Top += movement;
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    this.movement = 1;
                    break;
                default:
                    break;
            }
            Debug.WriteLine(e.Key);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    this.movement = 10;
                    break;
            }
        }

	#endregion    
        #region Mouse Wheel KeyPress Drag&Drop

        private void StackPanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double o = this.image.Opacity;
            if (e.Delta > 0)
            {
                o += .05;
            }
            if (e.Delta < 0)
            {
                o -= .05;
            }
            if (o > 1) o = 1;
            if (o < .05) o = .1;
            this.image.Opacity = o;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            Win32Point wp;
            e.Effects = DragDropEffects.Copy;
            Point p = e.GetPosition(this);
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.DragEnter(wndHelper.Handle, (ComIDataObject)e.Data, ref wp, (int)e.Effects);
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.DragLeave();
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            Win32Point wp;
            e.Effects = DragDropEffects.Copy;
            Point p = e.GetPosition(this);
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.DragOver(ref wp, (int)e.Effects);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            Win32Point wp;
            e.Effects = DragDropEffects.Copy;
            Point p = e.GetPosition(this);
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.Drop((ComIDataObject)e.Data, ref wp, (int)e.Effects);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                this.OpenImage(files[0]);
            }

            System.Windows.IDataObject data = e.Data;
            string[] formats = data.GetFormats();
            if (formats.Contains("text/html"))
            {
                var obj = data.GetData("text/html");
                string html = string.Empty;
                if (obj is string)
                {
                    html = (string)obj;
                }
                else if (obj is MemoryStream)
                {
                    MemoryStream ms = (MemoryStream)obj;
                    byte[] buffer = new byte[ms.Length];
                    ms.Read(buffer, 0, (int)ms.Length);
                    if (buffer[1] == (byte)0)  // Detecting unicode
                    {
                        html = System.Text.Encoding.Unicode.GetString(buffer);
                    }
                    else
                    {
                        html = System.Text.Encoding.ASCII.GetString(buffer);
                    }
                }
                // Using a regex to parse HTML, but JUST FOR THIS EXAMPLE :-)
                var match = new Regex(@"<img[^>]+src=""([^""]*)""").Match(html);
                if (match.Success)
                {
                    Uri uri = new Uri(match.Groups[1].Value);
                    SetImageFromUri(uri);
                }
            }
        } 
        #endregion

        #region Get Color & ContextMenu

        private void image_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //获取像素
            //定义切割矩形
            var cut = new Int32Rect((int)e.CursorLeft, (int)e.CursorTop, 1, 1);
            //计算Stride
            var bitmap = this.image.Source as BitmapSource;
            var stride = bitmap.Format.BitsPerPixel * cut.Width / 8;
            //声明字节数组
            byte[] data = new byte[cut.Height * stride];
            //调用CopyPixels
            bitmap.CopyPixels(cut, data, stride, 0);

            //Debug.WriteLine(string.Format("{0},{1},{2},{3}", data[0], data[1], data[2], data[3]));

            this.mnuItemColor.Items.Clear();

            string color16 = "#", color10 = "";
            for (int i = 3; i >= 0; i--)
            {
                int c = (int)data[i];
                color16 += Convert.ToString(c, 16).ToUpper();
                if (color10.Length > 0)
                {
                    color10 += ",";
                }
                color10 += c.ToString();
            }
            this.mnuItemColor10.Header = color10;
            this.mnuItemColor16.Header = color16;

            //设置图标
            int iconW = 16, iconH =16;
            int iconStride = bitmap.Format.BitsPerPixel * iconW / 8;
            byte[] iconData = new byte[iconH * iconStride];
            for (int i = 0; i < iconData.Length; i++)
            {
                iconData[i] = data[i % 4];
            }
            BitmapSource icon = BitmapSource.Create(iconW, iconH, 96, 96, bitmap.Format, null, iconData, iconStride);
            this.mnuItemColor10.Icon = new Image() { Source = icon };
            this.mnuItemColor16.Icon = new Image() { Source = icon };
        }

        private void mnuItemColor_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            Clipboard.SetText(item.Header.ToString());
        } 
        #endregion

        #region Click Though Window

        private void Lock_Button_Click(object sender, RoutedEventArgs e)
        {
            DoLock();

        }
        private void Through_Button_Click(object sender, RoutedEventArgs e)
        {
            this.mnuItemThrough.IsChecked = !this.mnuItemThrough.IsChecked;

        }

        private void DoLock()
        {
            this.Lock = !this.Lock;
            this.Topmost = this.Lock;
            this.mnuItemLock.Header = this.Lock ? "Unlock" : "Lock";
            if (this.mnuItemThrough.IsChecked)
            {
                if (this.Lock)
                {

                    SetWindowLong(hwnd, GWL_EXSTYLE, oldStyle | WS_EX_TRANSPARENT);
                    this.LockTip.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    SetWindowLong(hwnd, GWL_EXSTYLE, oldStyle);
                    this.LockTip.Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }


        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        int oldStyle;
        IntPtr hwnd;

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            //为了能够穿透窗体
            oldStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            hwnd = new WindowInteropHelper(this).Handle;
        }

        private void CommandBinding_UndoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Lock)
            {
                DoLock();
            }
        }

        void Ctrl_Atl_U_OnHotKey()
        {
            if (this.Lock)
            {
                DoLock();
            }
        }

        #endregion
        
        public bool Lock { get; set; }

    }
}
