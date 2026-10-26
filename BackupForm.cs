using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using StarsectorConfigTool.Core;

namespace StarsectorConfigTool
{
    /// <summary>备份列表与还原的小对话框。</summary>
    public class BackupForm : Form
    {
        private readonly string _installDir;
        private readonly ListBox _list;
        private readonly Label _lblInfo;
        private List<FileInfo> _backups;

        /// <summary>是否在这个对话框里执行过还原。</summary>
        public bool RestoredSomething { get; private set; }

        public BackupForm(string installDir)
        {
            _installDir = installDir;

            Text = "备份与还原";
            ClientSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);

            var lblTitle = new Label
            {
                Text = "每次点「应用修改」都会自动备份被改动的文件。选中一个备份可以还原它。",
                Location = new Point(12, 12),
                Size = new Size(576, 36),
                ForeColor = Color.DimGray
            };

            _list = new ListBox
            {
                Location = new Point(12, 52),
                Size = new Size(576, 260),
                IntegralHeight = false
            };

            _lblInfo = new Label
            {
                Location = new Point(12, 318),
                Size = new Size(576, 26),
                ForeColor = Color.Gray
            };

            var btnRestore = new Button
            {
                Text = "还原选中的备份",
                Location = new Point(12, 352),
                Width = 140,
                Height = 32
            };
            btnRestore.Click += delegate { RestoreSelected(); };

            var btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(500, 352),
                Width = 88,
                Height = 32,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblTitle);
            Controls.Add(_list);
            Controls.Add(_lblInfo);
            Controls.Add(btnRestore);
            Controls.Add(btnClose);
            CancelButton = btnClose;

            Load += delegate { Reload(); };
        }

        private void Reload()
        {
            _backups = BackupManager.ListBackups(_installDir);
            _list.Items.Clear();

            if (_backups.Count == 0)
            {
                _lblInfo.Text = "还没有任何备份。";
                return;
            }

            foreach (FileInfo f in _backups)
            {
                _list.Items.Add(string.Format("{0:yyyy-MM-dd HH:mm:ss}   {1:N0} 字节   {2}",
                    f.LastWriteTime, f.Length, f.Name));
            }

            _list.SelectedIndex = 0;
            _lblInfo.Text = "共 " + _backups.Count + " 个备份。";
        }

        private void RestoreSelected()
        {
            if (_backups == null || _backups.Count == 0) return;

            int idx = _list.SelectedIndex;
            if (idx < 0 || idx >= _backups.Count)
            {
                MessageBox.Show(this, "请先在列表里选中一个备份。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FileInfo chosen = _backups[idx];

            if (MessageBox.Show(this,
                    "将用这个备份覆盖当前文件：\r\n\r\n" + chosen.Name + "\r\n\r\n" +
                    "当前的改动会丢失（但还是会先自动备份一次）。确定吗？",
                    "确认还原", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            string restoredTo, error;
            // 还原前先把当前文件也备份一份，避免还原错了没退路
            try
            {
                string target = GameInstall.IsValid(_installDir)
                    ? GameInstall.GetSettingsPath(_installDir)
                    : null;

                if (target != null && File.Exists(target))
                    BackupManager.Backup(target, _installDir);
            }
            catch { }

            if (BackupManager.Restore(chosen, _installDir, out restoredTo, out error))
            {
                RestoredSomething = true;
                MessageBox.Show(this, "已还原到：\r\n" + restoredTo, "还原成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Reload();
            }
            else
            {
                MessageBox.Show(this, "还原失败：\r\n" + error, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
