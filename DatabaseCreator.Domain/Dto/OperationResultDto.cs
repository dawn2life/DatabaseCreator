using System.Collections.Generic;

namespace DatabaseCreator.Domain.Dto
{
    public class OperationResultDto
    {
        public List<string> CreatedDatabaseNames { get; set; } = new List<string>();
        public string SummaryMessage { get; set; } = string.Empty;
        public bool Success { get; set; } // Overall success, can be based on if any DB was created.
    }
}
