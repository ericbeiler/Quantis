using Microsoft.Extensions.Logging;
using System;
using Dapper;
using Visavi.Quantis.Data;
using System.Data.Common;

namespace Visavi.Quantis.Modeling
{
    public class ModelService
    {
        private readonly ILogger<DataService> _logger;
        private readonly Connections _connections;
        private const string selectTrainedModels = "SELECT * FROM EquityModels";

        public ModelService(ILogger<DataService> logger, Connections connections)
        {
            _logger = logger;
            _connections = connections;
        }

        internal async Task<IEnumerable<TrainedModel>> GetModelsAsync()
        {
            using var dbConnection = _connections.DbConnection;
            return await dbConnection.QueryAsync<TrainedModel>(selectTrainedModels);
        }

    }
}
