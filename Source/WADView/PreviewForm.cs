using System;
using System.Text;
using System.Windows.Forms;

namespace WADView
{
    public partial class PreviewForm : Form
    {
        private ArchiveFile file;
        public PreviewForm(ArchiveFile file)
        {
            InitializeComponent();
            dataText.Text = Encoding.ASCII.GetString(file.Data);
            this.file = file;
            this.Text = file.Name;
        }

        private void PreviewForm_Load(object sender, EventArgs e)
        {

        }

        private void PreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            file.Data = Encoding.ASCII.GetBytes(dataText.Text);
        }
    }
}
