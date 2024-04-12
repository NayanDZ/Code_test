using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Q4ExportImport.Common.Class;
using Q4ExportImport.Data.Contracts;
using Q4ExportImport.Models.Export;
using System.Drawing;

namespace Q4ExportImport.Data.Export
{
    public class TimeCardValidationData : ITimeCardValidationData
    {
        private readonly ICosmosDbLoggingData _cosmosDbLogging;
        private readonly IConfiguration _configuration;
        private readonly ICosmosDbTransactionData _cosmosDbTransaction;
        private readonly IConnectionData _connectionData;
        public TimeCardValidationData(ICosmosDbLoggingData cosmosDbLogging, IConfiguration configuration, ICosmosDbTransactionData cosmosDbTransaction, IConnectionData connectionData)
        {
            _cosmosDbLogging = cosmosDbLogging;
            _configuration = configuration;
            _cosmosDbTransaction = cosmosDbTransaction;
            _connectionData = connectionData;
        }
        public string _sqlFewValidationExecute = Configurations.SqlFewValidationExecute;

        /// <summary>
        /// Get record from PF1105 for Associate validation
        /// </summary>
        /// <param name="exportOfficeRequest"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PF1105>> GetProcessedOffice(ExportOfficeRequest exportOfficeRequest)
        {
            var connectionString = await _connectionData.GetConnectionString(exportOfficeRequest);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT " + _sqlFewValidationExecute + @" * FROM PF1105 WHERE iOfficeNum = " + exportOfficeRequest.OfficeNumber.ToString() + @" and sOfficeDept = '' 
                        and iDateProcessWE = '20240212'
                        and (iDateTransmitted is NULL OR iDateTransmitted = '')";
                var result = connection.Query<PF1105>(query);
                return result;
            }
        }

        /// <summary>
        /// Get record from XF1344 for Associate validation
        /// </summary>
        /// <param name="exportOfficeRequest"></param>
        /// <returns></returns>
        public async Task<IEnumerable<XF1344>> GetXF1344ByOfficeNumber(ExportOfficeRequest exportOfficeRequest)
        {
            var connectionString = await _connectionData.GetConnectionString(exportOfficeRequest);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT  " + _sqlFewValidationExecute + @"  sAddressLine1, sStateCode, iCityCode FROM XF1344 
                        WHERE iOfficeNum = " + exportOfficeRequest.OfficeNumber.ToString() + @"
                         AND sOfficeDept = '' 
                         --AND iIndCtlNum = @iIndCtlNum";
                var result = connection.Query<XF1344>(query);
                return result;
            }
        }

        /// <summary>
        /// Get time card data from PF1103, PF1123, PF1105 for Associate validation.
        /// </summary>
        /// <param name="exportOfficeRequest"></param>
        /// <param name="countryId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ExportTimeCard>> GetExportTimeCardData(ExportOfficeRequest exportOfficeRequest, int countryId)
        {
            var connectionString = await _connectionData.GetConnectionString(exportOfficeRequest);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT DISTINCT  " + _sqlFewValidationExecute + @"  amf.sEmployeeID,amf.sFirstName,amf.sLastName,";
                if (countryId == Convert.ToInt16(Configurations.Australia) ||
                    countryId == Convert.ToInt16(Configurations.Canada))
                {
                    query += @"amf.iPayFreq,amf.iIndCtlNum,aamf.cI9onFile ";
                }
                else
                {
                    query += @"amf.iDateW4,amf.iPayFreq,amf.iIndCtlNum,amf.iTaxStatus,amf.iExemptNum ";
                }
                query += @" FROM PF1103 amf 
                            INNER JOIN PF1123 aamf ON aamf.iOfficeNum = amf.iOfficeNum 
	                            AND aamf.sOfficeDept = amf.sOfficeDept 
	                            AND aamf.iIndCtlNum = amf.iIndCtlNum 
	                            AND aamf.iDivision <> 4
                            INNER JOIN PF1105 tc ON amf.iOfficeNum = tc.iOfficeNum AND 
	                            amf.sOfficeDept = tc.sOfficeDept AND 
	                            amf.iIndCtlNum = tc.iIndCtlNum AND  
	                            tc.iTransactCode NOT IN (446,447,448,449) AND 
	                            (tc.iDateTransmitted is NULL OR tc.iDateTransmitted = '') AND 
	                            tc.iOfficeNum = " + exportOfficeRequest.OfficeNumber.ToString() + @" AND 
	                            tc.sOfficeDept = ''  
                            Where tc.iDateProcessWE  = '" + exportOfficeRequest.FormattedDateProcessWE + @"'";
                var result = connection.Query<ExportTimeCard>(query);
                return result;
            }
        }

    }
}
