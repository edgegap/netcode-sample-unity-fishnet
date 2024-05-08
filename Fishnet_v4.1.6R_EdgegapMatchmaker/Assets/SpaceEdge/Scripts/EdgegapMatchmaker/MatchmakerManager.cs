using FishNet;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MatchmakerManager : ScriptableObject
{
    public const string MATCHMAKER_URL = "MATCHMAKER_URL";
    private readonly HttpClient _httpClient = new();
    public Action<string, bool> OnStatusUpdate;

    /// <summary>
    /// Create a new ticket for the Matchmaker
    /// </summary>
    /// <param name="modeTag">What game mode the players wants to be in</param>
    /// <returns>Ticket data</returns>
    public async Task<TicketData> CreateTicket(int scoreValue, string modeTag)
    {
        OnStatusUpdate?.Invoke($"Creating New Matchmaker Ticket With {modeTag} mode...", false);

        var dataObject = new
        {
            edgegap_profile_id = "GameExample",
            matchmaking_data = new
            {
                selector_data = new
                {
                    mode = modeTag,
                },
                filter_data = new
                {
                    score = scoreValue,
                },
            }

        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(dataObject), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{MATCHMAKER_URL}/v1/tickets", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error code: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        TicketData content = JsonConvert.DeserializeObject<Response<TicketData>>(responseContent).data;

        return content;
    }

    /// <summary>
    /// Get a ticket's data from the Matchmaker
    /// </summary>
    /// <param name="ticketId">Ticket's ID</param>
    /// <returns>Ticket data</returns>
    public async Task<TicketData> GetTicket(string ticketId)
    {
        OnStatusUpdate?.Invoke($"Getting Information On Matchmaker Ticket ID #{ticketId}...", false);
        var response = await _httpClient.GetAsync($"{MATCHMAKER_URL}/v1/tickets/{ticketId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error code: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        TicketData content = JsonConvert.DeserializeObject<Response<TicketData>>(responseContent).data;

        return content;
    }

    /// <summary>
    /// Delete a ticket from the Matchmaker
    /// </summary>
    /// <param name="ticketId">Ticket's ID</param>
    public async Task DeleteTicket(string ticketId)
    {
        OnStatusUpdate?.Invoke($"Deleting Matchmaker Ticket ID #{ticketId}...", false);
        var response = await _httpClient.DeleteAsync($"{MATCHMAKER_URL}/v1/tickets/{ticketId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error code: {response.StatusCode}");
        }
    }

    #region Data
    public struct TicketData
    {
        [JsonProperty("ticket_id")]
        public string Id { get; set; }

        [JsonProperty("assignment")]
        public ServerAddress? Connection { get; set; }
    }

    public struct ServerAddress
    {
        [JsonProperty("server_host")]
        public string Address { get; set; }
    }

    public struct Response<T>
    {
        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("data")]
        public T data { get; set; }
    }
    #endregion
}
