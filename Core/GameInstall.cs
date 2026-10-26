using System;
using System.Collections.Generic;
using System.IO;

namespace StarsectorConfigTool.Core
{
    /// <summary>
    /// 负责定位 Starsector 安装目录，并记住上次使用的路径。
    /// </summary>
    public static class GameInstall
    {
        /// <summary>settings.json 相对于安装根目录的位置。</summary>
        public const string SettingsRelativePath = @"starsector-core\data\config\settings.json";

        /// <summary>记住了上次选择的目录的小文件，放在 exe 旁边，绿色免安装。</summary>
        private static string ConfigFilePath
        {
            get
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(exeDir, "scf.ini");
            }
        }

        /// <summary>判断一个目录是不是有效的 Starsector 根目录。</summary>
        public static bool IsValid(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return false;
            try
            {
                return File.Exists(Path.Combine(dir, SettingsRelativePath));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>取 settings.json 的完整路径。</summary>
        public static string GetSettingsPath(string installDir)
        {
            return Path.Combine(installDir, SettingsRelativePath);
        }

        /// <summary>
        /// 尝试自动探测安装目录。找不到返回 null。
        /// 顺序：上次记录 -> exe 自身与上级目录 -> 常见路径 -> 各盘根目录搜一层 Starsector 文件夹。
        /// </summary>
        public static string Detect()
        {
            // 1) 上次使用过的
            string remembered = LoadRemembered();
            if (IsValid(remembered)) return remembered;

            // 2) exe 所在目录及其所有上级（用户常把 exe 丢进游戏目录或旁边）
            try
            {
                DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int i = 0; i < 4 && di != null; i++)
                {
                    if (IsValid(di.FullName)) return di.FullName;
                    di = di.Parent;
                }
            }
            catch { }

            // 3) 常见安装路径
            var common = new List<string>
            {
                @"C:\Program Files (x86)\Fractal Softworks\Starsector",
                @"C:\Program Files\Fractal Softworks\Starsector",
                @"C:\Starsector",
                @"D:\Starsector",
                @"E:\Starsector",
                @"D:\Games\Starsector",
                @"E:\Games\Starsector",
                @"D:\Program Files\Starsector",
                @"E:\Program Files\Starsector",
            };
            foreach (string p in common)
            {
                if (IsValid(p)) return p;
            }

            // 4) 各固定磁盘根目录下找一层名为 Starsector 的文件夹
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                    string root = drive.RootDirectory.FullName;

                    string direct = Path.Combine(root, "Starsector");
                    if (IsValid(direct)) return direct;

                    // 再往下一层（如 D:\Games\Starsector）
                    try
                    {
                        foreach (string sub in Directory.GetDirectories(root))
                        {
                            string name = Path.GetFileName(sub);
                            if (name.IndexOf("program", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            string candidate = Path.Combine(sub, "Starsector");
                            if (IsValid(candidate)) return candidate;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        /// <summary>记住用户选择的目录。</summary>
        public static void Remember(string dir)
        {
            try
            {
                File.WriteAllText(ConfigFilePath, dir ?? string.Empty);
            }
            catch { }
        }

        private static string LoadRemembered()
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return null;
                return File.ReadAllText(ConfigFilePath).Trim();
            }
            catch
            {
                return null;
            }
        }
    }
}
