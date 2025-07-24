using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using ShutdownApp; // Para poder usar el Logger
using System.Configuration;

namespace ShutdownApp
{
    /// <summary>
    /// Gestiona la carga y recuperación de cadenas de texto localizadas para la aplicación.
    /// </summary>
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _translations = new Dictionary<string, string>();
        private static readonly string LangDirectory = GetLangDirectory();

        /// <summary>
        /// Obtiene la ruta del directorio de archivos de idioma.
        /// </summary>
        /// <returns>La ruta absoluta al directorio de idiomas.</returns>
        private static string GetLangDirectory()
        {
            string? appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (appDirectory == null)
            {
                appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            string langFolderName = ConfigurationManager.AppSettings["LangFolderPath"] ?? "lang";
            return Path.Combine(appDirectory, langFolderName);
        }
        private const string DefaultLanguage = "en";

        /// <summary>
        /// Carga el idioma actual de la interfaz de usuario del sistema o el idioma por defecto si no está disponible.
        /// </summary>
        public static void LoadCurrentLanguage()
        {
            string currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (!LoadLanguage(currentCulture))
            {
                // Si falla el idioma del sistema, intenta cargar el idioma por defecto.
                // No es necesario notificar al usuario, ya que la aplicación funcionará en inglés.
                LoadLanguage(DefaultLanguage);
            }
        }

        /// <summary>
        /// Carga las cadenas de texto para un idioma específico desde un archivo JSON.
        /// </summary>
        /// <param name="language">El código de dos letras del idioma (ej. "en", "es").</param>
        /// <returns>True si el idioma se cargó correctamente; de lo contrario, False.</returns>
        private static bool LoadLanguage(string language)
        {
            string filePath = Path.Combine(LangDirectory, $"{language}.json");
            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"Archivo de idioma no encontrado: {filePath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                return _translations.Count > 0;
            }
            catch (JsonException ex)
            {
                // Error específico de JSON, el archivo está corrupto.
                Logger.LogError($"Error al deserializar el archivo de idioma '{filePath}': {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Otros errores (lectura de archivo, etc.)
                Logger.LogError($"Error inesperado al cargar el archivo de idioma '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la cadena de texto localizada para una clave dada.
        /// </summary>
        /// <param name="key">La clave de la cadena de texto.</param>
        /// <returns>La cadena de texto localizada, o la clave si no se encuentra la traducción.</returns>
        public static string GetString(string key)
        {
            return _translations.TryGetValue(key, out var value) ? value : key;
        }
    }
}