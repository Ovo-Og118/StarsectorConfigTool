using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StarsectorConfigTool.Core;

namespace StarsectorConfigTool
{
    public class MainForm : Form
    {
        // ---- 顶部：游戏目录 ----
        private TextBox _txtDir;
        private Label _lblStatus;

        // ---- 中间：选项卡 ----
        private TabControl _tabs;

        // ---- 运存页 ----
        private Label _lblHeapNow;
        private Label _lblHeapFile;
        private NumericUpDown _numHeapMb;
        private TrackBar _trkHeapGb;
        private bool _syncingHeap;

        // ---- 设置页 ----
        private readonly Dictionary<string, NumericUpDown> _numControls =
            new Dictionary<string, NumericUpDown>();
        private readonly List<SettingDef> _allDefs = new List<SettingDef>();

        // ---- 底部 ----
        private TextBox _log;
        private Button _btnApply;

        // ---- 状态 ----
        private string _installDir;
        private SettingsEditor _settings;
        private JvmEditor _jvm;

        public MainForm()
        {
            BuildUi();
            Load += delegate { DetectAndLoad(); Shown += OnFirstShown; };
        }

        private bool _firstShownDone;
        private void OnFirstShown(object sender, EventArgs e)
        {
            if (_firstShownDone) return;
            _firstShownDone = true;
            Log("提示：修改前请先关闭游戏，否则改动可能被游戏退出时覆盖。");
        }

        // =====================================================================
        //  界面构建
        // =====================================================================

        private void BuildUi()
        {
            Text = "Starsector 基础配置工具  v1.0";
            ClientSize = new Size(880, 730);
            MinimumSize = new Size(900, 770);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            BuildTopPanel();
            BuildBottomPanel();
            BuildTabs();
        }

        private void BuildTopPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 74, Padding = new Padding(12, 10, 12, 6) };

            var lbl = new Label
            {
                Text = "游戏目录",
                Location = new Point(12, 14),
                AutoSize = true
            };

            _txtDir = new TextBox
            {
                Location = new Point(90, 10),
                Width = 596,
                ReadOnly = true,
                BackColor = SystemColors.Window
            };

            var btnBrowse = new Button
            {
                Text = "浏览...",
                Location = new Point(694, 9),
                Width = 76,
                Height = 26
            };
            btnBrowse.Click += delegate { BrowseInstallDir(); };

            var btnDetect = new Button
            {
                Text = "自动检测",
                Location = new Point(776, 9),
                Width = 88,
                Height = 26
            };
            btnDetect.Click += delegate { DetectAndLoad(); };

            _lblStatus = new Label
            {
                Location = new Point(92, 42),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Text = "未加载"
            };

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(_txtDir);
            pnl.Controls.Add(btnBrowse);
            pnl.Controls.Add(btnDetect);
            pnl.Controls.Add(_lblStatus);
            Controls.Add(pnl);
        }

        private void BuildBottomPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Bottom, Height = 158, Padding = new Padding(12, 6, 12, 10) };

            _btnApply = new Button
            {
                Text = "应用修改",
                Location = new Point(12, 6),
                Width = 120,
                Height = 32,
                Enabled = false
            };
            _btnApply.Click += delegate { ApplyChanges(); };

            var btnDefaults = new Button
            {
                Text = "恢复原版默认值",
                Location = new Point(140, 6),
                Width = 130,
                Height = 32
            };
            btnDefaults.Click += delegate { ResetToDefaults(); };

            var btnBackup = new Button
            {
                Text = "备份还原...",
                Location = new Point(278, 6),
                Width = 110,
                Height = 32
            };
            btnBackup.Click += delegate { ShowBackupDialog(); };

            var btnOpenBackup = new Button
            {
                Text = "打开备份文件夹",
                Location = new Point(396, 6),
                Width = 126,
                Height = 32
            };
            btnOpenBackup.Click += delegate { OpenBackupFolder(); };

            var lblLog = new Label
            {
                Text = "运行日志",
                Location = new Point(534, 13),
                AutoSize = true,
                ForeColor = Color.DimGray
            };

            _log = new TextBox
            {
                Location = new Point(12, 44),
                Width = 852,
                Height = 104,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(250, 250, 250),
                Font = new Font("Consolas", 9F)
            };

            pnl.Controls.Add(_btnApply);
            pnl.Controls.Add(btnDefaults);
            pnl.Controls.Add(btnBackup);
            pnl.Controls.Add(btnOpenBackup);
            pnl.Controls.Add(lblLog);
            pnl.Controls.Add(_log);
            Controls.Add(pnl);
        }

        private void BuildTabs()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
            _tabs.TabPages.Add(BuildHeapTab());
            _tabs.TabPages.Add(BuildSettingsTab("技能与等级", SettingCatalog.SkillGroup(),
                "这些是角色成长相关的全局数值。改完需要重新读档或升级一次才会体现在界面上。"));
            _tabs.TabPages.Add(BuildSettingsTab("舰队与战斗", SettingCatalog.FleetGroup(),
                "舰队规模与战斗规模。战斗规模调太高会明显掉帧，请按自己的机器量力而行。"));
            Controls.Add(_tabs);
            _tabs.BringToFront();
        }

        private TabPage BuildHeapTab()
        {
            var page = new TabPage("运存（JVM 堆）") { Padding = new Padding(14), BackColor = SystemColors.Control };

            var lblTitle = new Label
            {
                Text = "游戏分配的内存上限",
                Location = new Point(14, 14),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold)
            };

            var lblDesc = new Label
            {
                Location = new Point(14, 44),
                Size = new Size(820, 54),
                Text = "内存太小会在大规模战斗或加载大量 mod 时卡顿甚至崩溃；\r\n" +
                       "设置得比物理内存还大会导致游戏无法启动。一般建议物理内存的一半左右，且不宜超过 16 GB。"
            };

            var lblNowCaption = new Label
            {
                Text = "当前设置：",
                Location = new Point(14, 108),
                AutoSize = true
            };

            _lblHeapNow = new Label
            {
                Location = new Point(92, 108),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 0)
            };

            var lblFileCaption = new Label
            {
                Text = "参数文件：",
                Location = new Point(14, 136),
                AutoSize = true
            };

            _lblHeapFile = new Label
            {
                Location = new Point(92, 136),
                Size = new Size(760, 36),
                ForeColor = Color.DimGray
            };

            var lblNewCaption = new Label
            {
                Text = "改为：",
                Location = new Point(14, 186),
                AutoSize = true
            };

            _numHeapMb = new NumericUpDown
            {
                Location = new Point(92, 182),
                Width = 120,
                Minimum = 1024,
                Maximum = 65536,
                Increment = 512,
                ThousandsSeparator = true
            };
            _numHeapMb.ValueChanged += delegate { SyncHeapFromNumeric(); };

            var lblMb = new Label
            {
                Text = "MB",
                Location = new Point(218, 186),
                AutoSize = true
            };

            _trkHeapGb = new TrackBar
            {
                Location = new Point(92, 216),
                Width = 640,
                Minimum = 1,
                Maximum = 64,
                TickFrequency = 4,
                SmallChange = 1,
                LargeChange = 4
            };
            _trkHeapGb.ValueChanged += delegate { SyncHeapFromTrackbar(); };

            var lblGbMin = new Label { Text = "1 GB", Location = new Point(92, 262), AutoSize = true, ForeColor = Color.Gray };
            var lblGbMax = new Label { Text = "64 GB", Location = new Point(694, 262), AutoSize = true, ForeColor = Color.Gray };

            var lblQuick = new Label
            {
                Text = "快捷设定：",
                Location = new Point(14, 296),
                AutoSize = true
            };

            int x = 92;
            int[] quick = { 4, 6, 8, 12, 16, 24, 32 };
            foreach (int gb in quick)
            {
                var b = new Button
                {
                    Text = gb + " GB",
                    Location = new Point(x, 291),
                    Width = 68,
                    Height = 28
                };
                int captured = gb;
                b.Click += delegate { SetHeapMb(captured * 1024); };
                page.Controls.Add(b);
                x += 74;
            }

            page.Controls.Add(lblTitle);
            page.Controls.Add(lblDesc);
            page.Controls.Add(lblNowCaption);
            page.Controls.Add(_lblHeapNow);
            page.Controls.Add(lblFileCaption);
            page.Controls.Add(_lblHeapFile);
            page.Controls.Add(lblNewCaption);
            page.Controls.Add(_numHeapMb);
            page.Controls.Add(lblMb);
            page.Controls.Add(_trkHeapGb);
            page.Controls.Add(lblGbMin);
            page.Controls.Add(lblGbMax);
            page.Controls.Add(lblQuick);

            return page;
        }

        private TabPage BuildSettingsTab(string title, List<SettingDef> defs, string header)
        {
            var page = new TabPage(title) { Padding = new Padding(14), BackColor = SystemColors.Control, AutoScroll = true };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Padding = new Padding(0, 0, 0, 8)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));

            var lblHeader = new Label
            {
                Text = header,
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(3, 3, 3, 10)
            };
            table.Controls.Add(lblHeader, 0, 0);
            table.SetColumnSpan(lblHeader, 2);

            int row = 1;
            var tips = new ToolTip { AutoPopDelay = 12000, InitialDelay = 300, ReshowDelay = 100 };

            foreach (SettingDef def in defs)
            {
                _allDefs.Add(def);

                var lbl = new Label
                {
                    Text = def.Label,
                    AutoSize = true,
                    Margin = new Padding(3, 8, 3, 3)
                };

                var num = new NumericUpDown
                {
                    Width = 150,
                    Minimum = (decimal)def.Min,
                    Maximum = (decimal)def.Max,
                    Increment = (decimal)def.Step,
                    DecimalPlaces = def.Decimals,
                    Margin = new Padding(3, 6, 3, 3),
                    Tag = def
                };

                tips.SetToolTip(lbl, def.Tip);
                tips.SetToolTip(num, def.Tip);

                _numControls[def.Key] = num;

                table.Controls.Add(lbl, 0, row);
                table.Controls.Add(num, 1, row);
                row++;
            }

            page.Controls.Add(table);
            return page;
        }

        // =====================================================================
        //  加载 / 刷新
        // =====================================================================

        private void DetectAndLoad()
        {
            string dir = GameInstall.Detect();
            if (dir == null)
            {
                Log("未能自动找到 Starsector 安装目录，请点「浏览...」手动选择。");
                SetStatus("未找到游戏目录", false);
                return;
            }

            LoadInstall(dir, "自动检测");
        }

        private void BrowseInstallDir()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "请选择 Starsector 的安装根目录（里面有 starsector-core 文件夹的那个）";
                dlg.ShowNewFolderButton = false;

                if (_installDir != null && System.IO.Directory.Exists(_installDir))
                    dlg.SelectedPath = _installDir;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                if (!GameInstall.IsValid(dlg.SelectedPath))
                {
                    MessageBox.Show(this,
                        "这个目录看起来不是 Starsector 的安装目录。\r\n\r\n" +
                        "请选择包含 starsector-core 文件夹的那一层。",
                        "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoadInstall(dlg.SelectedPath, "手动选择");
            }
        }

        private void LoadInstall(string dir, string how)
        {
            if (!GameInstall.IsValid(dir))
            {
                SetStatus("目录无效", false);
                return;
            }

            _installDir = dir;
            GameInstall.Remember(dir);
            _txtDir.Text = dir;
            _tabs.Enabled = true;
            _btnApply.Enabled = true;

            Log("──────── 加载 ────────");
            Log("游戏目录（" + how + "）：" + dir);

            // settings.json
            try
            {
                _settings = new SettingsEditor(GameInstall.GetSettingsPath(dir));
                Log("已读取 settings.json");
            }
            catch (Exception ex)
            {
                _settings = null;
                Log("读取 settings.json 失败：" + ex.Message);
            }

            // JVM 参数
            _jvm = JvmEditor.Locate(dir);
            if (_jvm != null)
                Log("已定位 JVM 参数文件（" + _jvm.DetectionNote + "）：" + _jvm.FilePath);
            else
                Log("警告：没有找到含 -Xmx 的 JVM 参数文件，运存页不可用。");

            RefreshSettings();
            RefreshHeap();

            SetStatus("已加载，可以修改", true);
        }

        private void RefreshSettings()
        {
            if (_settings == null) return;

            int ok = 0, missing = 0;
            foreach (SettingDef def in _allDefs)
            {
                NumericUpDown num;
                if (!_numControls.TryGetValue(def.Key, out num)) continue;

                double v;
                if (_settings.TryGet(def.Key, out v))
                {
                    v = Math.Max(def.Min, Math.Min(def.Max, v));
                    try { num.Value = (decimal)v; } catch { }
                    ok++;
                }
                else
                {
                    num.Value = (decimal)def.Default;
                    missing++;
                }
            }

            Log(string.Format("settings.json 共 {0} 项，读到 {1} 项，缺失 {2} 项（缺失的项在应用时会跳过）",
                _allDefs.Count, ok, missing));
        }

        private void RefreshHeap()
        {
            if (_jvm == null)
            {
                _lblHeapNow.Text = "不可用";
                _lblHeapFile.Text = "没有找到 JVM 参数文件。";
                _numHeapMb.Enabled = false;
                _trkHeapGb.Enabled = false;
                return;
            }

            _numHeapMb.Enabled = true;
            _trkHeapGb.Enabled = true;
            _lblHeapFile.Text = _jvm.FilePath;

            int mb;
            if (_jvm.TryGetHeapMb(out mb))
            {
                _lblHeapNow.Text = string.Format("{0:N0} MB  ({1:0.#} GB)", mb, mb / 1024.0);
                SetHeapMb(mb);
            }
            else
            {
                _lblHeapNow.Text = "读取失败";
            }
        }

        private void SyncHeapFromNumeric()
        {
            if (_syncingHeap) return;
            _syncingHeap = true;
            try
            {
                int mb = (int)_numHeapMb.Value;
                int gb = (int)Math.Round(mb / 1024.0);
                gb = Math.Max(_trkHeapGb.Minimum, Math.Min(_trkHeapGb.Maximum, gb));
                _trkHeapGb.Value = gb;
            }
            finally { _syncingHeap = false; }
        }

        private void SyncHeapFromTrackbar()
        {
            if (_syncingHeap) return;
            _syncingHeap = true;
            try
            {
                _numHeapMb.Value = _trkHeapGb.Value * 1024;
            }
            finally { _syncingHeap = false; }
        }

        private void SetHeapMb(int mb)
        {
            _syncingHeap = true;
            try
            {
                mb = Math.Max((int)_numHeapMb.Minimum, Math.Min((int)_numHeapMb.Maximum, mb));
                _numHeapMb.Value = mb;
                int gb = (int)Math.Round(mb / 1024.0);
                gb = Math.Max(_trkHeapGb.Minimum, Math.Min(_trkHeapGb.Maximum, gb));
                _trkHeapGb.Value = gb;
            }
            finally { _syncingHeap = false; }
        }

        // =====================================================================
        //  应用修改
        // =====================================================================

        private void ApplyChanges()
        {
            if (_settings == null && _jvm == null)
            {
                MessageBox.Show(this, "没有可修改的文件。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (IsGameRunning())
            {
                DialogResult r = MessageBox.Show(this,
                    "检测到 Starsector 正在运行。\r\n\r\n" +
                    "在游戏运行时修改配置，改动可能会在游戏退出或存档时被覆盖。\r\n" +
                    "建议先关闭游戏再修改。\r\n\r\n仍然继续吗？",
                    "游戏正在运行", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }

            // 战斗规模一致性检查
            NumericUpDown numMax, numDefault;
            if (_numControls.TryGetValue("maxBattleSize", out numMax) &&
                _numControls.TryGetValue("defaultBattleSize", out numDefault))
            {
                if (numDefault.Value > numMax.Value)
                {
                    MessageBox.Show(this,
                        "「默认战斗规模」不能大于「战斗规模上限」。\r\n请先调整两者关系。",
                        "数值不合法", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (MessageBox.Show(this, "即将修改配置文件，修改前会自动备份。\r\n确定要应用吗？",
                    "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            Log("──────── 应用修改 ────────");
            int changedSettings = 0;
            int skipped = 0;
            string errors = string.Empty;

            // 1) settings.json
            if (_settings != null)
            {
                // 先算一下有没有实际变化，避免无意义地备份
                var pending = new List<KeyValuePair<string, SettingDef>>();
                foreach (SettingDef def in _allDefs)
                {
                    NumericUpDown num;
                    if (!_numControls.TryGetValue(def.Key, out num)) continue;

                    double oldValue;
                    if (!_settings.TryGet(def.Key, out oldValue)) { skipped++; continue; }

                    double newValue = (double)num.Value;
                    if (Math.Abs(oldValue - newValue) < 0.0001) continue;

                    pending.Add(new KeyValuePair<string, SettingDef>(def.Key, def));
                }

                if (pending.Count == 0)
                {
                    Log("settings.json 没有需要修改的项。");
                }
                else
                {
                    string backup = BackupManager.Backup(_settings.Path_, _installDir);
                    Log("已备份：" + backup);

                    foreach (KeyValuePair<string, SettingDef> kv in pending)
                    {
                        SettingDef def = kv.Value;
                        double newValue = (double)_numControls[def.Key].Value;

                        if (_settings.TrySet(def.Key, newValue, def.IsInt))
                        {
                            Log(string.Format("  {0} → {1}", def.Label, newValue));
                            changedSettings++;
                        }
                        else
                        {
                            Log(string.Format("  {0} 写入失败（键不存在或重复）", def.Label));
                            errors += def.Label + "\r\n";
                        }
                    }

                    try
                    {
                        _settings.Save();
                        Log("settings.json 已保存。");
                    }
                    catch (Exception ex)
                    {
                        Log("保存 settings.json 失败：" + ex.Message);
                        MessageBox.Show(this, "保存 settings.json 失败：\r\n" + ex.Message,
                            "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            // 2) JVM 参数
            if (_jvm != null)
            {
                int oldMb;
                bool hasOld = _jvm.TryGetHeapMb(out oldMb);
                int newMb = (int)_numHeapMb.Value;

                if (hasOld && oldMb == newMb)
                {
                    Log("运存没有变化，跳过。");
                }
                else
                {
                    string backup = BackupManager.Backup(_jvm.FilePath, _installDir);
                    Log("已备份：" + backup);

                    string err;
                    if (_jvm.TrySetHeapMb(newMb, out err))
                    {
                        try
                        {
                            _jvm.Save();
                            Log(string.Format("运存 → {0:N0} MB ({1:0.#} GB)", newMb, newMb / 1024.0));
                        }
                        catch (Exception ex)
                        {
                            Log("保存 JVM 参数失败：" + ex.Message);
                        }
                    }
                    else
                    {
                        Log("运存写入失败：" + err);
                    }
                }
            }

            if (skipped > 0)
                Log(string.Format("有 {0} 项在当前 settings.json 里找不到，已跳过。", skipped));

            Log(string.Format("完成：共修改 {0} 项设置。", changedSettings));
            Log("提示：重新启动游戏后生效。");

            // 重新读盘，保证界面显示的是真实值
            if (_settings != null)
            {
                try { _settings = new SettingsEditor(GameInstall.GetSettingsPath(_installDir)); }
                catch { }
            }
            if (_jvm != null)
            {
                JvmEditor again = JvmEditor.Locate(_installDir);
                if (again != null) _jvm = again;
            }
            RefreshSettings();
            RefreshHeap();
            SetStatus("修改已保存", true);
        }

        private void ResetToDefaults()
        {
            if (MessageBox.Show(this,
                    "把所有数值恢复成原版默认值（只是填到界面上，点「应用修改」才会真正写入）。\r\n\r\n继续吗？",
                    "恢复默认值", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            foreach (SettingDef def in _allDefs)
            {
                NumericUpDown num;
                if (_numControls.TryGetValue(def.Key, out num))
                    num.Value = (decimal)def.Default;
            }

            Log("已把各数值填为原版默认，尚未写入磁盘。");
        }

        // =====================================================================
        //  备份 / 杂项
        // =====================================================================

        private void ShowBackupDialog()
        {
            if (_installDir == null)
            {
                MessageBox.Show(this, "还没有加载游戏目录。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new BackupForm(_installDir))
            {
                dlg.ShowDialog(this);

                if (dlg.RestoredSomething)
                {
                    Log("已从备份还原文件。");
                    LoadInstall(_installDir, "还原后重新读取");
                }
            }
        }

        private void OpenBackupFolder()
        {
            if (_installDir == null) return;
            try
            {
                string dir = BackupManager.GetBackupDir(_installDir);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex)
            {
                Log("打开备份文件夹失败：" + ex.Message);
            }
        }

        private static bool IsGameRunning()
        {
            try
            {
                string[] names = { "java", "starsector", "Starsector" };
                foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
                {
                    foreach (string n in names)
                    {
                        if (string.Equals(p.ProcessName, n, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void Log(string message)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n";
            _log.AppendText(line);
        }

        private void SetStatus(string text, bool ok)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = ok ? Color.FromArgb(0, 120, 0) : Color.Firebrick;
        }
    }
}
