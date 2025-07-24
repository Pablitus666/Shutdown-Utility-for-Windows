using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ShutdownApp
{
    /// <summary>
    /// Clase principal del formulario de la aplicación ShutdownApp.
    /// Gestiona la interfaz de usuario y la interacción con la lógica de apagado.
    /// </summary>
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        // Variables para las imágenes
        private Image? clickImagen;
        private Image? nombreImagen;
        private Image? botonImagen;
        private Image? robotImagen;

        // Controles de la UI
        private Label labelResultado = null!;
        private NoFocusCueTextBox entryCustomTime = null!;
        private Button botonProgramar = null!;
        private Label labelErrorCustomTime = null!; // Declaración del Label para el mensaje de error
        private Panel entryPanel = null!;

        // Manejador de la lógica de apagado
        private readonly ShutdownManager shutdownManager = new ShutdownManager();
        
        private bool _shouldClearInput = false;

        /// <summary>
        /// Constructor de la clase Form1.
        /// Inicializa los componentes de la UI, carga la configuración y las imágenes.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        public Form1()
        {
                        LocalizationManager.LoadCurrentLanguage();

            InitializeComponent();

            // Habilitar DoubleBuffered para reducir el parpadeo
            this.DoubleBuffered = true;

            // Ocultar la ventana al inicio para evitar parpadeo
            this.Hide();

            // Configuración de la ventana principal
            this.Text = LocalizationManager.GetString("AppTitle");
            this.BackColor = ColorTranslator.FromHtml("#023047");
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Equivalente a resizable(False, False)
            this.MaximizeBox = false;
            this.MinimizeBox = true; // Habilitar el botón de minimizar
            this.KeyPreview = true; // Permitir que el formulario reciba eventos de teclado primero
            this.StartPosition = FormStartPosition.CenterScreen;

            // Cargar imágenes
            LoadImages();

            // Centrar la ventana (aumentar altura para los botones inferiores)
            

            // Crear widgets
            CreateWidgets();

            // Asociar teclas a funciones
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);

            // Mostrar ventana y aplicar autofocus
            this.Shown += (sender, e) =>
            {
                this.Show();
                if (entryCustomTime != null)
                {
                    entryCustomTime.Focus();
                }
            };
        }

        /// <summary>
        /// Maneja los eventos de teclado del formulario.
        /// Permite programar el apagado con Enter y cancelar con Escape.
        /// </summary>
        /// <param name="sender">El objeto que generó el evento.</param>
        /// <param name="e">Argumentos del evento de teclado.</param>
        private async void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                await ProgramarApagadoPersonalizado();
                e.Handled = true; // Evita que el sonido de "ding" de Windows suene
                e.SuppressKeyPress = true; // Previene que otros controles procesen el Enter
            }
            else if (e.KeyCode == Keys.Escape)
            {
                await CancelarApagado();
            }
        }

        private void CenterWindow(int clientWidth, int clientHeight, Form? window = null)
        {
            if (window == null)
            {
                window = this;
            }
            window.ClientSize = new Size(clientWidth, clientHeight); // Establecer ClientSize directamente
            window.StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// Carga las imágenes necesarias para la interfaz de usuario desde el directorio 'images'.
        /// Lanza una excepción si la imagen del botón principal no se encuentra.
        /// </summary>
        private void LoadImages()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string images_dir = Path.Combine(exePath, "images");

            // Cargar y establecer el icono de la aplicación
            string icon_path = Path.Combine(images_dir, "icon.png");
            if (File.Exists(icon_path))
            {
                try
                {
                    // Intentar cargar como Bitmap primero
                    using (Bitmap bmp = new Bitmap(icon_path))
                    {
                        // Intentar convertir a Icon
                        this.Icon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"No se pudo cargar el icono en {icon_path}: {ex.Message}");
                    // Si falla, el icono de la ventana será el predeterminado
                }
            }

            string ruta_click = Path.Combine(images_dir, "click.png");
            string ruta_nombre = Path.Combine(images_dir, "nombre.png");
            string ruta_boton = Path.Combine(images_dir, "boton.png");
            string ruta_robot = Path.Combine(images_dir, "robot.png");

            clickImagen = CargarImagenAjustada(ruta_click, new Size(100, 180));
                        nombreImagen = CargarImagenAjustada(ruta_nombre, new Size(450, 130));
                        botonImagen = CargarImagen(ruta_boton, new Size(UIConstants.ButtonWidth, UIConstants.ButtonHeight));
            if (botonImagen == null)
            {
                throw new InvalidOperationException(LocalizationManager.GetString("WarningInterfaceMessage"));
            }
            robotImagen = CargarImagenAjustada(ruta_robot, new Size(120, 150));
        }

        /// <summary>
        /// Carga una imagen desde una ruta específica y la redimensiona a un tamaño dado.
        /// </summary>
        /// <param name="ruta">La ruta completa del archivo de imagen.</param>
        /// <param name="size">El tamaño deseado para la imagen (ancho y alto).</param>
        /// <returns>La imagen redimensionada, o null si ocurre un error.</returns>
        private Image? CargarImagen(string ruta, Size size)
        {
            try
            {
                using (FileStream fs = new FileStream(ruta, FileMode.Open, FileAccess.Read))
                {
                    using (Image originalImage = Image.FromStream(fs))
                    {
                        return originalImage.GetThumbnailImage(size.Width, size.Height, null, IntPtr.Zero);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error al cargar la imagen {ruta}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Carga una imagen desde una ruta específica y la redimensiona proporcionalmente
        /// para que se ajuste dentro de un tamaño máximo dado.
        /// </summary>
        /// <param name="ruta">La ruta completa del archivo de imagen.</param>
        /// <param name="max_size">El tamaño máximo deseado para la imagen (ancho y alto).</param>
        /// <returns>La imagen redimensionada proporcionalmente, o null si ocurre un error.</returns>
        private Image? CargarImagenAjustada(string ruta, Size max_size)
        {
            try
            {
                using (FileStream fs = new FileStream(ruta, FileMode.Open, FileAccess.Read))
                {
                    using (Image img = Image.FromStream(fs))
                    {
                        int original_width = img.Width;
                        int original_height = img.Height;
                        int max_width = max_size.Width;
                        int max_height = max_size.Height;

                        double aspect_ratio = (double)original_width / original_height;
                        int new_width, new_height;

                        if (original_width > original_height)
                        {
                            new_width = Math.Min(max_width, original_width);
                            new_height = (int)(new_width / aspect_ratio);
                        }
                        else
                        {
                            new_height = Math.Min(max_height, original_height);
                            new_width = (int)(new_height * aspect_ratio);
                        }

                        return img.GetThumbnailImage(new_width, new_height, null, IntPtr.Zero);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error al cargar la imagen ajustada {ruta}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea y organiza todos los controles de la interfaz de usuario en el formulario.
        /// </summary>
        private void CreateWidgets()
        {
            this.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);

            InitializeHeaderControls();
            InitializePresetTimeButtons();
            InitializeCustomTimeControls();
            InitializeResultLabel();
            InitializeBottomButtons();
        }

        /// <summary>
        /// Inicializa y posiciona los controles de la sección superior del formulario (etiquetas de bienvenida, imagen de nombre, etc.).
        /// </summary>
        private void InitializeHeaderControls()
        {
            // --- Controles Superiores ---
            PictureBox? pictureBox_click = null;
            if (clickImagen != null)
            {
                pictureBox_click = new PictureBox
                {
                    Image = clickImagen,
                    BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor),
                    Cursor = Cursors.Hand,
                    Location = new Point(UIConstants.ClickLabelX, UIConstants.ClickLabelY),
                    Size = clickImagen.Size,
                    SizeMode = PictureBoxSizeMode.AutoSize
                };
                pictureBox_click.Click += (sender, e) => ShowInfo();
                this.Controls.Add(pictureBox_click);
            }

            if (nombreImagen != null)
            {
                var pictureBox_nombre = new PictureBox
                {
                    Image = nombreImagen,
                    BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor),
                    Size = nombreImagen.Size,
                    SizeMode = PictureBoxSizeMode.AutoSize
                };

                // Centrar la imagen 'nombre' en el espacio restante
                int clickImageRight = pictureBox_click?.Right ?? 0;
                int availableWidth = this.ClientSize.Width - clickImageRight;
                int newX = clickImageRight + (availableWidth - pictureBox_nombre.Width) / 2 - 20;

                pictureBox_nombre.Location = new Point(newX, UIConstants.NameImageY);
                this.Controls.Add(pictureBox_nombre);
                pictureBox_nombre.BringToFront();
            }
            this.Controls.Add(new Label { Text = LocalizationManager.GetString("TimeInputLabel"), Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeMedium, FontStyle.Bold), BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor), ForeColor = Color.White, Location = new Point(UIConstants.TimeLabelX, UIConstants.TimeLabelY), AutoSize = true });
        }

        /// <summary>
        /// Inicializa y posiciona los botones de tiempo predefinido para el apagado.
        /// </summary>
        private void InitializePresetTimeButtons()
        {
            // --- Botones de Tiempo Predefinido (Cálculo Manual Preciso para Simetría Perfecta) ---
            int[] tiempos = { 10, 20, 30, 40, 50, 60, 90, 120, 150, 180, 210, 240 };
            for (int idx = 0; idx < tiempos.Length; idx++)
            {
                int currentTiempo = tiempos[idx];
                int fila = idx / 4;
                int columna = idx % 4;
                int x_pos = UIConstants.LargeMargin + columna * (UIConstants.ButtonWidth + UIConstants.ButtonSpacingX);
                int y_pos = UIConstants.TimeButtonsStartY + fila * UIConstants.ButtonSpacingY;
                var button = CreateCustomButton($"{currentTiempo} min", async () => await ProgramarApagado(currentTiempo), x_pos, y_pos, applyEnlargementHover: true);
                this.Controls.Add(button);
            }
        }

        /// <summary>
        /// Inicializa y posiciona los controles para la entrada de tiempo personalizado y el botón de programar.
        /// </summary>
        private void InitializeCustomTimeControls()
        {
            // --- Controles de Tiempo Personalizado (Centrados con Panel como borde) ---
            entryPanel = new Panel
            {
                Size = new Size(UIConstants.CustomEntryWidth, UIConstants.CustomEntryHeight),
                BackColor = Color.White,
                Padding = new Padding(1), // Simula el borde
                BorderStyle = BorderStyle.FixedSingle 
            };

            entryCustomTime = new NoFocusCueTextBox
            {
                Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeMedium, FontStyle.Bold),
                Dock = DockStyle.Fill, // Rellena el panel
                TextAlign = HorizontalAlignment.Center,
                ForeColor = ColorTranslator.FromHtml(UIConstants.CustomEntryTextColor),
                BackColor = Color.White,
                MaxLength = 4,
                BorderStyle = BorderStyle.None // Sin borde en el TextBox
            };

            entryPanel.Controls.Add(entryCustomTime);

            entryCustomTime.KeyPress += new KeyPressEventHandler(ValidateNumericInput);
            entryCustomTime.KeyDown += (sender, e) => { if (e.KeyCode == Keys.Delete) { entryCustomTime.Clear(); e.Handled = true; } };
            entryCustomTime.GotFocus += (sender, e) => HideCaret(entryCustomTime.Handle);
            
            botonProgramar = CreateCustomButton(LocalizationManager.GetString("ProgramButton"), async () => await ProgramarApagadoPersonalizado(), 0, 0, applyEnlargementHover: true);
            
            int totalCustomWidth = entryPanel.Width + UIConstants.CustomButtonSpacing + botonProgramar.Width;
            int customStartX = (this.ClientSize.Width - totalCustomWidth) / 2;
            
            entryPanel.Location = new Point(customStartX, UIConstants.CustomEntryY);
            botonProgramar.Location = new Point(entryPanel.Right + UIConstants.CustomButtonSpacing, UIConstants.CustomButtonY);
            
            this.Controls.Add(entryPanel);
            this.Controls.Add(botonProgramar);

            // Inicializar Label de error
            labelErrorCustomTime = new Label
            {
                Text = "", // Inicialmente vacío
                Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeSmall, FontStyle.Regular),
                ForeColor = Color.White, // Color blanco para el error
                BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor),
                AutoSize = true,
                Visible = false // Oculto por defecto
            };
            this.Controls.Add(labelErrorCustomTime);

            // Posicionar el label de error debajo del panel
            labelErrorCustomTime.Location = new Point(entryPanel.Left, entryPanel.Bottom + 5);

            // Asociar eventos de validación
            entryCustomTime.TextChanged += (sender, e) => labelErrorCustomTime.Visible = false; // Ocultar error al escribir
        }

        /// <summary>
        /// Inicializa y posiciona la etiqueta donde se muestra el resultado de la programación del apagado.
        /// </summary>
        private void InitializeResultLabel()
        {
            // --- Label de Resultado (Centrado) ---
                        labelResultado = new Label { Text = LocalizationManager.GetString("ResultLabelDefault"), Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeSmall, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml(UIConstants.CustomEntryTextColor), BackColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(this.ClientSize.Width - (UIConstants.LargeMargin * 2), UIConstants.ResultLabelHeight), Location = new Point(UIConstants.LargeMargin, UIConstants.ResultLabelY) };
            this.Controls.Add(labelResultado);
        }

        /// <summary>
        /// Inicializa y posiciona los botones inferiores del formulario (Cancelar y Salir).
        /// Utiliza un TableLayoutPanel para asegurar la simetría.
        /// </summary>
        private void InitializeBottomButtons()
        {
            // --- Botones Inferiores (con TableLayoutPanel para simetría perfecta) ---
            var bottomButtonsPanel = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                Location = new Point(0, UIConstants.BottomButtonsY),
                Size = new Size(this.ClientSize.Width, UIConstants.ButtonHeight + UIConstants.BottomPanelHeightOffset), // Altura para los botones + margen
                BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None // Sin bordes de celda
            };
            // Columnas: 50% para margen izquierdo, AutoSize para los botones, 50% para margen derecho
            bottomButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Crear un panel intermedio para los dos botones y su espacio
            var innerButtonsPanel = new Panel
            {
                Size = new Size((UIConstants.ButtonWidth * 2) + UIConstants.SpaceBetweenBottomButtons, UIConstants.ButtonHeight),
                BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor),
                Anchor = AnchorStyles.None // Centrar este panel en su celda
            };

            var cancelButton = CreateCustomButton(LocalizationManager.GetString("CancelButton"), async () => await CancelarApagado(), 0, 0, applyEnlargementHover: false);
            var exitButton = CreateCustomButton(LocalizationManager.GetString("ExitButton"), () => Application.Exit(), UIConstants.ButtonWidth + UIConstants.SpaceBetweenBottomButtons, 0, applyEnlargementHover: false);

            innerButtonsPanel.Controls.Add(cancelButton);
            innerButtonsPanel.Controls.Add(exitButton);

            bottomButtonsPanel.Controls.Add(innerButtonsPanel, 1, 0); // Añadir el panel intermedio a la columna central
            this.Controls.Add(bottomButtonsPanel);
        }

        /// <summary>
        /// Crea un botón personalizado con el estilo definido por la aplicación.
        /// </summary>
        /// <param name="text">El texto a mostrar en el botón.</param>
        /// <param name="command">La acción a ejecutar cuando se hace clic en el botón.</param>
        /// <param name="x">La posición X del botón.</param>
        /// <param name="y">La posición Y del botón.</param>
        /// <param name="width">El ancho del botón.</param>
        /// <param name="height">La altura del botón.</param>
        /// <param name="applyEnlargementHover">Indica si se debe aplicar el efecto de agrandamiento al pasar el ratón.</param>
        /// <returns>Un control Button configurado.</returns>
        private Button CreateCustomButton(string text, Action command, int x, int y, int width = UIConstants.ButtonWidth, int height = UIConstants.ButtonHeight, bool applyEnlargementHover = false)
        {
            Button button = new Button();
            if (botonImagen != null) // Comprobación de nulidad antes de asignar la imagen
            {
                button.Image = botonImagen;
            }
            button.Text = text;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.Click += (sender, e) => command();
            button.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            button.ForeColor = Color.White;
            button.Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeButton, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0; // Eliminar el borde
            button.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            button.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            
            // Usar ancho fijo y mantener la variable adjustedWidth para el efecto hover
            int adjustedWidth = width;
            button.Size = new Size(adjustedWidth, height);
            button.Location = new Point(x, y);

            // Efecto de cambio de color al pasar el ratón
            button.MouseEnter += (sender, e) => { if (sender is Button btn) btn.ForeColor = ColorTranslator.FromHtml(UIConstants.AccentColor); };
            button.MouseLeave += (sender, e) => { if (sender is Button btn) btn.ForeColor = Color.White; };

            // Efecto de agrandamiento solo si applyEnlargementHover es true
            if (applyEnlargementHover)
            {
                button.MouseEnter += (sender, e) =>
                {
                    if (sender is Button btn)
                    {
                        btn.BringToFront(); // Traer al frente para que no se superponga

                        // Guardar el tamaño y la posición originales
                        btn.Tag = new Tuple<Size, Point>(btn.Size, btn.Location);

                        // Calcular el nuevo tamaño y posición para centrar el agrandamiento
                        int newWidth = adjustedWidth + UIConstants.EnlargementWidth;
                        int newHeight = height + UIConstants.EnlargementHeight;
                        int newX = btn.Location.X - (newWidth - btn.Width) / 2;
                        int newY = btn.Location.Y - (newHeight - btn.Height) / 2;

                        btn.Size = new Size(newWidth, newHeight);
                        btn.Location = new Point(newX, newY);
                    }
                };
                button.MouseLeave += (sender, e) =>
                {
                    if (sender is Button btn && btn.Tag is Tuple<Size, Point> originalState)
                    {
                        // Restaurar el tamaño y la posición originales
                        btn.Size = originalState.Item1;
                        btn.Location = originalState.Item2;
                    }
                };
            }

            return button;
        }

        /// <summary>
        /// Programa el apagado del sistema después de un número específico de minutos.
        /// </summary>
        /// <param name="tiempo_minutos">El número de minutos para programar el apagado.</param>
        private async Task ProgramarApagado(int tiempo_minutos)
        {
            labelErrorCustomTime.Visible = false; // Ocultar mensaje de error
            try
            {
                if (await shutdownManager.ScheduleShutdown(tiempo_minutos))
                {
                                        MostrarMensaje(string.Format(LocalizationManager.GetString("ShutdownScheduledMessage"), tiempo_minutos));
                    botonProgramar.Text = LocalizationManager.GetString("ReprogramButton");
                    
                    _shouldClearInput = true;
                    entryCustomTime.Clear();
                }
                else
                {
                    CustomErrorMessage(LocalizationManager.GetString("ErrorDialogTitle"), LocalizationManager.GetString("ShutdownScheduleError"));
                }
            }
            catch (ArgumentException ex)
            {
                Logger.LogException(ex, "Error de argumento al programar el apagado.");
                CustomErrorMessage(LocalizationManager.GetString("ErrorDialogTitle"), ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error inesperado al programar el apagado.");
                CustomErrorMessage(LocalizationManager.GetString("ErrorDialogTitle"), LocalizationManager.GetString("UnexpectedShutdownError"));
            }
            if (entryCustomTime != null)
            {
                entryCustomTime.Focus();
            }
        }

        /// <summary>
        /// Valida la entrada del usuario para el tiempo de apagado personalizado y programa el apagado si es válido.
        /// </summary>
        private async Task ProgramarApagadoPersonalizado()
        {
            if (!ValidateCustomTimeInput())
            {
                return; // Si la validación falla, no continuar
            }

            try
            {
                string minutos_str = entryCustomTime.Text;
                long minutos_long = long.Parse(minutos_str); // Ya validado que es un número válido

                await ProgramarApagado((int)minutos_long);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error inesperado al programar el apagado personalizado.");
                CustomErrorMessage(LocalizationManager.GetString("ErrorDialogTitle"), LocalizationManager.GetString("UnexpectedShutdownError"));
            }
        }

        /// <summary>
        /// Cancela cualquier apagado del sistema programado previamente.
        /// </summary>
        private async Task CancelarApagado()
        {
            labelErrorCustomTime.Visible = false; // Ocultar mensaje de error
            try
            {
                if (shutdownManager.IsShutdownScheduled)
                {
                    if (await shutdownManager.CancelShutdown())
                    {
                        MostrarMensaje(LocalizationManager.GetString("ShutdownCancelledMessage"));
                        botonProgramar.Text = LocalizationManager.GetString("ProgramButton");
                        
                        _shouldClearInput = false;
                    }
                    else
                    {
                        Logger.LogError("No se pudo cancelar el apagado.");
                        CustomErrorMessage(LocalizationManager.GetString("ErrorDialogTitle"), LocalizationManager.GetString("ShutdownCancelError"));
                    }
                }
                else
                {
                    MostrarMensaje(LocalizationManager.GetString("NoShutdownScheduled"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, LocalizationManager.GetString("LogUnexpectedCancelError"));
                CustomErrorMessage(LocalizationManager.GetString("ErrorDialogTitle"), LocalizationManager.GetString("UnexpectedShutdownError"));
            }
            if (entryCustomTime != null)
            {
                entryCustomTime.Clear();
                entryCustomTime.Focus();
            }
        }

        /// <summary>
        /// Muestra un mensaje en la etiqueta de resultado del formulario.
        /// </summary>
        /// <param name="mensaje">El mensaje a mostrar.</param>
        private void MostrarMensaje(string mensaje)
        {
            labelResultado.Text = mensaje;
        }

        private Form? error_window = null;
        /// <summary>
        /// Muestra una ventana de mensaje de error personalizada.
        /// </summary>
        /// <param name="title">El título de la ventana de error.</param>
        /// <param name="message">El mensaje de error a mostrar.</param>
        private void CustomErrorMessage(string title, string message)
        {
            if (error_window != null && error_window.Created)
            {
                return;
            }

            error_window = new BufferedForm();
            error_window.Text = title;
            error_window.BackColor = ColorTranslator.FromHtml("#023047");
            error_window.FormBorderStyle = FormBorderStyle.FixedSingle;
            error_window.MaximizeBox = false;
            error_window.MinimizeBox = false;
            error_window.ShowInTaskbar = false;
            error_window.KeyPreview = true;

            error_window.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                {
                    error_window?.Close();
                }
            };

            error_window.FormClosed += (sender, e) => 
            {
                error_window?.Dispose();
                error_window = null;
            };

            if (this.Icon != null)
            {
                error_window.Icon = this.Icon;
            }

            // Establecer el tamaño de la ventana ANTES de añadir controles
            DialogHelper.CenterWindow(UIConstants.ErrorWindowWidth, UIConstants.ErrorWindowHeight, error_window);

            // El Panel nos sirve como contenedor principal
            Panel frame = new Panel();
            frame.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            frame.Dock = DockStyle.Fill;
            error_window.Controls.Add(frame);

            Label msg_label = new Label();
            msg_label.Text = message;
            msg_label.Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeSmall, FontStyle.Bold);
            msg_label.ForeColor = Color.White;
            msg_label.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            msg_label.TextAlign = ContentAlignment.MiddleCenter;
            msg_label.Size = new Size(frame.ClientSize.Width - (UIConstants.DefaultMargin * 2), 80);
            msg_label.Location = new Point(UIConstants.DefaultMargin, 15); // Margen superior de 15px
            msg_label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            frame.Controls.Add(msg_label);

            if (botonImagen != null)
            {
                Panel closeButtonPanel = DialogHelper.CreateDialogButton(LocalizationManager.GetString("CloseButton"), () => error_window?.Close(), botonImagen, margin_top: UIConstants.SmallMargin);
                // Posicionar el panel del botón 5px debajo de la etiqueta
                int button_y = msg_label.Bottom + UIConstants.SmallMargin;
                closeButtonPanel.Location = new Point((frame.ClientSize.Width - closeButtonPanel.Width) / 2, button_y);
                closeButtonPanel.Anchor = AnchorStyles.Top; // Anclar solo a la parte superior
                frame.Controls.Add(closeButtonPanel);
            }

            error_window.ShowDialog();
            if (entryCustomTime != null)
            {
                entryCustomTime.Focus();
            }
        }

        private Form? info_window = null;
        /// <summary>
        /// Muestra una ventana de información personalizada con detalles de la aplicación.
        /// </summary>
        private void ShowInfo()
        {
            if (info_window != null && info_window.Created)
            {
                return;
            }

            this.SuspendLayout(); // Suspender el diseño del formulario principal

            info_window = new BufferedForm();
            info_window.Text = LocalizationManager.GetString("InfoTitle");
            info_window.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            info_window.FormBorderStyle = FormBorderStyle.FixedSingle;
            info_window.MaximizeBox = false;
            info_window.MinimizeBox = false;
            info_window.ShowInTaskbar = false;
            info_window.KeyPreview = true; // Capturar teclas

            info_window.SuspendLayout(); // Suspender el diseño de la ventana de información

            // Evento KeyDown para cerrar con Enter o Escape
            info_window.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                {
                    info_window?.Close();
                }
            };

            info_window.FormClosed += (sender, e) => 
            {
                info_window?.Dispose();
                info_window = null;
                this.ResumeLayout(false); // Reanudar el diseño del formulario principal al cerrar la ventana de información
            };

            // Aumentar la altura de la ventana de información
            DialogHelper.CenterWindow(UIConstants.InfoWindowWidth, UIConstants.InfoWindowHeight, info_window); 

            if (this.Icon != null)
            {
                info_window.Icon = this.Icon;
            }

            Panel frame_info = new Panel();
            frame_info.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            frame_info.Padding = new Padding(UIConstants.DefaultMargin);
            frame_info.Dock = DockStyle.Fill;
            info_window.Controls.Add(frame_info);

            // Layout para la imagen y el texto
            TableLayoutPanel tableLayout = new TableLayoutPanel();
            tableLayout.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            tableLayout.ColumnCount = 2;
            tableLayout.RowCount = 2;
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayout.Dock = DockStyle.Fill;
            frame_info.Controls.Add(tableLayout);

            if (robotImagen != null)
            {
                PictureBox img_label = new PictureBox();
                img_label.Image = robotImagen;
                img_label.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
                img_label.SizeMode = PictureBoxSizeMode.AutoSize;
                tableLayout.Controls.Add(img_label, 0, 0);
                tableLayout.SetRowSpan(img_label, 2);
            }

            Label message = new Label();
            message.Text = LocalizationManager.GetString("InfoMessage");
            message.TextAlign = ContentAlignment.MiddleCenter;
            message.BackColor = ColorTranslator.FromHtml(UIConstants.PrimaryBackgroundColor);
            message.ForeColor = Color.White;
            message.Font = new Font(UIConstants.DefaultFontFamily, UIConstants.DefaultFontSizeSmall, FontStyle.Bold);
            message.AutoSize = false;
            message.Width = 215;
            message.Height = 110;
            tableLayout.Controls.Add(message, 1, 0);

            if (botonImagen != null)
            {
                Panel closeButtonPanel = DialogHelper.CreateDialogButton(LocalizationManager.GetString("CloseButton"), () => info_window?.Close(), botonImagen, margin_top: UIConstants.DefaultMargin);
                tableLayout.Controls.Add(closeButtonPanel, 1, 1);
            }

            info_window.ResumeLayout(false); // Reanudar el diseño de la ventana de información
            info_window.Show(this);
        }

        

        /// <summary>
        /// Valida la entrada de un TextBox para permitir solo caracteres numéricos y la tecla de retroceso.
        /// </summary>
        /// <param name="sender">El objeto que generó el evento.</param>
        /// <param name="e">Argumentos del evento KeyPress.</param>
        private void ValidateNumericInput(object? sender, KeyPressEventArgs e)
        {
            // Permitir solo dígitos y la tecla de retroceso
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

            // Si se presiona un dígito y _shouldClearInput es true, limpiar el TextBox
            if (char.IsDigit(e.KeyChar) && _shouldClearInput)
            {
                entryCustomTime.Text = string.Empty;
                _shouldClearInput = false;
            }
        }

        /// <summary>
        /// Valida la entrada del usuario para el tiempo de apagado personalizado.
        /// Muestra un mensaje de error si la entrada no es válida.
        /// </summary>
        /// <returns>True si la entrada es válida; de lo contrario, False.</returns>
        private bool ValidateCustomTimeInput()
        {
            string minutos_str = entryCustomTime.Text;
            bool isValid = true;
            string errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(minutos_str))
            {
                errorMessage = LocalizationManager.GetString("ArgumentError");
                isValid = false;
            }
            else if (!long.TryParse(minutos_str, out long minutos_long))
            {
                errorMessage = LocalizationManager.GetString("InvalidNumberError");
                isValid = false;
            }
            else if (minutos_long <= 0)
            {
                errorMessage = LocalizationManager.GetString("PositiveNumberError");
                isValid = false;
            }
            else if (minutos_long > int.MaxValue)
            {
                errorMessage = LocalizationManager.GetString("NumberTooLargeError");
                isValid = false;
            }

            labelErrorCustomTime.Text = errorMessage;
            labelErrorCustomTime.Visible = !isValid;

            if (!isValid)
            {
                // Recalcular la posición para centrar horizontalmente después de que el texto se ha establecido
                labelErrorCustomTime.Location = new Point(
                    (this.ClientSize.Width - labelErrorCustomTime.Width) / 2,
                    entryPanel.Bottom + 9
                );
                entryCustomTime.Focus();
            }

            return isValid;
        }

    }
}