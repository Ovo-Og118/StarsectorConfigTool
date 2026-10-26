using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StarsectorConfigTool.Core
{
    /// <summary>
    /// 读写 JVM 启动参数里的堆内存（-Xms / -Xmx）。
    ///
    /// Starsector 的启动方式因整合包而异，常见有两种：
    ///   1. Miko 系整合包：Miko_Rouge.bat 用 "@..\Miko_Simple.txt" 读参数文件
    ///   2. 原版：vmparams 文件里一整行 java 参数
    /// 这里会自动找出真正被使用的那一份。
    /// </summary>
    public class JvmEditor
    {
        /// <summary>参数文件的完整路径。</summary>
        public string FilePath { get; private set; }

        /// <summary>说明用：这个文件是怎么被找到的。</summary>
        public string DetectionNote { get; private set; }

        private string _text;

        private JvmEditor() { }

        /// <summary>
        /// 在安装目录里寻找 JVM 参数文件。找不到返回 null。
        /// </summary>
        public static JvmEditor Locate(string installDir)
        {
            if (string.IsNullOrWhiteSpace(installDir) || !Directory.Exists(installDir))
                return null;

            var tried = new List<string>();

            // 1) 从 .bat 里解析 "@..\xxx.txt" / "@xxx.txt" / "-classpath" 等
            foreach (string bat in SafeGetFiles(installDir, "*.bat"))
            {
                string content;
                try { content = File.ReadAllText(bat, Encoding.UTF8); }
                catch { continue; }

                foreach (Match m in Regex.Matches(content, @"@(\.\.[\\/])?([^\s""]+\.(?:txt|vmparams))", RegexOptions.IgnoreCase))
                {
                    string rel = m.Groups[2].Value.Replace('/', '\\');
                    string candidate = Path.GetFullPath(Path.Combine(installDir, rel));
                    tried.Add(candidate);
                    if (File.Exists(candidate) && ContainsHeapSetting(candidate))
                        return Make(candidate, "由 " + Path.GetFileName(bat) + " 推理得出");
                }
            }

            // 2) 常见文件名
            string[] known = { "Miko_Simple.txt", "vmparams", "Miko_Info.txt", "jvm_args.txt", "args.txt" };
            foreach (string name in known)
            {
                string candidate = Path.Combine(installDir, name);
                tried.Add(candidate);
                if (File.Exists(candidate) && ContainsHeapSetting(candidate))
                    return Make(candidate, "按常见文件名匹配");
            }

            // 3) 根目录下任意含 -Xmx 的 .txt / vmparams
            foreach (string file in SafeGetFiles(installDir, "*.txt"))
            {
                if (ContainsHeapSetting(file))
                    return Make(file, "扫描到含内存参数的文本文件");
            }
            foreach (string file in SafeGetFiles(installDir, "vmparams*"))
            {
                if (ContainsHeapSetting(file))
                    return Make(file, "扫描到 vmparams");
            }

            return null;
        }

        private static JvmEditor Make(string path, string note)
        {
            var e = new JvmEditor();
            e.FilePath = path;
            e.DetectionNote = note;
            e._text = File.ReadAllText(path, Encoding.UTF8);
            return e;
        }

        private static bool ContainsHeapSetting(string path)
        {
            try
            {
                string t = File.ReadAllText(path, Encoding.UTF8);
                return Regex.IsMatch(t, @"-Xmx\d+[mMgG]?");
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> SafeGetFiles(string dir, string pattern)
        {
            try { return Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly); }
            catch { return new string[0]; }
        }

        /// <summary>读取当前堆大小，单位 MB。读不到返回 false。</summary>
        public bool TryGetHeapMb(out int mb)
        {
            mb = 0;
            Match m = Regex.Match(_text, @"-Xmx(\d+)([mMgG]?)");
            if (!m.Success) return false;

            long value;
            if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return false;

            string unit = m.Groups[2].Value.ToLowerInvariant();
            if (unit == "g") value *= 1024;

            // 有些配置文件写成 11000m 或 11g，统一换算成 MB
            mb = (int)Math.Min(value, int.MaxValue);
            return true;
        }

        /// <summary>
        /// 写入新的堆大小（MB）。同时改写 -Xms 与 -Xmx，
        /// 避免出现 Xms > Xmx 导致 JVM 拒绝启动。
        /// </summary>
        public bool TrySetHeapMb(int mb, out string error)
        {
            error = null;
            if (mb < 512)
            {
                error = "内存不能小于 512 MB";
                return false;
            }

            string newArg = "-Xmx" + mb.ToString(CultureInfo.InvariantCulture) + "m";
            string newArgMin = "-Xms" + mb.ToString(CultureInfo.InvariantCulture) + "m";

            bool changed = false;

            if (Regex.IsMatch(_text, @"-Xmx\d+[mMgG]?"))
            {
                _text = Regex.Replace(_text, @"-Xmx\d+[mMgG]?", newArg);
                changed = true;
            }

            if (Regex.IsMatch(_text, @"-Xms\d+[mMgG]?"))
            {
                _text = Regex.Replace(_text, @"-Xms\d+[mMgG]?", newArgMin);
                changed = true;
            }

            if (!changed)
            {
                error = "这个文件里没有找到 -Xmx 参数";
                return false;
            }

            return true;
        }

        /// <summary>写回磁盘，保持 UTF-8 无 BOM。</summary>
        public void Save()
        {
            File.WriteAllText(FilePath, _text, new UTF8Encoding(false));
        }
    }
}
