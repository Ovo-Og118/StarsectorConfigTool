using System;
using System.Collections.Generic;
using System.IO;

namespace StarsectorConfigTool.Core
{
    /// <summary>
    /// 写入前的自动备份。备份统一放在游戏目录下的 _scf_backup 文件夹，
    /// 文件名形如 settings.json.20261026-101530.bak，按时间排序即可还原。
    /// </summary>
    public static class BackupManager
    {
        public const string BackupFolderName = "_scf_backup";

        /// <summary>备份目录（不存在则创建）。</summary>
        public static string GetBackupDir(string installDir)
        {
            string dir = Path.Combine(installDir, BackupFolderName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>备份单个文件，返回备份文件路径。</summary>
        public static string Backup(string sourceFile, string installDir)
        {
            string dir = GetBackupDir(installDir);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string name = Path.GetFileName(sourceFile) + "." + stamp + ".bak";
            string dest = Path.Combine(dir, name);

            // 同一秒内备份两次时避免覆盖
            int n = 1;
            while (File.Exists(dest))
            {
                dest = Path.Combine(dir, Path.GetFileName(sourceFile) + "." + stamp + "-" + n + ".bak");
                n++;
            }

            File.Copy(sourceFile, dest, false);
            return dest;
        }

        /// <summary>列出已有备份，按时间从新到旧。</summary>
        public static List<FileInfo> ListBackups(string installDir)
        {
            var list = new List<FileInfo>();
            try
            {
                string dir = Path.Combine(installDir, BackupFolderName);
                if (!Directory.Exists(dir)) return list;

                list.AddRange(new DirectoryInfo(dir).GetFiles("*.bak"));
                list.Sort(delegate (FileInfo a, FileInfo b)
                {
                    return b.LastWriteTime.CompareTo(a.LastWriteTime);
                });
            }
            catch { }
            return list;
        }

        /// <summary>
        /// 把某个备份还原回原文件。会根据备份文件名前缀推断目标路径。
        /// </summary>
        public static bool Restore(FileInfo backup, string installDir, out string restoredTo, out string error)
        {
            restoredTo = null;
            error = null;

            string target = ResolveTarget(backup.Name, installDir);
            if (target == null)
            {
                error = "无法从文件名推断这个备份属于哪个文件：" + backup.Name;
                return false;
            }

            try
            {
                File.Copy(backup.FullName, target, true);
                restoredTo = target;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>根据备份文件名推断它原本属于哪个文件。</summary>
        private static string ResolveTarget(string backupName, string installDir)
        {
            if (backupName.StartsWith("settings.json.", StringComparison.OrdinalIgnoreCase))
                return GameInstall.GetSettingsPath(installDir);

            int dot = backupName.IndexOf('.');
            if (dot <= 0) return null;
            string originalName = backupName.Substring(0, dot);

            string candidate = Path.Combine(installDir, originalName);
            if (File.Exists(candidate)) return candidate;

            return null;
        }
    }
}
