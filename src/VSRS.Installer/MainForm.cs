using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSRS.Installer
{
    internal sealed class MainForm : Form
    {
        private readonly DiskService _diskService = new DiskService();
        private readonly ComboBox _disks = new ComboBox();
        private readonly Button _refresh = new Button();
        private readonly Button _install = new Button();
        private readonly CheckBox _gpt = new CheckBox();
        private readonly TextBox _log = new TextBox();
        private readonly Label _summary = new Label();

        public MainForm()
        {
            Text = "VSRS 外接 SSD 安裝工具";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(790, 420);
            MinimumSize = new Size(620, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            Font = new Font("Microsoft JhengHei UI", 10F);
            BackColor = Color.FromArgb(28, 39, 54);
            ForeColor = Color.FromArgb(235, 241, 248);
            DoubleBuffered = true;

            Label title = new Label
            {
                Left = 24,
                Top = 20,
                Width = 740,
                Height = 32,
                Text = "在外接 SSD 安裝 Ventoy 與 VSRS 資料結構",
                Font = new Font(Font.FontFamily, 16F, FontStyle.Bold)
            };
            Label warning = new Label
            {
                Left = 24,
                Top = 62,
                Width = 740,
                Height = 54,
                ForeColor = Color.FromArgb(255, 190, 158),
                Text = "安全限制：只顯示 Windows 判定為 USB 匯流排、SSD 媒體類型且具有唯一識別碼的裝置；系統碟、開機碟、內接碟、唯讀碟與離線碟一律封鎖。安裝會清除整顆目標 SSD。"
            };

            Label selectLabel = new Label { Left = 24, Top = 130, Width = 180, Text = "選擇外接 SSD：" };
            _disks.SetBounds(24, 156, 610, 30);
            _disks.DropDownStyle = ComboBoxStyle.DropDownList;
            _disks.BackColor = Color.White;
            _disks.ForeColor = Color.FromArgb(28, 39, 54);
            _disks.FlatStyle = FlatStyle.Flat;
            _disks.DrawMode = DrawMode.OwnerDrawFixed;
            _disks.ItemHeight = 26;
            _disks.DrawItem += DrawDiskItem;
            _refresh.SetBounds(648, 154, 116, 32);
            _refresh.Text = "重新偵測";
            StyleButton(_refresh, false);
            _refresh.Click += (_, __) => RefreshDisks();

            _summary.SetBounds(24, 198, 740, 46);
            _summary.ForeColor = Color.FromArgb(184, 199, 217);

            _gpt.SetBounds(24, 248, 300, 28);
            _gpt.Text = "使用 GPT 分割表（建議）";
            _gpt.Checked = true;

            _install.SetBounds(574, 242, 190, 40);
            _install.Text = "安裝 Ventoy 到外接 SSD";
            StyleButton(_install, true);
            _install.Enabled = false;
            _install.Click += async (_, __) => await StartInstallAsync();

            _log.SetBounds(24, 304, 740, 190);
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Color.FromArgb(20, 29, 42);
            _log.ForeColor = Color.FromArgb(205, 219, 236);
            _log.BorderStyle = BorderStyle.FixedSingle;

            // Auto-sized text rows wrap as the window narrows; the log uses remaining space.
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < 6; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            foreach (Label label in new[] { title, warning, selectLabel, _summary })
            {
                label.AutoSize = true;
                label.Dock = DockStyle.Fill;
                label.Margin = new Padding(0, 0, 0, 10);
            }

            TableLayoutPanel diskRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2,
                RowCount = 1, Margin = new Padding(0, 0, 0, 10)
            };
            diskRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            diskRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            _disks.Dock = DockStyle.Fill;
            _disks.Margin = new Padding(0, 2, 12, 0);
            _refresh.Dock = DockStyle.Fill;
            _refresh.Margin = Padding.Empty;
            diskRow.Controls.Add(_disks, 0, 0);
            diskRow.Controls.Add(_refresh, 1, 0);

            TableLayoutPanel actionRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2,
                RowCount = 1, Margin = new Padding(0, 0, 0, 12)
            };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            _gpt.AutoSize = true;
            _gpt.Anchor = AnchorStyles.Left;
            _gpt.Margin = Padding.Empty;
            _install.Dock = DockStyle.Fill;
            _install.Margin = new Padding(12, 0, 0, 0);
            actionRow.Controls.Add(_gpt, 0, 0);
            actionRow.Controls.Add(_install, 1, 0);

            _log.Size = new Size(740, 90);
            _log.MinimumSize = new Size(0, 70);
            _log.Dock = DockStyle.Fill;
            _log.Margin = Padding.Empty;
            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(warning, 0, 1);
            layout.Controls.Add(selectLabel, 0, 2);
            layout.Controls.Add(diskRow, 0, 3);
            layout.Controls.Add(_summary, 0, 4);
            layout.Controls.Add(actionRow, 0, 5);
            layout.Controls.Add(_log, 0, 6);
            Controls.Add(layout);
            Shown += (_, __) => RefreshDisks();
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? Color.FromArgb(44, 103, 177) : Color.FromArgb(47, 64, 85);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(81, 144, 222) : Color.FromArgb(88, 108, 133);
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(55, 122, 202) : Color.FromArgb(61, 81, 106);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(33, 79, 139) : Color.FromArgb(36, 51, 70);
        }

        private void DrawDiskItem(object sender, DrawItemEventArgs e)
        {
            // Keep the collapsed selector white even when Windows applies a theme.
            bool highlighted = (e.State & DrawItemState.Selected) != 0
                && (e.State & DrawItemState.ComboBoxEdit) == 0;
            Color background = highlighted ? Color.FromArgb(226, 237, 251) : Color.White;
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);
            if (e.Index >= 0)
            {
                Rectangle bounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y,
                    Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, _disks.GetItemText(_disks.Items[e.Index]),
                    e.Font ?? _disks.Font, bounds, Color.FromArgb(28, 39, 54),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            e.DrawFocusRectangle();
        }

        private void RefreshDisks()
        {
            try
            {
                IReadOnlyList<DiskInfo> all = _diskService.GetAllDisks();
                List<DiskInfo> eligible = all.Where(d => d.IsEligible).ToList();
                _disks.DataSource = null;
                _disks.DataSource = eligible;
                _install.Enabled = eligible.Count > 0;
                int blocked = all.Count - eligible.Count;
                _summary.Text = eligible.Count == 0
                    ? $"找不到符合條件的外接 SSD（已封鎖 {blocked} 顆其他磁碟）。未回報 SSD 類型或唯一識別碼的 USB 轉接盒，基於安全理由也不會列出。"
                    : $"找到 {eligible.Count} 顆可用外接 SSD；另有 {blocked} 顆磁碟因安全規則未顯示。";
                AppendLog("磁碟清單已更新。");
            }
            catch (Exception ex)
            {
                _install.Enabled = false;
                MessageBox.Show("無法取得磁碟資訊：" + ex.Message, "VSRS Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StartInstallAsync()
        {
            DiskInfo selected = _disks.SelectedItem as DiskInfo;
            if (selected == null)
                return;

            using (ConfirmDialog confirm = new ConfirmDialog(selected))
            {
                if (confirm.ShowDialog(this) != DialogResult.OK)
                    return;
            }

            SetBusy(true);
            try
            {
                VentoyInstaller installer = new VentoyInstaller(_diskService, AppendLog);
                await Task.Run(() => installer.Install(selected, _gpt.Checked));
                MessageBox.Show("Ventoy、資料夾與預置資料均已安裝完成。", "完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("錯誤：" + ex.Message);
                MessageBox.Show(ex.Message, "安裝失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                RefreshDisks();
            }
        }

        private void SetBusy(bool busy)
        {
            _install.Enabled = !busy && _disks.Items.Count > 0;
            _refresh.Enabled = !busy;
            _disks.Enabled = !busy;
            _gpt.Enabled = !busy;
            UseWaitCursor = busy;
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }
            _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        }
    }
}
