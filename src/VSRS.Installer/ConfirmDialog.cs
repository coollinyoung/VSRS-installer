using System.Drawing;
using System.Windows.Forms;

namespace VSRS.Installer
{
    internal sealed class ConfirmDialog : Form
    {
        public ConfirmDialog(DiskInfo disk)
        {
            Text = "最後確認：將清除整顆目標磁碟";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 265);
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
                       disk + "\r\n\r\n已解除 USB 與 SSD 類型限制，目標可能是真正的內接資料碟。\r\n請核對磁碟編號、型號、容量與序號；按下安裝，表示您已確認目標並自行承擔資料清除風險。"
            };

            Button install = new Button { Left = 450, Top = 211, Width = 80, Text = "安裝", DialogResult = DialogResult.OK };
            Button cancel = new Button { Left = 540, Top = 211, Width = 80, Text = "取消", DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { warning, install, cancel });
            AcceptButton = install;
            CancelButton = cancel;
        }
    }
}
