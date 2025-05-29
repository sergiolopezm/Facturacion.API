using System.Globalization;
using System.Text.RegularExpressions;

namespace Facturacion.API.Util
{
    /// <summary>
    /// Helper para manejo de moneda colombiana (Peso Colombiano - COP)
    /// Maneja el formato: $1.000,23 (mil pesos con 23 centavos)
    /// Separador de miles: punto (.)
    /// Separador decimal: coma (,)
    /// </summary>
    public static class CurrencyHelper
    {
        private static readonly CultureInfo ColombianCulture;

        static CurrencyHelper()
        {
            // Configurar cultura personalizada para Colombia
            ColombianCulture = new CultureInfo("es-CO");
            ColombianCulture.NumberFormat.CurrencySymbol = "$";
            ColombianCulture.NumberFormat.CurrencyDecimalSeparator = ",";
            ColombianCulture.NumberFormat.CurrencyGroupSeparator = ".";
            ColombianCulture.NumberFormat.CurrencyDecimalDigits = 2;
            ColombianCulture.NumberFormat.CurrencyPositivePattern = 0; // $n
        }

        /// <summary>
        /// Convierte una cadena de texto en formato colombiano a decimal
        /// Ejemplos: "$1.000", "1.000,50", "1000", "1.000.000,25"
        /// </summary>
        public static decimal ParseCurrency(string currencyString)
        {
            if (string.IsNullOrWhiteSpace(currencyString))
                return 0;

            try
            {
                // Limpiar la cadena
                string cleanString = CleanCurrencyString(currencyString);

                // Si no tiene separador decimal, es un entero
                if (!cleanString.Contains(','))
                {
                    // Remover puntos (separadores de miles) y convertir
                    cleanString = cleanString.Replace(".", "");
                    return Convert.ToDecimal(cleanString);
                }

                // Separar parte entera y decimal
                string[] parts = cleanString.Split(',');
                if (parts.Length != 2)
                    throw new FormatException("Formato de moneda inválido");

                // Procesar parte entera (remover puntos de miles)
                string integerPart = parts[0].Replace(".", "");
                string decimalPart = parts[1];

                // Validar que la parte decimal no tenga más de 2 dígitos
                if (decimalPart.Length > 2)
                    throw new FormatException("La parte decimal no puede tener más de 2 dígitos");

                // Completar con ceros si es necesario
                decimalPart = decimalPart.PadRight(2, '0');

                // Construir el número final
                string finalString = $"{integerPart}.{decimalPart}";
                return Convert.ToDecimal(finalString);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Error al convertir '{currencyString}' a moneda: {ex.Message}");
            }
        }

        /// <summary>
        /// Formatea un decimal como moneda colombiana
        /// </summary>
        public static string FormatCurrency(decimal amount, bool includeSymbol = true)
        {
            string formatted = amount.ToString("N2", ColombianCulture);
            return includeSymbol ? $"${formatted}" : formatted;
        }

        /// <summary>
        /// Formatea un decimal como moneda colombiana sin decimales si son ceros
        /// </summary>
        public static string FormatCurrencyCompact(decimal amount, bool includeSymbol = true)
        {
            // Si no hay decimales, mostrar sin ellos
            if (amount % 1 == 0)
            {
                string formatted = amount.ToString("N0", ColombianCulture);
                return includeSymbol ? $"${formatted}" : formatted;
            }
            else
            {
                return FormatCurrency(amount, includeSymbol);
            }
        }

        /// <summary>
        /// Valida si una cadena tiene formato de moneda colombiana válido
        /// </summary>
        public static bool IsValidCurrencyFormat(string currencyString)
        {
            if (string.IsNullOrWhiteSpace(currencyString))
                return false;

            try
            {
                ParseCurrency(currencyString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convierte un string con formato de moneda a decimal de forma segura
        /// </summary>
        public static bool TryParseCurrency(string currencyString, out decimal result)
        {
            result = 0;
            try
            {
                result = ParseCurrency(currencyString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Limpia una cadena de moneda removiendo caracteres no deseados
        /// </summary>
        private static string CleanCurrencyString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "0";

            // Remover símbolo de moneda, espacios y caracteres especiales
            string cleaned = input.Trim()
                                  .Replace("$", "")
                                  .Replace(" ", "")
                                  .Replace("\t", "")
                                  .Trim();

            // Validar que solo contenga números, puntos y comas
            if (!Regex.IsMatch(cleaned, @"^[0-9.,]+$"))
                throw new FormatException("La cadena contiene caracteres no válidos para moneda");

            // Validar formato básico
            if (Regex.IsMatch(cleaned, @"[.,]{2,}"))
                throw new FormatException("Formato de moneda inválido: separadores consecutivos");

            return cleaned;
        }

        /// <summary>
        /// Formatea un valor para mostrar en input HTML
        /// </summary>
        public static string FormatForInput(decimal amount)
        {
            return FormatCurrency(amount, false);
        }

        /// <summary>
        /// Calcula el IVA sobre un monto base
        /// </summary>
        public static decimal CalcularIVA(decimal montoBase, decimal porcentajeIVA = 19m)
        {
            return Math.Round(montoBase * (porcentajeIVA / 100m), 2);
        }

        /// <summary>
        /// Calcula el descuento si aplica según las reglas de negocio
        /// </summary>
        public static decimal CalcularDescuento(decimal montoBase, decimal montoMinimo = 500000m, decimal porcentajeDescuento = 5m)
        {
            if (montoBase >= montoMinimo)
            {
                return Math.Round(montoBase * (porcentajeDescuento / 100m), 2);
            }
            return 0m;
        }

        /// <summary>
        /// Calcula todos los totales de una factura
        /// </summary>
        public static FacturaTotalesDto CalcularTotalesFactura(decimal subtotal, decimal porcentajeDescuento = 5m, decimal montoMinimoDescuento = 500000m, decimal porcentajeIVA = 19m)
        {
            var descuento = CalcularDescuento(subtotal, montoMinimoDescuento, porcentajeDescuento);
            var baseImpuestos = subtotal - descuento;
            var iva = CalcularIVA(baseImpuestos, porcentajeIVA);
            var total = baseImpuestos + iva;

            return new FacturaTotalesDto
            {
                Subtotal = subtotal,
                PorcentajeDescuento = descuento > 0 ? porcentajeDescuento : 0,
                ValorDescuento = descuento,
                BaseImpuestos = baseImpuestos,
                PorcentajeIVA = porcentajeIVA,
                ValorIVA = iva,
                Total = total
            };
        }

        /// <summary>
        /// Convierte pesos a centavos (para almacenamiento como entero)
        /// </summary>
        public static long ToSpanishCentavos(decimal pesos)
        {
            return (long)(pesos * 100);
        }

        /// <summary>
        /// Convierte centavos a pesos
        /// </summary>
        public static decimal FromSpanishCentavos(long centavos)
        {
            return centavos / 100m;
        }
    }

    /// <summary>
    /// DTO para los totales calculados de una factura
    /// </summary>
    public class FacturaTotalesDto
    {
        public decimal Subtotal { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal ValorDescuento { get; set; }
        public decimal BaseImpuestos { get; set; }
        public decimal PorcentajeIVA { get; set; }
        public decimal ValorIVA { get; set; }
        public decimal Total { get; set; }

        /// <summary>
        /// Formatea todos los valores como moneda colombiana
        /// </summary>
        public FacturaTotalesFormateadosDto FormatearParaMostrar(bool incluirSimbolo = true)
        {
            return new FacturaTotalesFormateadosDto
            {
                Subtotal = CurrencyHelper.FormatCurrency(Subtotal, incluirSimbolo),
                PorcentajeDescuento = $"{PorcentajeDescuento:0.##}%",
                ValorDescuento = CurrencyHelper.FormatCurrency(ValorDescuento, incluirSimbolo),
                BaseImpuestos = CurrencyHelper.FormatCurrency(BaseImpuestos, incluirSimbolo),
                PorcentajeIVA = $"{PorcentajeIVA:0.##}%",
                ValorIVA = CurrencyHelper.FormatCurrency(ValorIVA, incluirSimbolo),
                Total = CurrencyHelper.FormatCurrency(Total, incluirSimbolo)
            };
        }
    }

    /// <summary>
    /// DTO con los totales formateados como texto
    /// </summary>
    public class FacturaTotalesFormateadosDto
    {
        public string Subtotal { get; set; } = string.Empty;
        public string PorcentajeDescuento { get; set; } = string.Empty;
        public string ValorDescuento { get; set; } = string.Empty;
        public string BaseImpuestos { get; set; } = string.Empty;
        public string PorcentajeIVA { get; set; } = string.Empty;
        public string ValorIVA { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;
    }
}
