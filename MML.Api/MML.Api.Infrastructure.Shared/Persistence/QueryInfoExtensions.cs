using Dapper;
using log4net;
using System.Linq;

namespace MML.Enterprise.Persistence
{
    public static class QueryInfoExtensions
    {
        public static void LogError(this QueryInfo query, ILog log, string introMessage = null)
        {
            if (!log.IsErrorEnabled)
                return;

            if (!string.IsNullOrEmpty(introMessage))
                log.Error(introMessage);

            log.Error(query.Query);
            if (query.Parameters != null)
            {
                log.Error("Params:");
                foreach (var param in query.Parameters.ParameterNames)
                {
                    log.ErrorFormat("@{0}: {1}", param, TryGetParamVal(param, query.Parameters));
                }
            }
        }

        public static void LogInfo(this QueryInfo query, ILog log, string introMessage = null)
        {
            if (!log.IsInfoEnabled)
                return;

            if (!string.IsNullOrEmpty(introMessage))
                log.Info(introMessage);

            log.Info(query.Query);
            if (query.Parameters != null)
            {
                log.Info("Params:");
                foreach (var param in query.Parameters.ParameterNames)
                {
                    log.InfoFormat("@{0}: {1}", param, TryGetParamVal(param, query.Parameters));
                }
            }
        }

        public static void LogDebug(this QueryInfo query, ILog log, string introMessage = null)
        {
            if (!log.IsDebugEnabled)
                return;

            if (!string.IsNullOrEmpty(introMessage))
                log.Debug(introMessage);

            log.Debug(query.Query);
            if (query.Parameters != null)
            {
                log.Debug("Params:");
                foreach (var param in query.Parameters.ParameterNames)
                {
                    log.DebugFormat("@{0}: {1}", param, TryGetParamVal(param, query.Parameters));
                }
            }
        }

        private static string TryGetParamVal(string paramName, DynamicParameters parameters)
        {
            string value = null;
            string[] valueArray = null;
            try
            {
                value = parameters.Get<string>(paramName);
            }
            catch
            {
                try
                {
                    valueArray = parameters.Get<dynamic>(paramName) as string[];
                }
                catch
                {
                    value = "collections and complex objects cannot be interpreted here.";
                }
                
            }
            if(valueArray != null)
            {
                value = valueArray.Aggregate("", (current, nextCol) => current + " " + nextCol + ",");
                value = value.Remove(value.Length - 1);
            }

            return value;
        }
    }
}
