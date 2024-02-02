//    Example - Save and load JSON to/from text file


using UnityEngine;
using System.IO;
using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Examples {

	public class SaveAndLoadFile : MonoBehaviour {

		// Note: Since this is just an example, file is saved to TotalJSON Examples folder!
		// (Normally files should be saved to for example Application.persistentDataPath, depending on platform and what data is saved)
		private const string FILE_PATH = "Assets/TotalJSON/Examples/Test_SaveFile.json";

		void Start() {

			Debug.Log("---> Running SaveAndLoadFile.Test()");
			Test();

		}

		private void Test() {

			// Create JSON object for testing
			JSON originalObject = new JSON();
			originalObject.Add("name", "Test Person");
			originalObject.Add("age", 42);

			// Save to text file
			saveJsonObjectToTextFile(originalObject);

			// Load from text file
			JSON loadedObject = loadTextFileToJsonObject();

			// Check that objects are equal
			Debug.Log("Loaded object equals original object: "+loadedObject.Equals(originalObject));

		}

		private void saveJsonObjectToTextFile(JSON jsonObject) {
			string jsonAsString = jsonObject.CreateString(); // Could also use "CreatePrettyString()" to make more human readable result, it is still valid JSON to read and parse by computer
			StreamWriter writer = new StreamWriter(FILE_PATH);
			writer.WriteLine(jsonAsString);
			writer.Close();
		}

		private JSON loadTextFileToJsonObject() {
			StreamReader reader = new StreamReader(FILE_PATH); 
			string jsonAsString = reader.ReadToEnd();
			reader.Close();
			JSON jsonObject = JSON.ParseString(jsonAsString);
			return jsonObject;
		}

	}

}
