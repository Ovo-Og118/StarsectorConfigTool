using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using StarsectorConfigTool.Core;

namespace CoreTest
{
    internal static class Program
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool condition, string detail = null)
        {
            if (condition)
            {
                _pass++;
                Console.WriteLine("  [PASS] " + name);
            }
            else
            {
                _fail++;
                Console.WriteLine("  [FAIL] " + name + (detail != null ? "  -> " + detail : ""));
            }
        }

        private static int Main(string[] args)
        {
            string root = args.Length > 0 ? args[0] : @"E:\3\test\_testdata\FakeStarsector";
            Console.WriteLine("测试目录: " + root);
            Console.WriteLine();

            TestInstall(root);
            TestSettings(root);
            TestJvm(root);
            TestBackup(root);

            Console.WriteLine();
            Console.WriteLine(string.Format("结果: {0} 通过, {1} 失败", _pass, _fail));
            return _fail == 0 ? 0 : 1;
        }

        private static void TestInstall(string root)
        {
            Console.WriteLine("== 1. 目录识别 ==");
            Check("有效目录被识别", GameInstall.IsValid(root));
            Check("无效目录被拒绝", !GameInstall.IsValid(Path.Combine(root, "starsector-core")));
            Check("空字符串被拒绝", !GameInstall.IsValid(""));
            Check("settings 路径拼接正确",
                GameInstall.GetSettingsPath(root).EndsWith(@"starsector-core\data\config\settings.json"));
        }

        private static void TestSettings(string root)
        {
            Console.WriteLine();
            Console.WriteLine("== 2. settings.json 读写 ==");

            string path = GameInstall.GetSettingsPath(root);
            string original = File.ReadAllText(path);
            int originalLines = original.Split('\n').Length;
            int originalComments = Regex.Matches(original, "#").Count;

            var ed = new SettingsEditor(path);

            double v;
            Check("读到 playerMaxLevel = 50", ed.TryGet("playerMaxLevel", out v) && Math.Abs(v - 50) < 0.001,
                "实际 " + v);
            Check("读到 skillPointsPerLevel = 2", ed.TryGet("skillPointsPerLevel", out v) && Math.Abs(v - 2) < 0.001,
                "实际 " + v);
            Check("读到 maxBattleSize = 600", ed.TryGet("maxBattleSize", out v) && Math.Abs(v - 600) < 0.001,
                "实际 " + v);
            Check("读到小数项 xpGainMult", ed.TryGet("xpGainMult", out v), "读不到");
            Check("不存在的键返回 false", !ed.TryGet("no_such_key_xyz", out v));

            // 写入
            Check("写入 playerMaxLevel = 120", ed.TrySet("playerMaxLevel", 120, true));
            Check("写入 xpGainMult = 2.5", ed.TrySet("xpGainMult", 2.5, false));
            Check("写入 fluxPerCapacitor = 400", ed.TrySet("fluxPerCapacitor", 400, true));
            Check("写入不存在的键返回 false", !ed.TrySet("no_such_key_xyz", 1, true));

            double back;
            Check("回读 playerMaxLevel = 120", ed.TryGet("playerMaxLevel", out back) && Math.Abs(back - 120) < 0.001,
                "实际 " + back);
            Check("回读 xpGainMult = 2.5", ed.TryGet("xpGainMult", out back) && Math.Abs(back - 2.5) < 0.001,
                "实际 " + back);

            string after = ed.Text;
            int afterLines = after.Split('\n').Length;
            int afterComments = Regex.Matches(after, "#").Count;

            Check("行数未变（格式未被破坏）", originalLines == afterLines,
                originalLines + " -> " + afterLines);
            Check("注释数量未变", originalComments == afterComments,
                originalComments + " -> " + afterComments);
            Check("只改了 3 处", DiffCount(original, after) >= 3);

            // 落盘再读
            ed.Save();
            var ed2 = new SettingsEditor(path);
            Check("落盘后重新读取仍是 120",
                ed2.TryGet("playerMaxLevel", out back) && Math.Abs(back - 120) < 0.001);

            byte[] bytes = File.ReadAllBytes(path);
            Check("无 BOM", !(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF));

            File.WriteAllText(path, original);
            Console.WriteLine("  （已把测试文件恢复成原始内容）");
        }

        private static int DiffCount(string a, string b)
        {
            // 粗略：比较不同字符数
            int n = Math.Min(a.Length, b.Length);
            int d = Math.Abs(a.Length - b.Length);
            for (int i = 0; i < n; i++) if (a[i] != b[i]) d++;
            return d;
        }

        private static void TestJvm(string root)
        {
            Console.WriteLine();
            Console.WriteLine("== 3. JVM 参数 ==");

            JvmEditor jvm = JvmEditor.Locate(root);
            Check("找到参数文件", jvm != null);
            if (jvm == null) return;

            Console.WriteLine("  （定位方式：" + jvm.DetectionNote + "）");
            Console.WriteLine("  （文件：" + jvm.FilePath + "）");

            Check("定位到 Miko_Simple.txt",
                jvm.FilePath.EndsWith("Miko_Simple.txt", StringComparison.OrdinalIgnoreCase),
                jvm.FilePath);

            int mb;
            Check("读到堆大小 11264", jvm.TryGetHeapMb(out mb) && mb == 11264, "实际 " + mb);

            string before = File.ReadAllText(jvm.FilePath);

            string err;
            Check("写入 16384 MB", jvm.TrySetHeapMb(16384, out err), err);

            int after;
            Check("回读 16384", jvm.TryGetHeapMb(out after) && after == 16384, "实际 " + after);

            jvm.Save();
            string written = File.ReadAllText(jvm.FilePath);
            Check("文件里出现 -Xmx16384m", written.Contains("-Xmx16384m"));
            Check("文件里出现 -Xms16384m", written.Contains("-Xms16384m"));
            Check("其他参数没被删（还有 -classpath）", written.Contains("-classpath"));
            Check("行数未变", before.Split('\n').Length == written.Split('\n').Length,
                before.Split('\n').Length + " -> " + written.Split('\n').Length);

            File.WriteAllText(jvm.FilePath, before);
            Console.WriteLine("  （已把测试文件恢复成原始内容）");
        }

        private static void TestBackup(string root)
        {
            Console.WriteLine();
            Console.WriteLine("== 4. 备份与还原 ==");

            string settings = GameInstall.GetSettingsPath(root);
            string original = File.ReadAllText(settings);

            string b1 = BackupManager.Backup(settings, root);
            Check("备份文件已生成", File.Exists(b1), b1);
            Check("备份在 _scf_backup 下", b1.Contains(BackupManager.BackupFolderName));
            Check("备份内容与原文一致", File.ReadAllText(b1) == original);

            string b2 = BackupManager.Backup(settings, root);
            Check("连续备份不会覆盖前一份", b1 != b2, b1 + " vs " + b2);

            List<FileInfo> list = BackupManager.ListBackups(root);
            Check("能列出备份", list.Count >= 2, "数量 " + list.Count);

            // 破坏文件再还原
            File.WriteAllText(settings, "BROKEN");
            string restoredTo, error;
            bool ok = BackupManager.Restore(new FileInfo(b1), root, out restoredTo, out error);
            Check("还原成功", ok, error);
            Check("还原后内容正确", File.ReadAllText(settings) == original);

            File.WriteAllText(settings, original);
            Console.WriteLine("  （已把测试文件恢复成原始内容）");
        }
    }
}
