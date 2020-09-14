using System;
using System.Data;
using System.Data.SqlClient;

//http://stackoverflow.com/questions/23023534/managing-connection-with-non-buffered-queries-in-dapper
namespace MML.Enterprise.Persistence.Dapper
{
    public class Repository
    {
        private readonly string _connectionString;

        public Repository(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected T GetConnection<T>(Func<IDbConnection, T> getData) where T : class
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return getData(connection);
            }
        }

        protected TResult GetConnection<TRead, TResult>(Func<IDbConnection, TRead> getData, Func<TRead, TResult> process)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var data = getData(connection);
                return process(data);
            }
        }


        protected T GetTransConnection<T>(Func<IDbConnection, T> getData) where T : class
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    return getData(connection);
                    
                }

            }
        }



    }
}
