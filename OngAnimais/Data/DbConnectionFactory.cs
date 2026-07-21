using System.Data;
using Npgsql;

namespace OngAnimais.Data
{
    /// <summary>
    /// Fábrica de conexões com o banco de dados PostgreSQL.
    /// Responsável por criar e fornecer instâncias de IDbConnection.
    /// </summary>
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Inicializa a fábrica com a connection string configurada.
        /// </summary>
        /// <param name="connectionString">String de conexão com o PostgreSQL.</param>
        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Cria e retorna uma nova conexão, já aberta, com o banco de dados.
        /// </summary>
        /// <returns>Uma instância aberta de <see cref="IDbConnection"/>.</returns>
        /// <remarks>
        /// A conexão é aberta aqui (não deixada para o Dapper abrir sob demanda) porque,
        /// quando o Dapper abre a conexão sozinho para uma query, ele também a fecha ao
        /// final dessa mesma chamada. Módulos que fazem uma consulta e, na sequência,
        /// abrem uma transação (<c>connection.BeginTransaction()</c>) quebrariam com
        /// "Connection is not open" se a conexão chegasse fechada.
        /// </remarks>
        public IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
