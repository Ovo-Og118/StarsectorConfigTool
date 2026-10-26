using System;
using System.Windows.Forms;

namespace StarsectorConfigTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += delegate (object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show(
                    "程序遇到一个未处理的错误：\r\n\r\n" + e.Exception.Message,
                    "Starsector 基础配置工具",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
    }
}
