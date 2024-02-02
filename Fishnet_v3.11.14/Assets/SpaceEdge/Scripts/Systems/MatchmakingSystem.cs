using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FishNet;
using Leguar.TotalJSON;
using UnityEngine;


namespace SpaceEdge
{
    /// <summary>
    ///     This class is the core of this sample and the only class that communicates with EdgeGap API.
    ///     While being a pretty straight forward matchmaker, it's a solid foundation that can be easily
    ///     extended to create a more complex matchmaking system that can handle all scenarios and matchmaking requirements.
    /// </summary>
    public class MatchmakingSystem : MonoBehaviour
    {
        /// <summary>
        ///     Name of the EdgeGap app we are trying to connect to
        ///     Please refer to the "EdgeGap FishNet Sample Guide" for detailed instruction on how to configure app name.
        /// </summary>
        [SerializeField] private string AppName = "fishnet-v3";

        /// <summary>
        ///     Version of the EdgeGap app we are trying to connect to
        ///     Please refer to the "EdgeGap FishNet Sample Guide" for detailed instruction on how to configure app version.
        /// </summary>
        [SerializeField] private string AppVersion = "3.0.0";

        /// <summary>
        ///     Used by the HTTP request's Authorization header.
        ///     Please refer to the "EdgeGap FishNet Sample Guide" for detailed instruction on how to acquire this value.
        /// </summary>
        [SerializeField] private string AuthHeaderValue = "bfcea7e4-892a-4f68-a2ca-22682aeb47ea";

        /// <summary>
        ///     Used by the HTTP request's Content-Type header.
        ///     It should always be set to "application/json" while communicating with EdgeGap API.
        /// </summary>
        private const string TypeHeaderValue = "application/json";

        /// <summary>
        ///     The URL to post request for new server deployment.
        ///     Refer to https://docs.edgegap.com/api/#operation/deploy for details.
        /// </summary>
        private const string AppDeployURL = "https://api.edgegap.com/v1/deploy";

        /// <summary>
        ///     The URL to get status of a specific deployment request.
        ///     Refer to https://docs.edgegap.com/api/#operation/deployment-status-get for details.
        /// </summary>
        private const string AppStatusURL = "https://api.edgegap.com/v1/status/";

        /// <summary>
        ///     The URL to get a list of all the available deployments.
        ///     Refer to https://docs.edgegap.com/api/#operation/deployment-status-get for details.
        /// </summary>
        private const string AppListURL = "https://api.edgegap.com/v1/deployments";
        
        /// <summary>
        ///     The URL to get the public ip of the player.
        ///     Refer to https://docs.edgegap.com/api/#operation/IP for details.
        /// </summary>
        private const string PublicIpURL = "https://api.edgegap.com/v1/ip";

        /// <summary>
        ///     The HTTP client that will handle all the communication with the EdgeGap API.
        /// </summary>
        private readonly HttpClient _httpClient = new();

        /// <summary>
        ///     The request object that sends the "_requestData" via the "_httpClient".
        /// </summary>
        private HttpResponseMessage _request;

        /// <summary>
        ///     Stores the JSON request data that is properly formatted and converted to a ByteArray
        ///     for properly delivering the HTTP request to the EdgeGap API.
        /// </summary>
        private StringContent _requestData;

        /// <summary>
        ///     Used by the "StartConnectionAttempt" method to communicate with and keep track of the deployment instance
        ///     it's attempting to connect to. This variable is either populated by the "FindDeployedServers" method if trying
        ///     to connect to an existing deployment , or by the "DeployNewServer" method if attempting a new deployment.
        /// </summary>
        private string _requestId;

        /// <summary>
        ///     Acts as a buffer for HTTP request's response that is read asynchronously.
        /// </summary>
        private string _response;

        private string _publicIP;
        
        public Action OnStartGameFailed;

        //These public events are used to communicate with the "MainMenuSystem".
        public Action<string, bool> OnStatusUpdate;


        private void Awake()
        {
            //Setting the proper Content-Type and Authorization header values to the _httpClient.
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(TypeHeaderValue));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", AuthHeaderValue);
        }

        /// <summary>
        ///     This method fetches a list of all live deployments and connect to the first one on the list.
        ///     Free tier accounts are allowed to have only 1 live deployment at a given time, thus connecting
        ///     to the first one makes the most sense. But in case of paid tier account this method can be extended
        ///     easily to allow players to select any preferred deployment from the list and connect to it.
        /// </summary>
        public async void FindDeployedServers()
        {
            //Request the list of deployments by sending a HTTP GET request to the AppListURL on EdgeGap API
            _request = await _httpClient.GetAsync(AppListURL);
            //Store the response into the buffer.
            _response = await _request.Content.ReadAsStringAsync();

            //If the request is successful.
            if (_request.IsSuccessStatusCode)
            {
                //Parse the response string into a JSON object
                var responseJson = JSON.ParseString(_response);
                //If total count of deployments is 0 then start new server deployment.
                if (responseJson.GetInt("total_count") == 0)
                {
                    OnStatusUpdate?.Invoke("No Deployed Servers Found, Attempting To Deploy A New Instance ", false);

                    //Attempt to deploy new server as no instances are found.
                    DeployNewServer();
                }
                //Else connect to the first instance of the deployment from the deployments list.
                else
                {
                    OnStatusUpdate?.Invoke("Found Deployed Servers, Attempting Connection..", false);
                    //Parse the deployments list into a JSON Array.
                    var allServers = responseJson.GetJArray("data");
                    //Grab the entry from the first index of the JSON Array.
                    var firstServer = allServers.GetJSON(0);
                    //Populate the _requestID with the first server's "request_id" parameter.
                    _requestId = firstServer.GetString("request_id");
                    //Attempt to connect to the server.
                    StartConnectionAttempt();
                }
            }
            //Invoked in case of an unexpected error.
            else
            {
                OnStatusUpdate?.Invoke(
                    $"Could Not Start Game, Error {(int)_request.StatusCode} With Message: \n{_response}",
                    true);
                OnStartGameFailed?.Invoke();
            }
        }

        /// <summary>
        ///     This method configures and sends a POST request to the EdgeGap API for deploying a new server instance
        ///     If the server deployment is successful, the "StartConnectionAttempt" method is called.
        /// </summary>
        public async void DeployNewServer()
        {
            //Get the public ip of the player. If null or empty then return from the function
            //GetLocalIPAddress method will take care of error handling and logging.
            await GetLocalIPAddress();
            if(String.IsNullOrEmpty(_publicIP))
                return;
            
            //Create a new JSON object and add the minimum required fields for a deployment.
            var requestJson = new JSON();
            //Please refer to https://docs.edgegap.com/api/#operation/deploy for more details.
            requestJson.Add("app_name", AppName);
            requestJson.Add("version_name", AppVersion);
            requestJson.Add("ip_list", new[] { _publicIP });
            //Convert the JSON object to a ByteArray and format it for "application/json" Content-Type.
            _requestData = new StringContent(requestJson.CreateString(), Encoding.UTF8, TypeHeaderValue);
            //Request new deployment by sending HTTP POST request to the AppDeployURL on EdgeGap API.
            _request = await _httpClient.PostAsync(AppDeployURL, _requestData);
            //Store the response inti the buffer.
            _response = await _request.Content.ReadAsStringAsync();

            //If the request is successful.
            if (_request.IsSuccessStatusCode)
            {
                //Parse the response string into a JSON object.
                var responseJson = JSON.ParseString(_response);
                //Fetch the response_id of the deployed server.
                _requestId = responseJson.GetString("request_id");
                //Start connection attempt to the deployed server.
                OnStatusUpdate?.Invoke("Server Deployment Successful, Attempting To Connect To Server...", false);
                StartConnectionAttempt();
            }
            //Invoked in case of an unexpected error.
            else
            {
                OnStatusUpdate?.Invoke(
                    $"Could Not Deploy Server, Error {(int)_request.StatusCode} With Message: \n{_response}",
                    true);
                OnStartGameFailed?.Invoke();
            }
        }


        private async void StartConnectionAttempt()
        {
            var isServerReady = false;
            while (!isServerReady)
            {
                //Send a GET HTTP request to the EdgeGap api to get the status of the deployment linked to the _requestId.
                _request = await _httpClient.GetAsync(AppStatusURL + _requestId);
                _response = await _request.Content.ReadAsStringAsync();

                if (_request.IsSuccessStatusCode)
                {
                    var responseJson = JSON.ParseString(_response);

                    isServerReady = responseJson.GetBool("running");
                    //If the response has the "running" bool as "true" then the server is running and ready for connection.
                    if (isServerReady)
                    {
                        OnStatusUpdate?.Invoke("Server Is Ready For Connection, Starting The Game...", false);
                        //Grab the port number of the port named "UDP_PORT" from the "ports" object in the responseJson.
                        //For more information on how to configure ports please refer to the "FishNet Example Guide".
                        var serverPort = (ushort)responseJson.GetJSON("ports").GetJSON("UDP_PORT").GetInt("external");
                        //The "fqdn" string in the responseJson represents the server address to which we want the client to connect.
                        var serverAddress = responseJson.GetString("fqdn");

                        //Set the serverPort and serverAddress to the default transport (Tugboat)
                        InstanceFinder.TransportManager.Transport.SetPort(serverPort);
                        InstanceFinder.TransportManager.Transport.SetClientAddress(serverAddress);
                        Debug.Log($"Connecting To Server Using Port {serverPort} And IP {serverAddress}");
                        //StartConnection method will connect to the server with the given port and address.
                        //Once the connection is complete the SceneManager will auto load the "OnlineScene".
                        //Please refer to the "FishNet Example Guide" for more details on SceneManager.
                        InstanceFinder.ClientManager.StartConnection();
                    }
                    //If the server is not ready for connection, we delay the function execution for 10 seconds.
                    //After 10 seconds the loop will run again as the "isServerReady" bool is still set to false 
                    else
                    {
                        OnStatusUpdate?.Invoke("Server Is Not Ready For Connection Retrying In 10 Seconds...", false);
                        await Task.Delay(10000);
                    }
                }
                //Invoked in case of an unexpected error
                else
                {
                    OnStatusUpdate?.Invoke(
                        $"Could Not Update Server Status, Error {(int)_request.StatusCode} With Message: \n{_response}",
                        true);
                    OnStartGameFailed?.Invoke();
                }
            }
        }

        //Function to get the IP address of the client's device
        private async Task GetLocalIPAddress()
        {
            //Request the public ip of the player using the EdgeGap API 
            _request = await _httpClient.GetAsync(PublicIpURL);
            //Store the response into the buffer.
            _response = await _request.Content.ReadAsStringAsync();

          
            if (_request.IsSuccessStatusCode)
            {
                OnStatusUpdate?.Invoke("Fetching The Public Ip Address",false);
                var responseJson = JSON.ParseString(_response);
                var ip = responseJson.GetString("public_ip");
                if (String.IsNullOrEmpty(ip))
                {
                    OnStatusUpdate?.Invoke("Failed To Get Player Public Ip",true);
                    OnStartGameFailed?.Invoke();
                }
                else
                {
                    OnStatusUpdate?.Invoke($"Public Ip Address is {ip}",false);
                    _publicIP = ip;
                }
            }
            else
            {
                OnStatusUpdate?.Invoke(
                    $"Could Not Get Player Ip, Error {(int)_request.StatusCode} With Message: \n{_response}",
                    true);
                OnStartGameFailed?.Invoke();
                
            }
        }
    }
}