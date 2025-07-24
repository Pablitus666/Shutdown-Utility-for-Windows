using System;
using System.Windows.Forms;
using System.Threading;

namespace ShutdownApp
{
    /// <summary>
    /// Clase principal de la aplicación. Contiene el punto de entrada y el manejo global de excepciones.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// Configura el manejo de excepciones global y ejecuta el formulario principal.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Cargar el idioma actual al inicio de la aplicación
#if DEBUG_LANG_TEST
            // Cargar el idioma actual al inicio de la aplicación
            LocalizationManager.LoadCurrentLanguage();
#else
            LocalizationManager.LoadCurrentLanguage();
#endif

            // Manejar excepciones en el hilo de la UI
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            // Manejar excepciones en otros hilos (no-UI)
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// Maneja las excepciones no controladas que ocurren en el hilo de la interfaz de usuario.
        /// </summary>
        /// <param name="sender">El objeto que generó el evento.</param>
        /// <param name="e">Argumentos del evento de excepción de hilo.</param>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, "UI Thread Exception");
        }

        /// <summary>
        /// Maneja las excepciones no controladas que ocurren en cualquier hilo de la aplicación (excepto el hilo de la UI).
        /// </summary>
        /// <param name="sender">El objeto que generó el evento.</param>
        /// <param name="e">Argumentos del evento de excepción no controlada.</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            HandleException(ex, "Unhandled Exception");
        }

        /// <summary>
        /// Método auxiliar para loguear y notificar al usuario sobre una excepción crítica.
        /// </summary>
        /// <param name="ex">La excepción a manejar.</param>
        /// <param name="source">La fuente de la excepción (ej. "UI Thread Exception", "Unhandled Exception").</param>
        private static void HandleException(Exception ex, string source)
        {
            // Loguear la excepción
            Logger.LogException(ex, string.Format(LocalizationManager.GetString("LogCriticalErrorSource"), source));

            // Mostrar un mensaje amigable al usuario
            MessageBox.Show(
                LocalizationManager.GetString("CriticalErrorMessage"),
                LocalizationManager.GetString("CriticalErrorTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            // Opcional: Terminar la aplicación si el error es crítico
            Application.Exit();
        }
    }
}
