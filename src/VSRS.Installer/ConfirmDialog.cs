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
            Text = "最後確認：將清除整顆 SSD";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 265);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label warning = new Label
            {
                Left = 20,
                Top = 18,
                Width = 500,
                Height = 112,
                ForeColor = Color.DarkRed,
                Font = new Font(Font, FontStyle.Bold),
                Text = "下列外接 SSD 的所有分割區與資料都會被永久清除：\r\n\r\n" +
                       disk + "\r\n\r\n請確認型號、容量與序號完全正確。"
            };

            Label prompt = new Label
            {
                Left = 20,
                Top = 142,
                Width = 500,
                Text = "請輸入「" + _expected + "」以啟用安裝按鈕："
            };

            _confirmation = new TextBox { Left = 20, Top = 166, Width = 500 };
            Button install = new Button { Left = 350, Top = 211, Width = 80, Text = "安裝", Enabled = false, DialogResult = DialogResult.OK };
            Button cancel = new Button { Left = 440, Top = 211, Width = 80, Text = "取消", DialogResult = DialogResult.Cancel };
            _confirmation.TextChanged += (_, __) => install.Enabled = _confirmation.Text.Trim() == _expected;

            Controls.AddRange(new Control[] { warning, prompt, _confirmation, install, cancel });
            AcceptButton = install;
            CancelButton = cancel;
        }
    }
}
