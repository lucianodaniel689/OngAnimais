using System.Data;
using Dapper;

namespace OngAnimais.Data
{
    /// <summary>
    /// Dapper ainda não sabe mapear <see cref="DateOnly"/> para um parâmetro de comando
    /// (só o retorno de leitura funciona nativamente com o driver do PostgreSQL). Esses
    /// handlers cobrem os dois lados para colunas <c>DATE</c> (ex: <c>vacinas.data_aplicacao</c>).
    /// Registrados em <c>DatabaseConfig.AddDatabaseConfiguration</c>.
    /// </summary>
    public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value;
        }

        /// <inheritdoc />
        public override DateOnly Parse(object value)
        {
            return value is DateOnly dataOnly ? dataOnly : DateOnly.FromDateTime((DateTime)value);
        }
    }

    /// <summary>
    /// Variante para <see cref="Nullable{DateOnly}"/> (ex: <c>vacinas.proxima_dose</c>, opcional).
    /// </summary>
    public class DateOnlyNulavelTypeHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        /// <inheritdoc />
        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            parameter.Value = (object?)value ?? DBNull.Value;
        }

        /// <inheritdoc />
        public override DateOnly? Parse(object value)
        {
            if (value is DBNull)
                return null;

            return value is DateOnly dataOnly ? dataOnly : DateOnly.FromDateTime((DateTime)value);
        }
    }
}
