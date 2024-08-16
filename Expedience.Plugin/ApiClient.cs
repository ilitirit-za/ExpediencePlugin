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
		private readonly IPluginLog _pluginLog;

		public ApiClient(string apiEndPointBaseAddress, IPluginLog pluginLog) 
        {
            _httpClient = new HttpClient();
            _baseAddress = apiEndPointBaseAddress;
			_pluginLog = pluginLog;
		}

        public async Task UploadDutyCompletionResults(List<string> resultsToUpload)
        {
            try
            {
                await _httpClient.PostAsync($"{_baseAddress}/api/DutyCompletion", JsonContent.Create(resultsToUpload));
            }
            catch (Exception ex)
            {
				_pluginLog.Error(ex, $"Error occurred trying to upload completion result: {ex.Message}");
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
					_pluginLog.Information("User not found. User names are only generated after the first upload.");
                }
                else
                {
					_pluginLog.Error("Error occurred trying to get user name: {responseCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
				_pluginLog.Error(ex, $"Error occurred trying to get user name.");
            }

            return null;
        }
    }
}
