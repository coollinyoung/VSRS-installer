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
            ClientSize = new Size(790, 520);
            MinimumSize = new Size(806, 559);
            Font = new Font("Microsoft JhengHei UI", 10F);

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
                ForeColor = Color.DarkRed,
                Text = "安全限制：只顯示 Windows 判定為 USB 匯流排、SSD 媒體類型且具有唯一識別碼的裝置；系統碟、開機碟、內接碟、唯讀碟與離線碟一律封鎖。安裝會清除整顆目標 SSD。"
            };

            Label selectLabel = new Label { Left = 24, Top = 130, Width = 180, Text = "選擇外接 SSD：" };
            _disks.SetBounds(24, 156, 610, 30);
            _disks.DropDownStyle = ComboBoxStyle.DropDownList;
            _refresh.SetBounds(648, 154, 116, 32);
            _refresh.Text = "重新偵測";
            _refresh.Click += (_, __) => RefreshDisks();

            _summary.SetBounds(24, 198, 740, 46);
            _summary.ForeColor = Color.DimGray;

            _gpt.SetBounds(24, 248, 300, 28);
            _gpt.Text = "使用 GPT 分割表（建議）";
            _gpt.Checked = true;

            _install.SetBounds(574, 242, 190, 40);
            _install.Text = "安裝 Ventoy 到外接 SSD";
            _install.Enabled = false;
            _install.Click += async (_, __) => await StartInstallAsync();

            _log.SetBounds(24, 304, 740, 190);
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Color.White;

            Controls.AddRange(new Control[] { title, warning, selectLabel, _disks, _refresh, _summary, _gpt, _install, _log });
            Shown += (_, __) => RefreshDisks();
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
