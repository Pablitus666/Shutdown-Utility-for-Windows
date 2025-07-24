using System.Windows.Forms;

namespace ShutdownApp
{
    public class NoFocusCueTextBox : TextBox
    {
        protected override bool ShowFocusCues
        {
            get { return false; }
        }
    }
}
