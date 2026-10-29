using System.Drawing;
using System.Windows.Forms;

namespace VSRS.Installer
{
    internal sealed class ConfirmDialog : Form
    {
        private readonly TextBox _confirmation;
        private readonly string _expected;

        public ConfirmDialog(DiskInfo disk)
        {
            _expected = "0";
            Text = "最後確認：將清除整顆目標磁碟";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 335);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label warning = new Label
            {
                Left = 20,
                Top = 18,
                Width = 600,
                Height = 180,
                ForeColor = Color.DarkRed,
                Font = new Font(Font, FontStyle.Bold),
                Text = "下列目標磁碟的所有分割區與資料都會被永久清除：\r\n\r\n" +
                       disk + "\r\n\r\n已解除 USB 與 SSD 類型限制，目標可能是真正的內接資料碟。\r\n請核對磁碟編號、型號、容量與序號；輸入 0 並按下安裝，表示您已確認目標並自行承擔資料清除風險。"
            };

            Label prompt = new Label
            {
                Left = 20,
                Top = 212,
                Width = 600,
                Text = "請輸入「" + _expected + "」以啟用安裝按鈕："
            };

            _confirmation = new TextBox { Left = 20, Top = 236, Width = 600 };
            Button install = new Button { Left = 450, Top = 281, Width = 80, Text = "安裝", Enabled = false, DialogResult = DialogResult.OK };
            Button cancel = new Button { Left = 540, Top = 281, Width = 80, Text = "取消", DialogResult = DialogResult.Cancel };
            _confirmation.TextChanged += (_, __) => install.Enabled = _confirmation.Text.Trim() == _expected;

            Controls.AddRange(new Control[] { warning, prompt, _confirmation, install, cancel });
            AcceptButton = install;
            CancelButton = cancel;
        }
    }
}
