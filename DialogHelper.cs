using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShutdownApp
{
    /// <summary>
    /// Clase auxiliar estática para la creación y gestión de ventanas de diálogo y controles relacionados.
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Centra una ventana en la pantalla y establece su tamaño.
        /// </summary>
        /// <param name="clientWidth">El ancho deseado para el área de cliente de la ventana.</param>
        /// <param name="clientHeight">La altura deseada para el área de cliente de la ventana.</param>
        /// <param name="window">La instancia de la ventana (Form) a centrar.</param>
        public static void CenterWindow(int clientWidth, int clientHeight, Form window)
        {
            window.ClientSize = new Size(clientWidth, clientHeight);
            window.StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// Crea un panel que contiene un botón personalizado para usar en diálogos.
        /// </summary>
        /// <param name="text">El texto a mostrar en el botón.</param>
        /// <param name="command">La acción a ejecutar cuando se hace clic en el botón.</param>
        /// <param name="buttonImage">La imagen a usar como fondo del botón.</param>
        /// <param name="width">El ancho del botón.</param>
        /// <param name="height">La altura del botón.</param>
        /// <param name="margin_top">El margen superior del panel contenedor del botón.</param>
        /// <returns>Un Panel que contiene el botón configurado.</returns>
        public static Panel CreateDialogButton(string text, Action command, Image? buttonImage, int width = UIConstants.ButtonWidth, int height = UIConstants.ButtonHeight, int margin_top = 0)
        {
            Button button = new Button();
            if (buttonImage != null)
            {
                button.Image = buttonImage;
            }
            button.Text = text;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.Click += (sender, e) => command();
            button.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            button.ForeColor = Color.White;
            button.Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeButton, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            button.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            
            int adjustedWidth = width;
            button.Size = new Size(adjustedWidth, height);
            button.Location = new Point(0, 0);

            Panel buttonContainer = new Panel();
            buttonContainer.Size = new Size(adjustedWidth, height);
            buttonContainer.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            buttonContainer.Margin = new Padding(0, margin_top, 0, 0);
            buttonContainer.Controls.Add(button);

            button.MouseEnter += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    int newWidth = adjustedWidth + UIConstants.EnlargementWidth;
                    int newHeight = height + UIConstants.EnlargementHeight;
                    int newX = (adjustedWidth - newWidth) / 2;
                    int newY = (height - newHeight) / 2;

                    btn.Size = new Size(newWidth, newHeight);
                    btn.Location = new Point(newX, newY);
                    btn.ForeColor = ColorTranslator.FromHtml(UIConstants.AccentColor);
                }
            };
            button.MouseLeave += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    btn.Size = new Size(adjustedWidth, height);
                    btn.Location = new Point(0, 0);
                    btn.ForeColor = Color.White;
                }
            };

            return buttonContainer;
        }
    }
}
