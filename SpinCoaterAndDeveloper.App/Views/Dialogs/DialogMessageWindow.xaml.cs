using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpinCoaterAndDeveloper.App.Views.Dialogs
{
    /// <summary>
    /// DialogMessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DialogMessageWindow : Window, IDialogWindow
    {
        public DialogMessageWindow()
        {
            InitializeComponent();
        }
        public IDialogResult Result { get; set; }

        //屏蔽关闭按钮显示(ALT+F4仍可使用,需要下面的屏蔽事件)
        private const int GWL_STYLE = -16; private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Title == "提示")
            {
                //只有窗口名为"提示"时,屏蔽关闭及Icon等
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            }
        }

        //屏蔽关闭事件
        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    e.Cancel = true;
        //}
    }
}
