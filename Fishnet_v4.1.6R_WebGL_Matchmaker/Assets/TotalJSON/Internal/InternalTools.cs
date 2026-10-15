//  InternalTools


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace Leguar.TotalJSON.Internal {

	class InternalTools {

		internal static JValue objectAsJValue(object value) {
			List<object> stack = new List<object>();
			return objectAsJValue(value, stack);
		}

		private static JValue objectAsJValue(object value, List<object> stack) {

			// Single object?
			JValue jValue=singleObjectAsJValue(value);
			if (jValue!=null) {
				return jValue;
			}

			// Dictionary
			if (value is IDictionary) {
				return dictionaryToJSON((IDictionary)(value), new JSON(), stack);
			}

			// List/array
			if (value is IList) {
				return listToJArray((IList)(value), new JArray(), stack);
			}

			// Unknown, handled by caller
			return null;

		}

		internal static JSON dictionaryToJSON(IDictionary sourceDictionary, JSON targetJSON, List<object> stack) {
			stack.Add(sourceDictionary);
			foreach (object objectKey in sourceDictionary.Keys) {
				if (objectKey==null) { // Dictionary can't have null keys though because calculating hash wouldn't work? No harm to check anyway
					throw (new JArgumentException("Dictionary 'key' can not be null", "sourceDictionary"));
				}
				if (!(objectKey is string)) {
					throw (new JArgumentException("Key have to be string in JSON.<init>(IDictionary) constructor", "sourceDictionary"));
				}
				string stringKey = (string)(objectKey);
				object objectValue = sourceDictionary[objectKey];
				if (stack.Contains(objectValue)) {
					throw (new JArgumentException("Dictionary value is referring to earlier object. This would cause circular JSON.", "sourceDictionary[\""+stringKey+"\"]"));
				}
				JValue jValue = InternalTools.objectAsJValue(objectValue, stack);
				if (jValue==null) {
					throw (new UnknownObjectTypeException(objectValue, "sourceDictionary[\""+stringKey+"\"]"));
				}
				targetJSON.Add(stringKey, jValue);
			}
			stack.Remove(sourceDictionary);
			return targetJSON;
		}

		internal static JArray listToJArray(IList sourceList, JArray targetJArray, List<object> stack) {
			stack.Add(sourceList);
			for (int n = 0; n<sourceList.Count; n++) {
				object listItem = sourceList[n];
				if (stack.Contains(listItem)) {
					throw (new JArgumentException("List item is referring to earlier object. This would cause circular JSON.", "sourceList["+n+"]"));
				}
				JValue jValue = InternalTools.objectAsJValue(listItem, stack);
				if (jValue==null) {
					throw (new UnknownObjectTypeException(listItem, "sourceList["+n+"]"));
				}
				targetJArray.Add(jValue);
			}
			stack.Remove(sourceList);
			return targetJArray;
		}

		internal static JValue serializeObject(object obj, SerializeSettings serializeSettings) {
			List<object> stack = new List<object>();
			return serializeObject(obj, serializeSettings, stack);
		}

		// This never returns null
		private static JValue serializeObject(object obj, SerializeSettings serializeSettings, List<object> stack) {

			JValue singleValue=singleObjectAsJValue(obj);
			if (singleValue!=null) {
				return singleValue;
			}

			if (obj is IList) {
				JArray jArray = new JArray();
				IList list = (IList)(obj);
				stack.Add(obj);
				for (int n=0; n<list.Count; n++) {
					object listItem = list[n];
					if (stack.Contains(listItem)) {
						throw (new SerializeException("List item is referring to earlier object. This would cause circular JSON.", listItem));
					}
					JValue jValue=serializeObject(listItem, serializeSettings, stack);
					if (jValue==null) {
						throw (new SerializeException("List item is type that can't be serialized", listItem));
					}
					jArray.Add(jValue);
				}
				stack.Remove(obj);
				return jArray;
			}

			Type type = obj.GetType();
			if (type.IsGenericType) {
				if (type.GetGenericTypeDefinition()==typeof(Dictionary<,>)) {
					JSON json = new JSON();
					Type[] dictTypes = type.GetGenericArguments();
					bool dictKeyIsString = (dictTypes[0]==typeof(string));
					if (!serializeSettings.AllowNonStringDictionaryKeys && !dictKeyIsString) {
						throw (new SerializeException("Dictionary key is type ('"+dictTypes[0]+"') that can't be serialized. Dictionary keys must be strings, or allow more loose options using SerializeSettings"));
					}
					IDictionary dict = (IDictionary)(obj);
					stack.Add(obj);
					foreach (object objectKey in dict.Keys) {
						object dictionaryValue = dict[objectKey];
						if (stack.Contains(dictionaryValue)) {
							throw (new SerializeException("Dictionary value is referring to earlier object. This would cause circular JSON.", dictionaryValue));
						}
						JValue jValue = serializeObject(dictionaryValue, serializeSettings, stack);
						if (jValue==null) {
							throw (new SerializeException("Dictionary item is type that can't be serialized", dictionaryValue));
						}
						string stringKey;
						if (dictKeyIsString) {
							stringKey = (string)(objectKey);
						} else {
							stringKey = objectKey.ToString();
						}
						json.Add(stringKey, jValue);
					}
					stack.Remove(obj);
					return json;
				}
			}

			JSON jsonSer = new JSON();

			FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			stack.Add(obj);
			foreach (FieldInfo fieldInfo in fieldInfos) {
				if (isSerializing(fieldInfo, serializeSettings.IgnoreSystemAndUnitySerializeAttributes)) {
					string fieldName = fieldInfo.Name;
					object fieldToSerialize = fieldInfo.GetValue(obj);
					if (stack.Contains(fieldToSerialize)) {
						throw (new SerializeException("Class field is referring to earlier object. This would cause circular JSON.", fieldToSerialize));
					}
					JValue jValue = serializeObject(fieldToSerialize, serializeSettings, stack);
					if (jValue==null) {
						throw (new SerializeException("Field \""+fieldName+"\" is type that can't be serialized",fieldToSerialize));
					}
					jsonSer.Add(fieldName,jValue);
				}
			}
			stack.Remove(obj);

			return jsonSer;

		}

		internal static bool isSerializing(FieldInfo fieldInfo, bool ignoreNonJSONSpecificAttributes) {
			object[] attributes = fieldInfo.GetCustomAttributes(false);
			if (containsType(attributes,typeof(ExcludeFromJSONSerializeAttribute))) {
				return false;
			}
			if (!ignoreNonJSONSpecificAttributes) {
				if (containsType(attributes,typeof(System.NonSerializedAttribute))) {
					return false;
				}
			}
			if (fieldInfo.IsPublic && !fieldInfo.IsLiteral) {
				return true;
			}
			if (containsType(attributes,typeof(IncludeToJSONSerializeAttribute))) {
				return true;
			}
			if (!ignoreNonJSONSpecificAttributes) {
				if (containsType(attributes,typeof(UnityEngine.SerializeField))) {
					return true;
				}
			}
			return false;
		}

		private static bool containsType(object[] attributes, Type type) {
			foreach (object attribute in attributes) {
				if (attribute.GetType()==type) {
					return true;
				}
			}
			return false;
		}

		private static JValue singleObjectAsJValue(object value) {

			// Null
			if (value==null) {
				return (new JNull());
			}

			// JValue directly
			if (value is JValue) {
				return ((JValue)(value));
			}

			// Known numbers
			if (value is float) {
				return (new JNumber((float)(value)));
			}
			if (value is double) {
				return (new JNumber((double)(value)));
			}
			if (value is decimal) {
				return (new JNumber((decimal)(value)));
			}
			if (value is byte) {
				return (new JNumber((byte)(value)));
			}
			if (value is sbyte) {
				return (new JNumber((sbyte)(value)));
			}
			if (value is short) {
				return (new JNumber((short)(value)));
			}
			if (value is ushort) {
				return (new JNumber((ushort)(value)));
			}
			if (value is int) {
				return (new JNumber((int)(value)));
			}
			if (value is uint) {
				return (new JNumber((uint)(value)));
			}
			if (value is long) {
				return (new JNumber((long)(value)));
			}
			if (value is ulong) {
				return (new JNumber(((ulong)(value)).ToString(CultureInfo.InvariantCulture)));
			}

			// String
			if (value is string) {
				return (new JString((string)(value)));
			}

			// Bool
			if (value is bool) {
				return (new JBoolean((bool)(value)));
			}

			// Unknown, handled by caller
			return null;

		}

		internal static object jValueAsSystemObject(JValue jValue) {
			if (jValue is JSON) {
				return ((JSON)(jValue)).AsDictionary();
			} else if (jValue is JArray) {
				return ((JArray)(jValue)).AsList();
			} else if (jValue is JNumber) {
				return ((JNumber)(jValue)).AsObject();
			} else if (jValue is JString) {
				return ((JString)(jValue)).AsString();
			} else if (jValue is JBoolean) {
				return ((JBoolean)(jValue)).AsBool();
			} else { // JNull
				return null;
			}
		}

		internal static string getExceptionMessageTailForID(string debugIDForExceptions, string exceptionSource) {
			if (debugIDForExceptions!=null) {
				return (" - "+exceptionSource+" Debug ID: \""+debugIDForExceptions+"\"");
			}
			return "";
		}

		internal static string getCleanedStackTrace(string originalStackTrace) {
			string cleanedStackTrace=originalStackTrace;
			bool first=true;
			do {
				int lf=getLineFeedIndex(cleanedStackTrace);
				if (lf<=0) {
					if (first) {
						// This is unexpected, just returning original stacktrace as fallback
						return originalStackTrace;
					} else {
						return cleanedStackTrace;
					}
				}
				first=false;
				if (!isInternalStackTraceLine(cleanedStackTrace.Substring(0,lf))) {
					return cleanedStackTrace;
				}
				cleanedStackTrace=cleanedStackTrace.Substring(lf+1);
			} while (true);
		}

		private static int getLineFeedIndex(string source) {
			int i=source.IndexOf('\n');
			return i;
		}

		private static bool isInternalStackTraceLine(string str) {
			int packegeName=str.IndexOf("Leguar.TotalJSON.");
			if (packegeName<0) {
				return false;
			}
			int i1=str.IndexOf('(');
			if (i1>0 && i1<packegeName) {
				return false;
			}
			int i2=str.IndexOf('<');
			if (i2>0 && i2<packegeName) {
				return false;
			}
			return true;
		}

#if UNITY_EDITOR
		internal static JSONRuntimeDebugContainer getDebugContainer() {
			GameObject jsonDebugObject = GameObject.Find("TotalJSON_DebugObject");
			if (jsonDebugObject==null) {
				jsonDebugObject=new GameObject("TotalJSON_DebugObject");
				jsonDebugObject.hideFlags=HideFlags.HideInHierarchy;
				GameObject.DontDestroyOnLoad(jsonDebugObject);
			}
			JSONRuntimeDebugContainer jsonRuntimeDebugContainer = jsonDebugObject.GetComponent<JSONRuntimeDebugContainer>();
			if (jsonRuntimeDebugContainer==null) {
				jsonRuntimeDebugContainer=jsonDebugObject.AddComponent<JSONRuntimeDebugContainer>();
			}
			return jsonRuntimeDebugContainer;
		}
#endif

	}

}
