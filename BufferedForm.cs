using System.Windows.Forms;

namespace ShutdownApp
{
    public class BufferedForm : Form
    {
        public BufferedForm()
        {
            // Habilitar DoubleBuffered para reducir el parpadeo
            this.DoubleBuffered = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}
