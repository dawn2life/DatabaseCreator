using AutoMapper;
using DatabaseCreator.Domain.Dto;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Repositories;
using DatabaseCreator.Domain.Services;
using Microsoft.IdentityModel.Protocols.WSIdentity;
using System.Xml.Linq;

namespace DatabaseCreator.Service.CommonService
{
    public  class DatabaseOperationService : IDatabaseOperationService
    {
        private readonly IDatabsaeOperationRepository _databsaeOperationRepository;
        private readonly IMapper _mapper;

        public DatabaseOperationService(IDatabsaeOperationRepository databsaeOperationRepository, IMapper mapper)
        {
            _databsaeOperationRepository = databsaeOperationRepository;
            _mapper = mapper;
        }

        public List<string>? SingleExecution(List<string>? databaseNames)
        {
            ValidateInputParameters(databaseNames);
            List<DbInfodto> result = new List<DbInfodto>();

            foreach (string dbName in databaseNames)
            {
                var dbCreated = _databsaeOperationRepository.CreateDbWithSingleExecution(dbName);

                if (dbCreated) 
                {
                    result.Add(new DbInfodto { DbName = dbName, IsCreated = true });
                    continue;
                }

                result.Add(new DbInfodto { DbName = dbName, IsCreated = false });
            }

            var mappedDbInfo = _mapper.Map<List<DbInfo>>(result);
            
            _databsaeOperationRepository.AddCreatedDb(mappedDbInfo);

            DisplayResult(result);

            return result.Where(x => x.IsCreated == true).Select(x => x.DbName).ToList();
        }

        public List<string>? Batch(List<string>? databaseNames)
        {
            ValidateInputParameters(databaseNames);
            List<DbInfodto> result = new List<DbInfodto>();

            var isTransactionSuccess = _databsaeOperationRepository.CreateDbWithBatch(databaseNames);

            if (isTransactionSuccess) 
            {
                foreach (var dbname in databaseNames) 
                {
                    result.Add(new DbInfodto { DbName = dbname , IsCreated = true });
                }

                var mappedDbInfo = _mapper.Map<List<DbInfo>>(result);
                _databsaeOperationRepository.AddCreatedDb(mappedDbInfo);
            }

            DisplayResult(result);

            return result.Where(x => x.IsCreated == true).Select(x => x.DbName).ToList();
        }

        #region Private methods

        /// <summary>
        /// This method validates the input parameters for creating multiple databases.
        /// It throws an exception if any of these conditions are not met.
        /// </summary>
        /// <param name="dbNames">The list of database names to be created.</param>
        /// <exception cref="InvalidInputException">Thrown when the connection is not open or the list of database names is empty.</exception>
        private void ValidateInputParameters(List<string>? dbNames)
        {
            if (dbNames?.Count == 0)
            {
                throw new InvalidInputException($"The {nameof(dbNames)} is empty.");
            }
        }

        /// <summary>
        /// This method displays the result of creating multiple databases to the console.
        /// It displays a summary message showing how many databases were created and how many failed.
        /// </summary>
        /// <param name="createdDbNames">The list of database names that were created successfully.</param>
        /// <param name="failedDbNames">The list of database names that failed to be created.</param>
        private void DisplayResult(List<DbInfodto> dbs)
        {
            int createdDbCount = dbs.Where(x => x.IsCreated == true).Count();
            int failedDbCount = dbs.Where(x => x.IsCreated == false).Count();
            Console.WriteLine($"\nSummary: {createdDbCount} out of {dbs.Count} databases were created successfully. {failedDbCount} databases failed to be created.");
        }

        #endregion
    }
}

