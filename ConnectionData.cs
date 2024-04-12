using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Q4ExportImport.Common.Class;
using Q4ExportImport.Data.Contracts;
using Q4ExportImport.Models.Export;

namespace Q4ExportImport.Data.Export
{
    public class ConnectionData : IConnectionData
    {
        private readonly IConfiguration _configuration;
        private string _keyVaultUrl;
        public ConnectionData(IConfiguration configuration)
        {
            _configuration = configuration;
            _keyVaultUrl = Configurations.keyVaultUrl;
        }

        /// <summary>
        /// Get Connectionstring from Config file and Key/Vault.
        /// </summary>
        /// <param name="exportOfficeRequest">object for find connection string by office number/code</param>
        /// <returns></returns>
        public async Task<string> GetConnectionString(ExportOfficeRequest exportOfficeRequest)
        {
            //Azure key vault value get
            var client = new SecretClient(new Uri(_keyVaultUrl), new DefaultAzureCredential());
            //try
            //{
            KeyVaultSecret secret = client.GetSecret(Environment.GetEnvironmentVariable("secretName"));
            //}
            //catch (RequestFailedException ex)
            //{
            //    await _cosmosDbLogging.Log(ExceptionType.SystemException, LogType.Export, LogLevel.Exception, "Connection string not found for " + obj.OfficeCode + " office." + ex.ToString(), obj.TriggerGroupId, obj.OfficeCode, e);
            //}
            string connectionstring = Configurations.SQLConnectionString;
            string dbServerName = ConvertTo.Base64Decode(Environment.GetEnvironmentVariable(exportOfficeRequest.OfficeCode + "_dbServerName"));
            string HQDatabaseName = Environment.GetEnvironmentVariable(exportOfficeRequest.OfficeCode + "_HQDatabaseName");
            string userName = Environment.GetEnvironmentVariable(exportOfficeRequest.OfficeCode + "_userName");
            return connectionstring.Replace("<HQDatabaseName>", HQDatabaseName).Replace("<userName>", userName).Replace("<dbPassword>", secret.Value).Replace("<dbServerName>", dbServerName);
        }

    }
}
