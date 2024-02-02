//    Example - Serialize and Deserialize


using UnityEngine;
using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Examples {

	public class SerializeAndDeserialize : MonoBehaviour {

		void Start() {

			Debug.Log("---> Running SerializeAndDeserialize.SerializePlayerObjectToString()");
			string jsonString = SerializePlayerObjectToString();

			Debug.Log("---> Running SerializeAndDeserialize.DeserializeStringToPlayerObject()");
			DeserializeStringToPlayerObject(jsonString);

			Debug.Log("---> Running SerializeAndDeserialize.SerAndDeserUnityStructs()");
			SerAndDeserUnityStructs();
	
		}

		private string SerializePlayerObjectToString() {

			// Create example player (c# object)
			ExamplePlayerObject examplePlayer = new ExamplePlayerObject();
			examplePlayer.SetTestValues();

			// Print out current player data
			Debug.Log("Original player: "+examplePlayer);

			// Serialize ExamplePlayerObject to JSON object
			JSON json = JSON.Serialize(examplePlayer);

			// Output JSON
			string jsonString = json.CreateString();
			Debug.Log("Player as JSON for storing/transferring: " + jsonString);

			// Content of 'jsonString' will be:
			// {"name":"Test player","position":{"x":1.0,"y":2.0,"z":3.0},
			// "playerColor":{"r":0.0,"g":1.0,"b":0.1,"a":0.9},"score":42000,
			// "levelTimes":[31.41,42.0,12.3],"playerBackPack":[{"name":"axe","uses":99},{"name":"coin","uses":1}],
			// "charClass":{"value__":1},"mapStates":{"cave":78,"lake":42},"alignment":null}
			return jsonString;
	
		}

		private void DeserializeStringToPlayerObject(string jsonString) {

			// Create JSON object from string
			JSON json = JSON.ParseString(jsonString);

			// Re-create ExamplePlayerObject from JSON
			ExamplePlayerObject restoredPlayer = json.Deserialize<ExamplePlayerObject>();

			// Print out
			Debug.Log("Restored player: "+restoredPlayer);

		}

		private void SerAndDeserUnityStructs() {
			
			// Create some basic Unity structs
			Vector3 v3 = new Vector3(3f,14f,15f);
			Quaternion q = new Quaternion(3f,3f,3f,4f);
			Color32 c32 = new Color32(128,255,0,192);
			Color c = new Color(0.5f,1f,0f,0.75f);
			
			// Serialize them to JSON and store under one JSON object
			JSON jsonObject = new JSON();
			jsonObject.Add("v3",JSON.Serialize(v3));
			jsonObject.Add("q",JSON.Serialize(q));
			jsonObject.Add("c32",JSON.Serialize(c32));
			jsonObject.Add("c",JSON.Serialize(c));

			// Print out
			Debug.Log("Structs stored to JSON: "+jsonObject.CreateString());

			// Recreate (deserialize) structs from JSON
			Vector3 vector3recreated = jsonObject.GetJSON("v3").Deserialize<Vector3>();
			Quaternion quaternionRecreated = jsonObject.GetJSON("q").Deserialize<Quaternion>();
			Color32 color32recreated = jsonObject.GetJSON("c32").Deserialize<Color32>();
			Color colorRecreated = jsonObject.GetJSON("c").Deserialize<Color>();

			// Print out
			Debug.Log("Vector3 recreated: "+vector3recreated);
			Debug.Log("Quaternion recreated: "+quaternionRecreated);
			Debug.Log("Color32 recreated: "+color32recreated);
			Debug.Log("Color recreated: "+colorRecreated);

		}

	}
	
}
