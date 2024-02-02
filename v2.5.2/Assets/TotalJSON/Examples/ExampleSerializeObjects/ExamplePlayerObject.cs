//    Example - Serialize and Deserialize - ExamplePlayerObject

//    This class is part of "SerializeAndDeserialize.cs" example code


using System;
using UnityEngine;
using System.Collections.Generic;
using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Examples {

	public class ExamplePlayerObject {


		public string name;

		public Vector3 position;
		public Color playerColor;

		private int sessionTime; // This field will not get serialized since it is 'private'

		[SerializeField]
		private int score; // This private field will be serialized since it is marked with [SerializeField]

		[NonSerialized]
		public int debugScore; // This public field will not be serialized since it is marked with [NonSerialized]

		public float[] levelTimes;

		public List<ExamplePlayerItemObject> playerBackPack;

		public enum CharClass {
			Fighter,
			Mage,
			Thief
		}

		public CharClass charClass;

		public Dictionary<string,int> mapStates;

		public bool? alignment; // Nullable boolean value to allow also "not set" state


		public void SetTestValues() {
			name = "Test player";
			position = new Vector3(1f, 2f, 3f);
			playerColor = new Color(0f, 1f, 0.1f, 0.9f);
			sessionTime = 55555;
			score = 42000;
			debugScore = score;
			levelTimes = new float[] { 31.41f, 42.0f, 12.3f };
			playerBackPack = new List<ExamplePlayerItemObject>();
			playerBackPack.Add(new ExamplePlayerItemObject() { name="axe", uses=99 });
			playerBackPack.Add(new ExamplePlayerItemObject() { name="coin", uses=1 });
			charClass = CharClass.Mage;
			mapStates=new Dictionary<string, int>();
			mapStates.Add("cave", 78);
			mapStates.Add("lake", 42);
			alignment = null;
		}

		public override string ToString() {
			string str = "[ExamplePlayerObject: name = \""+name+"\", position = "+position+", playerColor = "+playerColor+", sessionTime = "+sessionTime+", score = "+score+", debugScore = "+debugScore+", levelTimes = ";
			if (levelTimes!=null) {
				str += '{';
				for (int n = 0; n<levelTimes.Length; n++) {
					str += levelTimes[n];
					if (n<levelTimes.Length-1) {
						str += ",";
					}
				}
				str += '}';
			} else {
				str += "null";
			}
			str += ", playerBackPack = ";
			if (playerBackPack!=null) {
				str += '{';
				for (int n = 0; n<playerBackPack.Count; n++) {
					str += playerBackPack[n].ToString();
					if (n<playerBackPack.Count-1) {
						str += ',';
					}
				}
				str += '}';
			} else {
				str += "null";
			} 
			str += ", charClass = "+charClass+", mapStates = ";
			if (mapStates!=null) {
				str += '{';
				bool first = true;
				foreach (string key in mapStates.Keys) {
					if (!first) {
						str += ", ";
					}
					str += key+"="+mapStates[key];
					first=false;
				}
				str += '}';
			} else {
				str += "null";
			}
			str += ", alignment = " + alignment + "]";
			return str;
		}
		
	}

}
