using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace Expedience
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseAddress = "";

		public ApiClient(string apiEndPointBaseAddress)
        {
            _httpClient = new HttpClient();
            _baseAddress = apiEndPointBaseAddress;
		}

        public async Task UploadDutyCompletionResults(List<string> resultsToUpload)
        {
            try
            {
                await _httpClient.PostAsync($"{_baseAddress}/api/DutyCompletion", JsonContent.Create(resultsToUpload));
            }
            catch (Exception ex)
            {
				Service.PluginLog.Error(ex, $"Error occurred trying to upload completion result: {ex.Message}");
            }
        }

        public async Task<string> GetUserName(int worldId, string userHash)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseAddress}/api/UserName/{worldId}/{userHash}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
					Service.PluginLog.Information("User not found. User names are only generated after the first upload.");
                }
                else
                {
					Service.PluginLog.Error("Error occurred trying to get user name: {responseCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
				Service.PluginLog.Error(ex, $"Error occurred trying to get user name.");
            }

            return null;
        }
    }
}
