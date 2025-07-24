using System;
using System.Diagnostics;

namespace ShutdownApp
{
    /// <summary>
    /// Gestiona la lógica para programar y cancelar el apagado del sistema.
    /// </summary>
    public class ShutdownManager
    {
        /// <summary>
        /// Obtiene un valor que indica si hay un apagado del sistema programado actualmente.
        /// </summary>
        public bool IsShutdownScheduled { get; private set; }

        /// <summary>
        /// Programa el apagado del sistema después de un número específico de minutos.
        /// </summary>
        /// <param name="minutes">El número de minutos para programar el apagado.</param>
        /// <returns>True si el apagado se programó correctamente; de lo contrario, False.</returns>
        /// <exception cref="ArgumentException">Se lanza si el tiempo especificado es menor o igual a cero.</exception>
        public async Task<bool> ScheduleShutdown(int minutes)
        {
            if (minutes <= 0)
            {
                throw new ArgumentException("El tiempo debe ser un número positivo.", nameof(minutes));
            }

            try
            {
                await Task.Run(() =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo("shutdown", $"-s -t {minutes * 60}");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                });
                IsShutdownScheduled = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error al programar el apagado.");
                return false;
            }
        }

        /// <summary>
        /// Cancela cualquier apagado del sistema programado previamente.
        /// </summary>
        /// <returns>True si el apagado se canceló correctamente o si no había ninguno programado; de lo contrario, False.</returns>
        public async Task<bool> CancelShutdown()
        {
            if (!IsShutdownScheduled)
            {
                return true; // O podrías lanzar una excepción si prefieres.
            }

            try
            {
                await Task.Run(() =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo("shutdown", "-a");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                });
                IsShutdownScheduled = false;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error al cancelar el apagado.");
                return false;
            }
        }
    }
}
