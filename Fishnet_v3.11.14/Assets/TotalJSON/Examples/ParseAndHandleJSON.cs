//    Example - Parse and handle JSON


using UnityEngine;
using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Examples {

	public class ParseAndHandleJSON : MonoBehaviour {

		void Start() {

			// Parsing Facebook user data
			Debug.Log("---> Running ParseAndHandleJSON.ReadFBUserData()");
			string dataFromFB = ReadFBUserData();
			Debug.Log("---> Running ParseAndHandleJSON.ParseFBUserData()");
			ParseFBUserData(dataFromFB);

			// Parsing Google Play Store receipt data
			Debug.Log("---> Running ParseAndHandleJSON.ReadGPSReceiptData()");
			string dataFromGPS = ReadGPSReceiptData();
			Debug.Log("---> Running ParseAndHandleJSON.ParseGPSReceiptData()");
			ParseGPSReceiptData(dataFromGPS);

		}
	
		// Returns example JSON string that is in same format than real data received from Facebook would be when receiving player account info
		private string ReadFBUserData() {

			string dataFromFB = @"
				{
					""id"":""123456789012345"",
					""name"":""Mumble Happyfeet"",
					""first_name"":""Mumble"",
					""last_name"":""Happyfeet"",
					""picture"":{
						""data"":{
							""height"":50,
							""width"":50,
							""is_silhouette"":false,
							""url"":""https://platform-lookaside.fbsbx.com/platform/profilepic/?asid=123456789012345&height=50&width=50&ext=1234567890&hash=AbCdeFghijkLmnop""
						}
					},
					""email"":""not.exist@leguar.com"",
					""gender"":""male"",
					""birthday"":""02/02/1979"",
					""location"":{
						""location"":{
							""city"":""Helsinki"",
							""country"":""Finland"",
							""latitude"":60.1708,
							""longitude"":24.9375
						},
						""id"":""109876543210987""
					},
					""currency"":{
						""currency_offset"":100,
						""usd_exchange"":1.1534292,
						""usd_exchange_inverse"":0.8669799585,
						""user_currency"":""EUR""
					}
				}";

			return dataFromFB;

		}

		private void ParseFBUserData(string jsonDataFromFbAsString) {

			// Parse string data received from FB to JSON
			// Second (optional) parameter is ID for debug purposes. If anything fails in JSON parsing, this debug id is added to exception message.
			// This will greatly help tracking problems in projects where lots of JSON objects are handled and stack trace is not necessary available (like in production builds).
			// This same debug id will be attached also to resulting JSON object so that if any exceptions happens afterwards (for example when reading values from JSON),
			// thos eexceptions will have this ID printed out also.
			JSON fbUserJSON = JSON.ParseString(jsonDataFromFbAsString,"FBUserJSON");

			// Set JSON protected (read only) so no accidental changes are made to it
			// In this example this is of course somewhat pointless since JSON is only used locally in this method and not passed forward to anywhere
			// But very handy if this JSON is paased forward and you want to make sure no other code accidentally changes anything
			fbUserJSON.SetProtected();

			// Add this JSON object to runtime debug in Unity Editor
			// After this call, you can see content of this JSON object in Unity Editor, by choosing "Window -> Total JSON -> JSON Runtime Debug" from Unity menu
			fbUserJSON.DebugInEditor("FB User JSON");

			// Get user basic info
			string userId = fbUserJSON.GetString("id");
			string name = fbUserJSON.GetString("name");

			// Get user picture info
			JSON userPictureData = fbUserJSON.GetJSON("picture").GetJSON("data");
			bool isPictureSilhouette = userPictureData.GetBool("is_silhouette");
			string pictureUrl = userPictureData.GetString("url");

			// Get country
			string country;
			try {
				// This code just assumes data contains location information, which it in real life doesn't necessary do
				country = fbUserJSON.GetJSON("location").GetJSON("location").GetString("country");
			}
			catch (JSONKeyNotFoundException) { // The lazy way to handle possibly missing information
				country = "Unknown";
			}

			// Get currency USD exchange
			decimal? usdExchange = null;
			if (fbUserJSON.ContainsKey("currency")) {
				JSON userCurrency = fbUserJSON.GetJSON("currency");
				usdExchange = userCurrency.GetJNumber("usd_exchange").AsDecimal();
			}

			// Print out what we got
			Debug.Log("userId = "+userId);
			Debug.Log("name = "+name);
			Debug.Log("isPictureSilhouette = "+isPictureSilhouette);
			Debug.Log("pictureUrl = "+pictureUrl);
			Debug.Log("country = "+country);
			Debug.Log("usdExchange = "+usdExchange);

		}
		
		// Returns example JSON string that is in same format than real data received from Google Play store would be when receiving purchase receipt
		private string ReadGPSReceiptData() {

			// Note that receipt really contains JSON that added inside other JSON's as strings, causing lots of escaping.
			// Then the string is escaped once more here (using TotalJSON Validator included to this package) to make it compatible for adding it to c# source code.
			string dataFromGPS="{\"Store\":\"GooglePlay\",\"TransactionID\":\"XYZ.1337-4242-1234-12345\",\"Payload\":\"{\\\"json\\\":\\\"{\\\\\\\"orderId\\\\\\\":\\\\\\\"XYZ.1337-4242-1234-12345\\\\\\\",\\\\\\\"packageName\\\\\\\":\\\\\\\"com.example.test\\\\\\\",\\\\\\\"productId\\\\\\\":\\\\\\\"niceProduct\\\\\\\",\\\\\\\"purchaseTime\\\\\\\":1504030209999,\\\\\\\"purchaseState\\\\\\\":0,\\\\\\\"purchaseToken\\\\\\\":\\\\\\\"abcdefghijklmno.AA-B1Cd--AbcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNO\\\\\\\"}\\\",\\\"signature\\\":\\\"abcdefghijklmnopqrstuvwxyz\\\\/0123456789abcdefghijklmnopqrstuvwxyz+ETCetc==\\\"}\"}";
			return dataFromGPS;

			// Actual data without this last round of escaping here, looks like this:
			// {"Store":"GooglePlay","TransactionID":"XYZ.1337-4242-1234-12345","Payload":"{\"json\":\"{\\\"orderId\\\":\\\"XYZ.1337-4242-1234-12345\\\",\\\"packageName\\\":\\\"com.example.test\\\",\\\"productId\\\":\\\"niceProduct\\\",\\\"purchaseTime\\\":1504030209999,\\\"purchaseState\\\":0,\\\"purchaseToken\\\":\\\"abcdefghijklmno.AA-B1Cd--AbcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNO\\\"}\",\"signature\":\"abcdefghijklmnopqrstuvwxyz\\/0123456789abcdefghijklmnopqrstuvwxyz+ETCetc==\"}"}

		}

		private void ParseGPSReceiptData(string jsonDataFromGpsAsString) {

			// Parse string to JSON object
			JSON jsonReceipt = JSON.ParseString(jsonDataFromGpsAsString);

			Debug.Log("Store: "+jsonReceipt.GetString("Store"));
			Debug.Log("TransactionID: "+jsonReceipt.GetString("TransactionID"));

			// Parse payload to JSON object as it is saved as string to previous JSON object
			JSON payloadAsJSON = JSON.ParseString(jsonReceipt.GetString("Payload"));

			string signature = payloadAsJSON.GetString("signature");
			Debug.Log("Signature: "+signature);

			// One more round of parsing, as receipt is saved as string in previous JSON object
			string receiptAsString = payloadAsJSON.GetString("json");
			// In some cases there's no need to parse this string to JSON as some purchase validating backend exactly needs this receipt-json as string

			// Just for sake of additional parsing example
			JSON receiptAsJSON = JSON.ParseString(receiptAsString);

			Debug.Log("packageName: "+receiptAsJSON.GetString("packageName"));
			Debug.Log("purchaseTime: "+receiptAsJSON.GetJNumber("purchaseTime").AsLong());

		}
	
	}

}
