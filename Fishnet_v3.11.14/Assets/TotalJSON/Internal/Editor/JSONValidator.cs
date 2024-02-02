//    JSONValidator


using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using Leguar.TotalJSON;

namespace Leguar.TotalJSON.Internal {

	public class JSONValidator : EditorWindow {

		private string editorAreaText="Write or copy&paste JSON object or JSON array to this text area. Example JSON:\n\n{\"name\":\"Player\",\"lastLogin\":123456789012,\"achievements\":[42,1337,1703],\"imageUrl\":null,\"have_bought\":true,\"levels\":[{\"passed\":true,\"score\":12345},{\"passed\":false}]}\n\nExtra texts like this before or after JSON object will be removed when clicking button below.";

		private string message="";
		private string tightJSON="";
		private string escapedJSON="";

		private Vector2 scrollPos;

		private float lineHeight=EditorGUIUtility.singleLineHeight;

		[MenuItem("Window/Total JSON/JSON Validator")]
		static void Init() {
			JSONValidator window=(JSONValidator)(GetWindow(typeof(JSONValidator)));
#if UNITY_5 || UNITY_2017
			window.titleContent = new GUIContent("JSON Validator");
#else
			Texture2D icon = (Texture2D)(AssetDatabase.LoadAssetAtPath("Assets/TotalJSON/Internal/Editor/window-icon.png", typeof(Texture2D)));
			window.titleContent = new GUIContent("JSON Validator",icon);
#endif
		}

		void OnGUI() {

			GUILayout.Space(20);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			EditorGUI.BeginChangeCheck();
			editorAreaText = EditorGUILayout.TextArea(editorAreaText,GUILayout.ExpandHeight(true));
			bool changes = EditorGUI.EndChangeCheck();
			EditorGUILayout.EndScrollView();

			if (changes) {
				message = "";
				tightJSON = "";
				escapedJSON = "";
			}

			GUILayout.Space(10);

			if (GUILayout.Button("Trim, validate and prettify above JSON object or array")) {

				string trimmedEditorText=editorAreaText.Trim();
				if (trimmedEditorText.Length<editorAreaText.Length) {
					editorAreaText=trimmedEditorText; // Needed in case of errors, so that line numbers will match
					GUIUtility.keyboardControl=0;
					GUIUtility.hotControl=0;
					EditorUtility.SetDirty(this);
				}

				if (string.IsNullOrEmpty(trimmedEditorText)) {
					message = "Input is empty";
					tightJSON = "";
					escapedJSON = "";

				} else {
					object objectOrError = findAndGetJSONOrJArray(trimmedEditorText);
					if (objectOrError==null) {
						message = "Can't find JSON start from input";
						tightJSON = "";
						escapedJSON = "";
					} else if (objectOrError is string) {
						message = "Invalid input: "+((string)(objectOrError));
						tightJSON = "";
						escapedJSON = "";
					} else {
						CreateStringSettings prettySettings=new CreateStringSettings() {
							HumanReadable=true,
							IndentUsingTab=true,
							NewLine=CreateStringSettings.NewLineTypes.LF // \r characters (may be part of environment default) seem to be problem in editor textarea, causing invisible linefeeds, so using plain \n
						};
						if (objectOrError is JSON[]) {
							JSON[] jsons = (JSON[])(objectOrError);
							if (jsons.Length==1) {
								message = "JSON is valid. Top level JSON key/value pair count = "+jsons[0].Count;
								editorAreaText = jsons[0].CreateString(prettySettings)+"\n";
								tightJSON = jsons[0].CreateString(new CreateStringSettings() { HumanReadable = false });
								escapedJSON = "\""+getEscapedString(tightJSON)+"\"";
							} else {
								message = "JSONs are valid. JSON object count = "+jsons.Length;
								editorAreaText = "";
								tightJSON = "";
								for (int n = 0; n<jsons.Length; n++) {
									editorAreaText += jsons[n].CreateString(prettySettings)+"\n";
									if (n<jsons.Length-1) {
										editorAreaText += '\n';
									}
									tightJSON += jsons[n].CreateString(new CreateStringSettings() { HumanReadable = false });
								}
								escapedJSON = "\""+getEscapedString(tightJSON)+"\"";
							}
						} else {
							JArray jArray = (JArray)(objectOrError);
							message = "JSON Array is valid. Top level array length = "+jArray.Length;
							editorAreaText = jArray.CreateString(prettySettings)+"\n";
							tightJSON = jArray.CreateString(new CreateStringSettings() { HumanReadable = false });
							escapedJSON = "\""+getEscapedString(tightJSON)+"\"";
						}
						GUIUtility.keyboardControl=0;
						GUIUtility.hotControl=0;
						EditorUtility.SetDirty(this);
					}
				}
			}

			GUILayout.Space(20);
			GUILayout.Label(message);
			GUILayout.Space(20);

			EditorGUI.BeginDisabledGroup(tightJSON.Length==0);

			GUILayout.Label("JSON formatted and encoded string ("+tightJSON.Length+" bytes):");
			GUILayout.Space(5);
			EditorGUILayout.SelectableLabel(tightJSON,EditorStyles.textField,GUILayout.Height(lineHeight+2));
			GUILayout.Space(15);
			GUILayout.Label("Above string with escapes (to be used for example directly in c# source code):");
			GUILayout.Space(5);
			EditorGUILayout.SelectableLabel(escapedJSON,EditorStyles.textField,GUILayout.Height(lineHeight+2));
			GUILayout.Space(20);

			EditorGUI.EndDisabledGroup();

		}

		private object findAndGetJSONOrJArray(string dirtySourceString) {

			// Remove any heading "trash", for example in case that JSON is pasted from Unity Console
			int jsonStartIndex=dirtySourceString.IndexOf('{');
			int jArrayStartIndex=dirtySourceString.IndexOf('[');

			//		Debug.Log("jsonStartIndex = "+jsonStartIndex);
			//		Debug.Log("jArrayStartIndex = "+jArrayStartIndex);

			// Nothing?
			if (jsonStartIndex<0 && jArrayStartIndex<0) {
				return null;
			}

			// Try parse array
			if (jArrayStartIndex>=0 && (jArrayStartIndex<jsonStartIndex || jsonStartIndex<0)) {
				try {
					return JArray.ParseString(dirtySourceString,new ParseStringSettings(){ ParseStartIndex=jArrayStartIndex, AllowNonWhiteCharactersAfterObject=true });
				}
				catch (ParseException e) {
					if (jsonStartIndex<0) {
						return e.Message;
					}
				}
			}

			// Parse one or more JSONs
			try {
				return JSON.ParseStringToMultiple(dirtySourceString,new ParseStringSettings(){ ParseStartIndex=jsonStartIndex, AllowNonWhiteCharactersAfterObject=true });
			}
			catch (ParseException e) {
				return e.Message;
			}

		}

		private static string getEscapedString(string source) {
			int length=source.Length;
			StringBuilder sb=new StringBuilder(length);
			for (int n=0; n<length; n++) {
				if (source[n]=='"') {
					sb.Append('\\');
					sb.Append('"');
				} else if (source[n]=='\\') {
					sb.Append('\\');
					sb.Append('\\');
				} else {
					sb.Append(source[n]);
				}
			}
			return sb.ToString();
		}

	}

}
