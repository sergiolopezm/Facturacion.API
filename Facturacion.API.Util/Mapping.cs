using System.Reflection;

namespace Facturacion.API.Util
{
    public static class Mapping
    {
        /// <summary>
        /// Convierte un objeto de tipo origen a tipo destino mapeando propiedades con el mismo nombre
        /// </summary>
        public static TDestino Convertir<TOrigen, TDestino>(TOrigen origen)
        where TOrigen : class
        where TDestino : class, new()
        {
            if (origen == null)
            {
                throw new ArgumentNullException(nameof(origen));
            }

            var destino = new TDestino();

            CopiarPropiedades(origen, destino);

            return destino;
        }

        /// <summary>
        /// Convierte objetos de dos tipos origen a un tipo destino
        /// </summary>
        public static TDestino Convertir<TOrigen1, TOrigen2, TDestino>(TOrigen1 origen1, TOrigen2 origen2)
        where TOrigen1 : class
        where TOrigen2 : class
        where TDestino : class, new()
        {
            if (origen1 == null)
            {
                throw new ArgumentNullException(nameof(origen1));
            }

            if (origen2 == null)
            {
                throw new ArgumentNullException(nameof(origen2));
            }

            var destino = new TDestino();

            CopiarPropiedades(origen1, destino);
            CopiarPropiedades(origen2, destino);

            return destino;
        }

        /// <summary>
        /// Convierte objetos de tres tipos origen a un tipo destino
        /// </summary>
        public static TDestino Convertir<TOrigen1, TOrigen2, TOrigen3, TDestino>(TOrigen1 origen1, TOrigen2 origen2, TOrigen3 origen3)
        where TOrigen1 : class
        where TOrigen2 : class
        where TOrigen3 : class
        where TDestino : class, new()
        {
            if (origen1 == null)
            {
                throw new ArgumentNullException(nameof(origen1));
            }

            if (origen2 == null)
            {
                throw new ArgumentNullException(nameof(origen2));
            }

            if (origen3 == null)
            {
                throw new ArgumentNullException(nameof(origen3));
            }

            var destino = new TDestino();

            CopiarPropiedades(origen1, destino);
            CopiarPropiedades(origen2, destino);
            CopiarPropiedades(origen3, destino);

            return destino;
        }

        /// <summary>
        /// Copia propiedades con el mismo nombre de un objeto origen a uno destino
        /// </summary>
        private static void CopiarPropiedades<TOrigen, TDestino>(TOrigen origen, TDestino destino)
        {
            var propiedadesOrigen = typeof(TOrigen).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propiedadesDestino = typeof(TDestino).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                   .ToDictionary(p => p.Name);

            foreach (var propiedadOrigen in propiedadesOrigen)
            {
                if (propiedadesDestino.TryGetValue(propiedadOrigen.Name, out PropertyInfo? propiedadDestino))
                {
                    if (propiedadDestino.CanWrite && propiedadOrigen.CanRead)
                    {
                        var valorOrigen = propiedadOrigen.GetValue(origen);
                        if (valorOrigen != null)
                        {
                            // Si los tipos son compatibles directamente
                            if (propiedadDestino.PropertyType.IsAssignableFrom(propiedadOrigen.PropertyType))
                            {
                                propiedadDestino.SetValue(destino, valorOrigen);
                            }
                            // Si el tipo destino es nullable y el origen no, o viceversa
                            else if (TryConvertValue(valorOrigen, propiedadOrigen.PropertyType, propiedadDestino.PropertyType, out object? valorConvertido))
                            {
                                propiedadDestino.SetValue(destino, valorConvertido);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Intenta convertir un valor al tipo especificado
        /// </summary>
        private static bool TryConvertValue(object valor, Type tipoOrigen, Type tipoDestino, out object? resultado)
        {
            resultado = null;
            try
            {
                // Si el tipo destino es nullable, obtenemos el tipo subyacente
                var tipoDestinoReal = Nullable.GetUnderlyingType(tipoDestino) ?? tipoDestino;

                // Si son el mismo tipo, no necesitamos convertir
                if (tipoOrigen == tipoDestinoReal)
                {
                    resultado = valor;
                    return true;
                }

                // Si el tipo destino es enum y el origen es string o numérico
                if (tipoDestinoReal.IsEnum)
                {
                    if (tipoOrigen == typeof(string))
                    {
                        resultado = Enum.Parse(tipoDestinoReal, (string)valor);
                        return true;
                    }
                    if (valor is IConvertible)
                    {
                        resultado = Enum.ToObject(tipoDestinoReal, valor);
                        return true;
                    }
                }

                // Intentar conversión mediante Convert
                if (valor is IConvertible && tipoDestinoReal.IsValueType)
                {
                    resultado = Convert.ChangeType(valor, tipoDestinoReal);
                    return true;
                }

                // Si el tipo destino es string, usar ToString()
                if (tipoDestinoReal == typeof(string))
                {
                    resultado = valor.ToString();
                    return true;
                }

                // Si el tipo destino tiene un constructor que acepta el tipo origen
                var constructor = tipoDestinoReal.GetConstructor(new[] { tipoOrigen });
                if (constructor != null)
                {
                    resultado = constructor.Invoke(new[] { valor });
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convierte una lista de objetos de tipo origen a tipo destino
        /// </summary>
        public static List<TDestino> ConvertirLista<TOrigen, TDestino>(IEnumerable<TOrigen> listaOrigen)
        where TOrigen : class
        where TDestino : class, new()
        {
            if (listaOrigen == null)
            {
                return new List<TDestino>();
            }

            return listaOrigen.Select(o => Convertir<TOrigen, TDestino>(o)).ToList();
        }
    }
}
