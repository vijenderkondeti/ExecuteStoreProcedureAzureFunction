using System.Linq;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace AzureFunctionExecuteStoreProcedure
{
    public class account
    {

        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Opsr { get; set; }
    }
  
    public static class Function1
    {
        public static async Task<string> GetToken(string authority, string resource, string scope)
        {

            string CLIENT_ID = Environment.GetEnvironmentVariable("CLIENT_ID");
            string CLIENT_SECRET = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(CLIENT_ID, CLIENT_SECRET);
            AuthenticationResult authResult = await authContext.AcquireTokenAsync(resource, clientCred);

            if (authResult == null)
                throw new InvalidOperationException("Failed to Obtain the JWT token");

            return authResult.AccessToken;
        }
        [FunctionName("Function1")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            try
            {
                string SecretUri = Environment.GetEnvironmentVariable("SecretUri");
                //string SecretUri = "https://ccbcckeyvault12.vault.azure.net/secrets/sqlserverazurekeyvault/b0b4c5ad2b984ed99cfa02e6f7490b0a";
                var kvToken = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
                var kvSecret = kvToken.GetSecretAsync(SecretUri).Result;
                var con = new SqlConnection(kvSecret.Value);
                SqlCommand cmd = new SqlCommand("dbo.proc_plan", con);
                await con.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader rdr = cmd.ExecuteReader();
                List<account> accounts = new List<account>();

                while (rdr.Read())
                {


                    accounts.Add(new account
                    {
                        AccountName = rdr.GetString(rdr.GetOrdinal("AccountName")),
                        AccountNumber = rdr.GetString(rdr.GetOrdinal("AccountNumber")),
                        ExpiryDate = DateTime.Today.ToString("dd-MM-yyyy"),
                        Opsr = rdr.GetString(rdr.GetOrdinal("Opsr")),
                    });

                    //
                }
                con.Close();
                return req.CreateResponse(HttpStatusCode.OK, accounts);
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, ex);

            }
        }
    }
}
