//    Example - Create and modify JSON


using UnityEngine;
using System.Collections.Generic;
using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Examples {

	public class CreateAndModifyJSON : MonoBehaviour {

		void Start() {

			Debug.Log("---> Running CreateAndModifyJSON.Test1()");
			Test1();

			Debug.Log("---> Running CreateAndModifyJSON.Test2()");
			Test2();
	
		}
	
		private void Test1() {

			// Create empty JSON object "{}"
			JSON jsonObject = new JSON();

			// Add this JSON object to debug
			// After this call, you can see content of this JSON object in Unity Editor, by choosing "Window -> Total JSON -> JSON Runtime Debug" from Unity menu
			// For debug/development purposes it is very handy to see contents of JSON objects while application is running in editor
			jsonObject.DebugInEditor("CreateAndModify Test JSON");

			Debug.Log("Note! You can see the content of selected JSON objects while application is running by choosing \"Window -> Total JSON -> JSON Runtime Debug\" from Unity menu");

			// Add basic things
			jsonObject.Add("text","Hello World!");
			jsonObject.Add("number",42);
			jsonObject.Add("truth",true);
			jsonObject.Add("nullText",null);
			jsonObject.Add("numberArray",new int[]{1,2,4,8});

			// Print out
			Debug.Log(jsonObject.CreateString()); // {"text":"Hello World!","number":42,"truth":true,"nullText":null,"numberArray":[1,2,4,8]}

			// Do some changes
			jsonObject.Remove("number");
			jsonObject.Replace("truth",false);

			// Loop through all the keys and print out debug info of all values
			Debug.Log("Info of all the values:");
			foreach (string key in jsonObject.Keys) {
				Debug.Log(key+": "+jsonObject[key]);
			}

			// Loop through all the keys and print out all values in JSON formatted strings
			Debug.Log("Content of all the values:");
			foreach (string key in jsonObject.Keys) {
				Debug.Log(key+" -> "+jsonObject[key].CreateString());
			}

			// Get and print out some values
			Debug.Log("Print outs of some values:");
			Debug.Log(jsonObject.GetString("text")); // "Hello World!"
			Debug.Log(jsonObject.ContainsKey("number")); // false
			Debug.Log(jsonObject.GetBool("truth")); // false
			Debug.Log(jsonObject.GetString("nullText")); // Null

			// Do some changes to array
			JArray jsonArray = jsonObject.GetJArray("numberArray"); // Contains ints 1,2,4,8
			jsonArray.Add(16); // 1,2,4,8,16
			jsonArray.RemoveAt(1); // 1,4,8,16
			jsonArray.InsertAt(3,0); // 1,4,8,0,16
			jsonArray.ReplaceAt(2,-1); // 1,4,-1,0,16

			// Print out some array values
			Debug.Log("Array length: "+jsonArray.Length); // 5
			Debug.Log("Fifth element as int: "+jsonArray.GetInt(4)); // 16

			// Since all values in array are numbers (JNumber) and they all fit to c# int values, array can be also copied to system int array
			int[] systemIntArray = jsonArray.AsIntArray();
			// Print out values
			Debug.Log("Second element as int: "+systemIntArray[1]); // 4

		}

		private void Test2() {

			// Create new JSON object from dictionary
			Dictionary<string,object> dict = new Dictionary<string,object>();
			dict.Add("one",1);
			dict.Add("two",2f);
			JSON jsonFromDict = new JSON(dict);

			// Create new JSON array from list
			List<string> list = new List<string>();
			list.Add("ABC");
			list.Add("XYZ");
			list.Add("ijk");
			JArray jArrayFromList = new JArray(list);

			// Create number that would be outside c# supported integers, by creating new JSON number from string
			JNumber bigInteger = new JNumber("184467440737095516150");

			// Add them all to one JSON
			JSON json = new JSON();
			json.Add("fromDict",jsonFromDict);
			json.Add("fromList",jArrayFromList);
			json.Add("bigInteger",bigInteger);

			// Print out in pretty form
			Debug.Log("Human readable output:\n" + json.CreateString(new CreateStringSettings() { HumanReadable=true }));
			/*
				Prints out:
				{
					"fromDict": {
						"one": 1,
						"two": 2.0
					},
					"fromList": [
						"ABC",
						"XYZ",
						"ijk"
					],
					"bigInteger": 184467440737095516150
				}
			*/

		}
		
	}

}
