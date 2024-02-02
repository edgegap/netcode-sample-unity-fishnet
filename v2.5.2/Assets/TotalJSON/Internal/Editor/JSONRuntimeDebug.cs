//    JSONRuntimeDebug


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Leguar.TotalJSON.Internal {

	public class JSONRuntimeDebug : EditorWindow {

		private static int selected=0;
		private static List<DebugObject> latestObjects;
		private static bool previousWasPlaying = false;

		[MenuItem("Window/Total JSON/JSON Runtime Debug")]
		static void Init() {
			JSONRuntimeDebug window=(JSONRuntimeDebug)(GetWindow(typeof(JSONRuntimeDebug)));
#if UNITY_5 || UNITY_2017
			window.titleContent = new GUIContent("JSON Runtime Debug");
#else
			Texture2D icon = (Texture2D)(AssetDatabase.LoadAssetAtPath("Assets/TotalJSON/Internal/Editor/window-icon.png", typeof(Texture2D)));
			window.titleContent = new GUIContent("JSON Runtime Debug",icon);
#endif
		}

		void OnGUI() {

			GUILayout.Space(15);

			if (!Application.isPlaying) {

				if (latestObjects==null) {

					GUILayout.Label("Application is not running. This debug is available only when application is running and some JSON/Jarray object is added to debug.");

				} else {

					GUILayout.Label("Application is not running. Below is last state of JSON/Jarray objects from previous run.");
					if (previousWasPlaying) {
						foreach (DebugObject latestObject in latestObjects) {
							latestObject.refresh();
						}
					}
					outputLatestContent();

				}

				previousWasPlaying=false;
				return;

			} else {

				previousWasPlaying=true;

			}

			JSONRuntimeDebugContainer jsonRuntimeDebugContainer=null;
			GameObject jsonDebugObject=GameObject.Find("TotalJSON_DebugObject");
			if (jsonDebugObject!=null) {
				jsonRuntimeDebugContainer=jsonDebugObject.GetComponent<JSONRuntimeDebugContainer>();
			}
			if (jsonRuntimeDebugContainer==null) {
				GUILayout.Label("Application is running but no JSON objects are added to debug.");
				latestObjects=null;
				return;
			}

			GUILayout.Label("Application is running, choose object below to show.");

			if (latestObjects==null) {
				latestObjects=new List<DebugObject>();
			}

			Dictionary<string,JValue> currentContent=jsonRuntimeDebugContainer.getContent();
			foreach (string key in currentContent.Keys) {
				int listIndex=getDebugObjectIndex(key);
				if (listIndex>=0) {
					if (latestObjects[listIndex].getValue()!=currentContent[key]) {
						latestObjects[listIndex].replace(currentContent[key]);
					}
				} else {
					latestObjects.Add(new DebugObject(key,currentContent[key]));
				}
			}

			outputLatestContent();

		}

		private int getDebugObjectIndex(string key) {
			for (int n=0; n<latestObjects.Count; n++) {
				if (latestObjects[n].getKey().Equals(key)) {
					return n;
				}
			}
			return -1;
		}

		private void outputLatestContent() {

			GUILayout.Space(10);

			int count=latestObjects.Count;
			string[] keys=new string[latestObjects.Count];
			for (int n=0; n<count; n++) {
				keys[n]=latestObjects[n].getKey();
			}

			int newSelected=GUILayout.Toolbar(selected,keys);

			if (newSelected!=selected) {
				selected=newSelected;
				GUIUtility.keyboardControl=0;
				GUIUtility.hotControl=0;
			}

			GUILayout.Space(10);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(latestObjects[selected].getInfoString());
			if (Application.isPlaying) {
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Refresh")) {
					latestObjects[selected].refresh();
					Repaint();
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			latestObjects[selected].scrollPos = EditorGUILayout.BeginScrollView(latestObjects[selected].scrollPos);
			EditorGUILayout.TextArea(latestObjects[selected].getContentString(),GUILayout.ExpandHeight(true));
			EditorGUILayout.EndScrollView();

		}

		private class DebugObject {

			private string key;
			private JValue value;

			internal Vector2 scrollPos;
			private string infoString;
			private string contentString;

			internal DebugObject(string key, JValue value) {
				this.key=key;
				this.value=value;
				refresh();
			}

			internal void replace(JValue value) {
				this.value=value;
				refresh();
			}

			internal void refresh() {
				scrollPos=Vector2.zero;
				infoString=null;
				contentString=null;
			}

			internal string getKey() {
				return key;
			}

			internal JValue getValue() {
				return value;
			}

			internal string getInfoString() {
				if (infoString==null) {
					infoString=value.ToString();
					if (isProtected()) {
						infoString+=" -- This object is set protected (read only)";
					}
				}
				return infoString;
			}

			internal string getContentString() {
				if (contentString==null) {
					contentString=value.CreateString(new CreateStringSettings() { HumanReadable=true });
				}
				return contentString;
			}

			private bool isProtected() {
				if (value is JSON) {
					return ((JSON)(value)).IsProtected();
				}
				if (value is JArray) {
					return ((JArray)(value)).IsProtected();
				}
				return false;
			}

		}

	}

}
