using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using ProtoBuf.Data;

namespace DataReaderTesting
{
    public class DatabaseReader
    {
        private string connectionString;

        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        public DatabaseReader(string connectionString)
        { this.connectionString = connectionString; }

        public void ReadData(string tableName, string commaSeparatedColumns = "CODE, STORAGEMODE")
        {
            string queryString = $"SELECT {commaSeparatedColumns} FROM {tableName}";
            using OracleConnection oracleConnection = new OracleConnection(connectionString);
            oracleConnection.Open();

            if (!tableExistsAsync(tableName, oracleConnection).Result)
                Console.WriteLine(tableName + " does not exist");

            using IDataReader dataReader = new OracleCommand(queryString, oracleConnection).ExecuteReader();

            while (dataReader.Read())
            {
                object[] values = new object[dataReader.FieldCount];
                dataReader.GetValues(values);
                Console.WriteLine(string.Join(", ", values));
            }
        }

        public void ReadDataProtobuf(string tableName, string commaSeparatedColumns = "CODE, STORAGEMODE")
        {
            string queryString = $"SELECT {commaSeparatedColumns} FROM {tableName}";
            using OracleConnection oracleConnection = new OracleConnection(connectionString);
            oracleConnection.Open();

            if (!tableExistsAsync(tableName, oracleConnection).Result)
                Console.WriteLine(tableName + " does not exist");

            using Stream buffer = new MemoryStream();

            using (IDataReader dataReader = new OracleCommand(queryString, oracleConnection).ExecuteReader())
            {
                DataSerializer.Serialize(buffer, dataReader);
            }

            buffer.Seek(0, SeekOrigin.Begin);

            using (IDataReader dataReader = DataSerializer.Deserialize(buffer))
            {
                while (dataReader.Read())
                {
                    object[] values = new object[dataReader.FieldCount];
                    dataReader.GetValues(values);
                    Console.WriteLine(string.Join(", ", values));
                }
            }
        }

        public MemoryStream GetProtoBufferStream(string tableName, string commaSeparatedColumns = "CODE, STORAGEMODE")
        {
            string queryString = $"SELECT {commaSeparatedColumns} FROM {tableName}";
            using OracleConnection oracleConnection = new OracleConnection(connectionString);
            oracleConnection.Open();

            if (!tableExistsAsync(tableName, oracleConnection).Result)
                Console.WriteLine(tableName + " does not exist");

            MemoryStream buffer = new MemoryStream();

            IDataReader dataReader = new OracleCommand(queryString, oracleConnection).ExecuteReader();

            DataSerializer.Serialize(buffer, dataReader);

            return buffer;
        }

        public IDataReader GetDataReader(string tableName, string commaSeparatedColumns = "CODE, STORAGEMODE")
        {
            string queryString = $"SELECT {commaSeparatedColumns} FROM {tableName}";
            OracleConnection oracleConnection = new OracleConnection(connectionString);
            oracleConnection.Open();

            if (!tableExistsAsync(tableName, oracleConnection).Result)
                Console.WriteLine(tableName + " does not exist");

            IDataReader dataReader = new OracleCommand(queryString, oracleConnection).ExecuteReader();

            return dataReader;
        }

        protected Task<bool> tableExistsAsync(string tableName, IDbConnection connection, IDbTransaction? transaction = null)
        {
            var query = $"select table_name from user_tables where table_name = :tableName";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("tableName", tableName.ToUpper());
            if (transaction != null)
                return Task<bool>.Factory.StartNew(() => connection.Query<dynamic>(query, parameters, transaction).Any());

            return Task<bool>.Factory.StartNew(() => connection.Query<dynamic>(query, parameters).Any());
        }

        protected Task<bool> isTableEmptyAsync(string tableName, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $"select count(1) from {tableName} where rownum=1";

            return Task<bool>.Factory.StartNew(() => connection.Query<int>(query, transaction: transaction).First() == 0);
        }
    }
}
