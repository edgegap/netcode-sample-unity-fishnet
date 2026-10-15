//  JValue - JSON


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Basic JSON object. Instance of this class contains key-value pairs, where key is always string and values can be any JValue objects.
	/// New values can be added, removed and replaced freely.
	/// </summary>
	public class JSON : JValue {

		private Dictionary<string,JValue> keyValuePairs;
		private List<string> keyOrder;

		private bool thisProtected;
		private string debugIdForExceptions;

		/// <summary>
		/// Creates new empty JSON object.
		/// </summary>
		public JSON() : base() {
			keyValuePairs = new Dictionary<string,JValue>();
			keyOrder = new List<string>();
			thisProtected = false;
		}

		/// <summary>
		/// Creates new JSON object from c# dictionary. Note that values in dictionary need to be basic c# objects like string, booleans,
		/// numbers, or another dictionaries or lists. To turn other classes to JSON object, or for more flexible options, consider using
		/// static JSON.Serialize() method.
		/// </summary>
		/// <param name="sourceKeysAndValues">
		/// All the keys and values to be added to this JSON.
		/// Keys have to be strings and values have to be either JValue, or any basic c# object that can be changed to JValue.
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter is null.
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If any exceptions occurs when trying to turn parameter dictionary to new JSON object.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If any value in dictionary is unsupported type.
		/// </exception>
		public JSON(IDictionary sourceKeysAndValues) : this() {
			if (sourceKeysAndValues==null) {
				throw (new JArgumentNullException("sourceKeysAndValues","Parameter in constructor JSON.<init>(IDictionary) can not be null"));
			}
			InternalTools.dictionaryToJSON(sourceKeysAndValues, this, new List<object>());
		}

		public override string ToString() {
			if (keyValuePairs.Count==0) {
				return ("[JSON: Empty JSON]");
			} else {
				return ("[JSON: Key/value pair count = "+keyValuePairs.Count+"]");
			}
		}

        /// <summary>
        /// Test if another object equals to this object. Always returns false if parameter object is null or it is not instance of JSON.
        /// Two JSON objects are equal if both contains same keys and every element mapped to each key equals to each other.
        /// 
        /// Note that string representation generated using CreateString() may not have identical output from two JSON objects that still equals.
        /// This is because key-value pairs may be in different order when they are written to string.
        /// </summary>
        /// <param name="anotherObject">
        /// Another object that is compared to this one.
        /// </param>
        /// <returns>
        /// True if objects are equal, false otherwise.
        /// </returns>
        public override bool Equals(object anotherObject) {
			if (anotherObject==null) {
				return false;
			}
			if (!(anotherObject is JSON)) {
				return false;
			}
			JSON anotherJSON=(JSON)(anotherObject);
			int count=keyValuePairs.Count;
			if (count!=anotherJSON.Count) {
				return false;
			}
			foreach (string key in keyValuePairs.Keys) {
				if (!anotherJSON.ContainsKey(key)) {
					return false;
				}
				if (!keyValuePairs[key].Equals(anotherJSON[key])) {
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode() {
			int hashCode=107;
			foreach (string key in keyValuePairs.Keys) {
				hashCode^=key.GetHashCode();
				hashCode^=keyValuePairs[key].GetHashCode();
			}
			return hashCode;
		}

		/// <summary>
		/// Gets the count of key/value pairs in this JSON.
		/// </summary>
		/// <value>
		/// Count of key/value pairs.
		/// </value>
		public int Count {
			get {
				return keyValuePairs.Count;
			}
		}

		/// <summary>
		/// Gets all the keys in this JSON object.
		/// You can use this to loop through all the keys in this json, for example:
		/// <code>foreach (string key in json.Keys) {...}</code>
		/// 
		/// Array of returned string keys is copy of internal dictionary keys of this JSON object.
		/// This means you can add or remove values from this JSON while looping through the keys.
		/// For example:
		/// <code>
		/// foreach (string key in json.Keys) {
		///     if (shouldRemove(key)) {
		///         json.Remove(key);
		///     }
		/// }
		/// </summary>
		/// <value>
		/// All the keys in this JSON object.
		/// </value>
		public string[] Keys {
			get {
				Dictionary<string,JValue>.KeyCollection keyCollection=keyValuePairs.Keys;
				int count = keyCollection.Count;
				string[] keys = new string[count];
				keyCollection.CopyTo(keys,0);
				return keys;
			}
		}

		/// <summary>
		/// Check whatever this JSON object contains key/value pair with specified key.
		/// </summary>
		/// <param name="keyToSearch">
		/// Key to look for.
		/// </param>
		/// <returns>
		/// <c>true</c> if key exists, <c>false</c> otherwise.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter value is null.
		/// </exception>
		public bool ContainsKey(string keyToSearch) {
			if (keyToSearch==null) {
				throw (new JArgumentNullException("keyToSearch","'keyToSearch' can not be null in JSON.ContainsKey(string)"+getExceptionMessageTail()));
			}
			return keyValuePairs.ContainsKey(keyToSearch);
		}

		// Check whatever this JSON object or recursively any of child objects contains specified object.
		internal bool deepContainsObject(JValue obj) {
			foreach (JValue value in keyValuePairs.Values) {
				if (value==obj) {
					return true;
				} else if (value is JSON) {
					if (((JSON)(value)).deepContainsObject(obj)) {
						return true;
					}
				} else if (value is JArray) {
					if (((JArray)(value)).deepContainsObject(obj)) {
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Gets or sets the value specified by key. Setting value can do both adding new value or replacing old one with same key.
		/// </summary>
		/// <param name="key">
		/// Key of the value to get or set, can not be null.
		/// </param>
		/// <value>
		/// Any JValue object, can not be null but can be JNull.
		/// </value>
		public JValue this[string key] {
			get {
				return Get(key);
			}
			set {
				AddOrReplace(key,value);
			}
		}

		/// <summary>
		/// Gets value specified by key. This method will cause exception if key/value pair doesn't exist.
		/// Returned value may be JSON, JString, JNumber, JBoolean, JArray or JNull.
		/// </summary>
		/// <param name="key">
		/// Key of the value to get.
		/// </param>
		/// <returns>
		/// Value mapped to key. Can't be null, but can be JNull.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		public JValue Get(string key) {
			checkKeyExists(key);
			return keyValuePairs[key];
		}

		/// <summary>
		/// Gets JSON value specified by key. This method will cause exception if key/value pair doesn't exist or value is anything else than JSON or JNull.
		/// </summary>
		/// <param name="key">
		/// Key of the JSON value to get.
		/// </param>
		/// <returns>
		/// JSON value or null if key was mapped to JNull.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JSON or JNull.
		/// </exception>
		public JSON GetJSON(string key) {
			checkKeyExists(key);
			JValue jValue=keyValuePairs[key];
			if (jValue is JSON) {
				return ((JSON)(jValue));
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(key,jValue,"JSON",getExceptionMessageTail()));
		}

		/// <summary>
		/// Gets JNumber value specified by key. This method will cause exception if key/value pair doesn't exist or value is anything else than JNumber or JNull.
		/// </summary>
		/// <param name="key">
		/// Key of the JNumber value to get.
		/// </param>
		/// <returns>
		/// JNumber value or null if key was mapped to JNull.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JNumber or null.
		/// </exception>
		public JNumber GetJNumber(string key) {
			checkKeyExists(key);
			JValue jValue=keyValuePairs[key];
			if (jValue is JNumber) {
				return ((JNumber)(jValue));
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(key,jValue,"JNumber",getExceptionMessageTail()));
		}

		/// <summary>
		/// Gets integer number specified by key. This method will cause exception if key/value pair doesn't exist, value is not number or it is outside int range.
		/// </summary>
		/// <param name="key">
		/// Key of the int value to get.
		/// </param>
		/// <returns>
		/// C# int value.
		/// </returns>
		/// <remarks>
		/// This method is for convenience. It is equal to <code>json.GetJNumber(key).AsInt();</code>
		/// If there is need to get number value from JSON in some very specific format, use <code>GetJNumber(key)</code> and
		/// then methods provided by JNumber class, for example <code>json.GetJNumber(key).AsULong();</code>
		/// </remarks>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueNullException">
		/// If value mapped to this key is null.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JNumber.
		/// </exception>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in int range.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public int GetInt(string key) {
			JNumber jNumber=GetJNumber(key);
			if (jNumber==null) {
				throw (new JValueNullException(key,"int",getExceptionMessageTail()));
			}
			return jNumber.AsInt();
		}

		/// <summary>
		/// Gets floating point number specified by key. This method will cause exception if key/value pair doesn't exist, value is not number or it is outside float range.
		/// </summary>
		/// <param name="key">
		/// Key of the float value to get.
		/// </param>
		/// <returns>
		/// C# float value.
		/// </returns>
		/// <remarks>
		/// This method is for convenience. It is equal to <code>json.GetJNumber(key).AsFloat();</code>
		/// If there is need to get number value from JSON in some very specific format, use <code>GetJNumber(key)</code> and
		/// then methods provided by JNumber class, for example <code>json.GetJNumber(key).AsDecimal();</code>
		/// </remarks>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueNullException">
		/// If value mapped to this key is null.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JNumber.
		/// </exception>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in float.
		/// </exception>
		public float GetFloat(string key) {
			JNumber jNumber=GetJNumber(key);
			if (jNumber==null) {
				throw (new JValueNullException(key,"float",getExceptionMessageTail()));
			}
			return jNumber.AsFloat();
		}

		/// <summary>
		/// Gets string value specified by key. This method will cause exception if key/value pair doesn't exist or value is anything else than JString or JNull.
		/// </summary>
		/// <param name="key">
		/// Key of the string value to get.
		/// </param>
		/// <returns>
		/// C# string or null if key was mapped to JNull.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JString of JNull.
		/// </exception>
		public string GetString(string key) {
			checkKeyExists(key);
			JValue jValue=keyValuePairs[key];
			if (jValue is JString) {
				return ((JString)(jValue)).AsString();
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(key,jValue,"string",getExceptionMessageTail()));
		}

		/// <summary>
		/// Gets c# bool value specified by key. This method will cause exception if key/value pair doesn't exist or value is not boolean.
		/// </summary>
		/// <param name="key">
		/// Key of the bool value to get.
		/// </param>
		/// <returns>
		/// C# bool value.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JBoolean.
		/// </exception>
		public bool GetBool(string key) {
			checkKeyExists(key);
			JValue jValue=keyValuePairs[key];
			if (jValue is JBoolean) {
				return ((JBoolean)(jValue)).AsBool();
			}
			throw (new JValueTypeException(key,jValue,"bool",getExceptionMessageTail()));
		}

		/// <summary>
		/// Gets JArray specified by key. This method will cause exception if key/value pair doesn't exist or value is anything else than JArray or JNull.
		/// </summary>
		/// <param name="key">
		/// Key of the array to get.
		/// </param>
		/// <returns>
		/// JArray object or null if key was mapped to JNull.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="JValueNullException">
		/// If value mapped to this key is JNull.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value mapped to this key is not JArray.
		/// </exception>
		public JArray GetJArray(string key) {
			checkKeyExists(key);
			JValue jValue=keyValuePairs[key];
			if (jValue is JArray) {
				return ((JArray)(jValue));
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(key,jValue,"JArray",getExceptionMessageTail()));
		}

		/// <summary>
		/// Check if value specified by key is JNull. This method throws exception if key doesn't exist.
		/// </summary>
		/// <param name="key">
		/// Key of the value to test.
		/// </param>
		/// <returns>
		/// <c>true</c> if value exists and it is JNull, <c>false</c> otherwise.
		/// </returns>
		/// <exception cref="JSONKeyNotFoundException">
		/// If key doesn't exist in this JSON.
		/// </exception>
		public bool IsJNull(string key) {
			if (keyValuePairs.ContainsKey(key)) {
				JValue jValue = keyValuePairs[key];
				return (jValue is JNull);
			}
			throw (new JSONKeyNotFoundException("Value for key \""+key+"\" can't be tested for being JNull since key does not exist in this JSON"+getExceptionMessageTail()));
		}

		/// <summary>
		/// Add new key/value pair to this JSON. This method will prevent accidental overwriting old values with same key.
		/// If there already is key/value pair with same key, this method will throw an exception.
		/// </summary>
		/// <param name="key">
		/// Key of the value, can not be null.
		/// </param>
		/// <param name="value">
		/// Value to add. Either any JValue, or any basic c# object that can be changed to JValue (string, bool, numbers and null).
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter key is null.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JSON object is set protected (read only).
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If adding parameter value to this JSON would cause circular JSON, meaning this JSON or recursively any of its children would contain this JSON itself.
		/// </exception>
		/// <exception cref="JSONKeyAlreadyExistsException">
		/// If parameter key already exists in this JSON.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type.
		/// </exception>
		public void Add(string key, object value) {
			if (key==null) {
				throw (new JArgumentNullException("key","'key' in JSON.Add(string key, JValue value) method can not be null"+getExceptionMessageTail()));
			}
			if (this.ContainsKey(key)) {
				throw (new JSONKeyAlreadyExistsException("Key/value pair with the same key (\""+key+"\") already exists in this JSON. Change key name, remove old element first, or use AddOrReplace or Replace method to overwrite old element."+getExceptionMessageTail()));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not add new value to this JSON, this JSON is set protected (read only)"+getExceptionMessageTail()));
			}
			if (value==this) {
				throw (new JArgumentException("Trying to add JSON value to itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
			}
			JValue jValue=InternalTools.objectAsJValue(value);
			if (jValue==null) {
				throw (new UnknownObjectTypeException(value,"value"));
			}
			if (jValue is JSON) {
				if (((JSON)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to add JSON value which contains this JSON itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
				}
			}
			if (jValue is JArray) {
				if (((JArray)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to add JArray value which contains this JSON itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
				}
			}
			keyValuePairs.Add(key,jValue);
			keyOrder.Add(key);
		}

		/// <summary>
		/// Add or replace key/value pair in this JSON. Possible previous value with same key is replaced.
		/// </summary>
		/// <param name="key">
		/// Key of the value, can not be null.
		/// </param>
		/// <param name="value">
		/// Value to set. Either any JValue, or any basic c# object that can be changed to JValue (string, bool, numbers and null).
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter key or value is null.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JSON object is set protected (read only).
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If adding parameter value to this JSON would cause circular JSON, meaning this JSON or recursively any of its children would contain this JSON itself.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type.
		/// </exception>
		public void AddOrReplace(string key, object value) {
			if (key==null) {
				throw (new JArgumentNullException("key","'key' in JSON.AddOrReplace(string key, JValue value) method can not be null"+getExceptionMessageTail()));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not set value to this JSON, this JSON is set protected (read only)"+getExceptionMessageTail()));
			}
			if (value==this) {
				throw (new JArgumentException("Trying to set JSON value to itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
			}
			JValue jValue=InternalTools.objectAsJValue(value);
			if (jValue==null) {
				throw (new UnknownObjectTypeException(value,"value"));
			}
			if (jValue is JSON) {
				if (((JSON)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JSON value which contains this JSON itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
				}
			}
			if (jValue is JArray) {
				if (((JArray)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JArray value which contains this JSON itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
				}
			}
			if (keyValuePairs.ContainsKey(key)) {
				keyValuePairs[key] = jValue;
			} else {
				keyValuePairs.Add(key,jValue);
				keyOrder.Add(key);
			}
		}

		/// <summary>
		/// Replace existing key/value pair in this JSON. If there isn't key/value pair with same key, this method will throw an exception.
		/// </summary>
		/// <param name="key">
		/// Key of the value, can not be null.
		/// </param>
		/// <param name="value">
		/// Value to set. Either any JValue, or any basic c# object that can be changed to JValue (string, bool, numbers and null).
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter key or value is null.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JSON object is set protected (read only).
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If adding parameter value to this JSON would cause circular JSON, meaning this JSON or recursively any of its children would contain this JSON itself.
		/// </exception>
		/// <exception cref="JSONKeyNotFoundException">
		/// If parameter key doesn't exist in this JSON.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type.
		/// </exception>
		public void Replace(string key, object value) {
			if (key==null) {
				throw (new JArgumentNullException("key","'key' in JSON.Replace(string key, JValue value) method can not be null"+getExceptionMessageTail()));
			}
			if (!keyValuePairs.ContainsKey(key)) {
				throw (new JSONKeyNotFoundException("Key/value pair with the same key (\""+key+"\") does not exists in this JSON and can not be replaced."+getExceptionMessageTail()));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not replace value in this JSON, this JSON is set protected (read only)"+getExceptionMessageTail()));
			}
			if (value==this) {
				throw (new JArgumentException("Trying to set JSON value to itself. This would cause circular JSON.","value"+getExceptionMessageTail()));
			}
			JValue jValue=InternalTools.objectAsJValue(value);
			if (jValue==null) {
				throw (new UnknownObjectTypeException(value,"value"));
			}
			if (jValue is JSON) {
				if (((JSON)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JSON value which contains this JSON itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
				}
			}
			if (jValue is JArray) {
				if (((JArray)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JArray value which contains this JSON itself. This would cause circular JSON."+getExceptionMessageTail(),"value"));
				}
			}
			keyValuePairs[key] = jValue;
		}

		/// <summary>
		/// Remove value specified by key. This method throws exception if key doesn't exist.
		/// </summary>
		/// <param name="key">
		/// Key of the value to remove.
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter key is null.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JSON object is set protected (read only).
		/// </exception>
		/// <exception cref="JSONKeyNotFoundException">
		/// If parameter key doesn't exist in this JSON.
		/// </exception>
		public void Remove(string key) {
			if (key==null) {
				throw (new JArgumentNullException("key","'key' in JSON.Remove(string key) method can not be null"+getExceptionMessageTail()));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not remove value from this JSON, this JSON is set protected (read only)"+getExceptionMessageTail()));
			}
			if (!keyValuePairs.ContainsKey(key)) {
				throw (new JSONKeyNotFoundException("Value for key \""+key+"\" can't be removed since key does not exist in this JSON"+getExceptionMessageTail()));
			}
			keyValuePairs.Remove(key);
			keyOrder.Remove(key);
		}
		
		/// <summary>
		/// Remove all the key/value pairs from this JSON, leaving it empty.
		/// </summary>
		/// <exception cref="ProtectedException">
		/// If this JSON object is set protected (read only).
		/// </exception>
		public void Clear() {
			if (IsProtected()) {
				throw (new ProtectedException("Can not clear this JSON, this JSON is set protected (read only)"+getExceptionMessageTail()));
			}
			keyValuePairs.Clear();
			keyOrder.Clear();
		}

		// Check if key exists and throw exception if not. Throwing own exception for more detailed exception message (for example including requested key string).
		private void checkKeyExists(string key) {
			if (!keyValuePairs.ContainsKey(key)) {
				throw (new JSONKeyNotFoundException("Value for key \""+key+"\" does not exist in this JSON"+getExceptionMessageTail()));
			}
		}

		/// <summary>
		/// Gets the whole JSON object as system dictionary. This is recursive, so if this JSON contains other JSON objects or lists,
		/// those will be also changed to system objects.
		/// </summary>
		/// <returns>
		/// Dictionary that doesn't contain any TotalJSON objects on any level.
		/// </returns>
		public Dictionary<string, object> AsDictionary() {
			Dictionary<string, object> targetKeysAndValues = new Dictionary<string, object>();
			foreach (string key in keyValuePairs.Keys) {
				JValue jValue = keyValuePairs[key];
				object oValue = InternalTools.jValueAsSystemObject(jValue);
				targetKeysAndValues.Add(key, oValue);
			}
			return targetKeysAndValues;
		}

		/// <summary>
		/// Turns this JSON object to single JSON formatted string that can be easily stored or sent to another system.
		/// String always starts with character '{' and ends to character '}' and contains no linefeeds.
		/// </summary>
		/// <returns>
		/// This JSON object as JSON formatted string, containing only basic ascii characters between [32..126] without line feeds.
		/// </returns>
		public override string CreateString() {
			return base.CreateString();
		}

		/// <summary>
		/// Turns this JSON object to single JSON formatted string using specified settings.
		/// </summary>
		/// <returns>
		/// This JSON object as JSON formatted string.
		/// </returns>
		public override string CreateString(CreateStringSettings settings) {
			return base.CreateString(settings);
		}

		/// <summary>
		/// Turns this JSON object to JSON formatted, but easily human readable string.
		/// This output is not as tightly packed as string returned by CreateString() but contains all the same data and is still completely valid JSON.
		/// </summary>
		/// <remarks>
		/// This method is just a shortcut for doing:
		/// <code>
		/// CreateStringSettings createStringSettings = new CreateStringSettings();
		/// createStringSettings.HumanReadable = true;
		/// string output = CreateString(createStringSettings);
		/// </code>
		/// </remarks>
		/// <returns>
		/// This JSON object as JSON formatted string, containing only basic ascii characters between [32..126], line feeds and tabs.
		/// </returns>
		public string CreatePrettyString() {
			CreateStringSettings createStringSettings = new CreateStringSettings();
			createStringSettings.HumanReadable = true;
			return CreateString(createStringSettings);
		}

		internal override void zCreate(CreateStringRunner createStringRunner) {
			if (keyValuePairs.Count!=keyOrder.Count) {
				Debug.LogError("Leguar.TotalJSON: JSON.zCreate(): Internal error: 'keyValuePairs.Count' ("+keyValuePairs.Count+") is no equal to 'keyOrder.Count' ("+keyOrder.Count+")");
			}
			if (keyValuePairs.Count>0) {
				createStringRunner.append('{',1);
				bool first = true;
				foreach (string key in keyOrder) {
					if (!first) {
						createStringRunner.append(',',0);
					} else {
						first = false;
					}
					JString.encode(createStringRunner,key);
					createStringRunner.append(':',true);
					keyValuePairs[key].zCreate(createStringRunner);
				}
				createStringRunner.append(-1,'}');
			} else {
				createStringRunner.append('{','}');
			}
		}

		/// <summary>
		/// Turns JSON formatted string to new JSON object.
		/// </summary>
		/// <param name="jsonObjectAsSingleString">
		/// Source JSON string to turn to JSON object.
		/// </param>
		/// <returns>
		/// New JSON object, never null.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JSON.
		/// </exception>
		public static JSON ParseString(string jsonObjectAsSingleString) {
			if (jsonObjectAsSingleString==null) {
				throw (new JArgumentNullException("jsonObjectAsSingleString","Source string can not be null"));
			}
			return innerParseString(jsonObjectAsSingleString,null);
		}

		/// <summary>
		/// Turns JSON formatted string to new JSON object. Debug ID is added to any possible exceptions that may occur during parse or
		/// when handling resulting JSON object afterwards.
		/// </summary>
		/// <remarks>
		/// This method is for convenience. This method will create new ParseStringSettings object and set value for DebugIDForExceptions,
		/// then call <code>ParseString(string jsonObjectAsSingleString, ParseStringSettings parseStringSettings)</code> method. See
		/// ParseStringSettings class for more information.
		/// </remarks>
		/// <param name="jsonObjectAsSingleString">
		/// Source JSON string to turn to JSON object.
		/// </param>
		/// <param name="debugIDForExceptions">
		/// Debug ID that will be added to any possible exception message. This value have no effect to resulting JSON object.
		/// </param>
		/// <returns>
		/// New JSON object, never null.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter JSON string is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JSON.
		/// </exception>
		public static JSON ParseString(string jsonObjectAsSingleString, string debugIDForExceptions) {
			if (jsonObjectAsSingleString==null) {
				throw (new JArgumentNullException("jsonObjectAsSingleString","Source string can not be null"+ParseException.getExceptionMessageTailForID(debugIDForExceptions)));
			}
			ParseStringSettings parseStringSettings=new ParseStringSettings();
			parseStringSettings.DebugIDForExceptions = debugIDForExceptions;
			return innerParseString(jsonObjectAsSingleString,parseStringSettings);
		}

		/// <summary>
		/// Turns JSON formatted string to new JSON object, using specified settings.
		/// </summary>
		/// <param name="jsonObjectAsSingleString">
		/// Source JSON string where to read.
		/// </param>
		/// <param name="parseStringSettings">
		/// Settings to be used when parsing the string.
		/// </param>
		/// <returns>
		/// New JSON object, never null.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string or settings object is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JSON.
		/// </exception>
		public static JSON ParseString(string jsonObjectAsSingleString, ParseStringSettings parseStringSettings) {
			if (jsonObjectAsSingleString==null) {
				throw (new JArgumentNullException("jsonObjectAsSingleString","Source string can not be null"+ParseException.getExceptionMessageTail(parseStringSettings)));
			}
			if (parseStringSettings==null) {
				throw (new JArgumentNullException("parseStringSettings","Settings can not be null in JSON.ParseString(string,ParseStringSettings)"+ParseException.getExceptionMessageTail(parseStringSettings)));
			}
			return innerParseString(jsonObjectAsSingleString,parseStringSettings);
		}

		private static JSON innerParseString(string jsonObjectAsSingleString, ParseStringSettings parseStringSettings) {
			int length = jsonObjectAsSingleString.Length;
			if (length==0) {
				throw ParseException.forEmpty("Parameter string 'jsonObjectAsSingleString' is empty (length 0)",parseStringSettings);
			}
			ParseStringRunner parseStringRunner = new ParseStringRunner(jsonObjectAsSingleString,parseStringSettings);
			if (!parseStringRunner.containsNonWhiteChars()) {
				throw ParseException.forEmpty("Parameter string 'jsonObjectAsSingleString' is not empty (length "+length+") but doesn't contain any non-white characters",parseStringSettings);
			}
			JSON json=zParse(parseStringRunner,true,false,false);
			if (!parseStringRunner.isAllowNonWhiteCharactersAfterObject()) {
				char chr;
				StringPointer sp = parseStringRunner.getStringPointer();
				if (sp.tryGetNextNonWhiteChar(out chr)) {
					throw ParseException.forCharactersAfterEnd(parseStringRunner);
				}
			}
			json.SetDebugIDForExceptions(parseStringRunner.getParseDebugIDForExceptions()); // May be null
			return json;
		}

		/// <summary>
		/// Turns single string to multiple new JSON objects. Source string may contain any amount of JSON objects, including zero.
		/// </summary>
		/// <remarks>
		/// Acceptable input string are for example:<br>
		/// '' (empty string, returns JSON array with length of 0)<br>
		/// '{"hello":"world"}' (returns JSON array with length of 1, normal ParseString method could be used to this one too)<br>
		/// '{}{} {}' (returns JSON array of length 3, all JSON objects in array are empty)
		/// </remarks>
		/// <param name="multipleJsonObjectsAsSingleString">
		/// Source string where to read JSON objects.
		/// </param>
		/// <returns>
		/// Array of JSON objects. Array may be also empty or contain just one item. Returned value is never null.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string object is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JSONs.
		/// </exception>
		public static JSON[] ParseStringToMultiple(string multipleJsonObjectsAsSingleString) {
			if (multipleJsonObjectsAsSingleString==null) {
				throw (new JArgumentNullException("multipleJsonObjectsAsSingleString","Source string to parse can not be null in JSON.ParseStringToMultiple(string)"));
			}
			return innerParseStringToMultiple(multipleJsonObjectsAsSingleString,null);
		}

		/// <summary>
		/// Turns single string to multiple new JSON objects, using specified settings. Source string may contain any amount of JSON objects, including zero.
		/// </summary>
		/// <param name="multipleJsonObjectsAsSingleString">
		/// Source string where to read JSON objects.
		/// </param>
		/// <param name="parseStringSettings">
		/// Settings to be used when parsing the string.
		/// </param>
		/// <returns>
		/// Array of JSON objects. Array may be also empty or contain just one item. Returned value is never null.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string or settings object is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JSONs.
		/// </exception>
		public static JSON[] ParseStringToMultiple(string multipleJsonObjectsAsSingleString, ParseStringSettings parseStringSettings) {
			if (multipleJsonObjectsAsSingleString==null) {
				throw (new JArgumentNullException("multipleJsonObjectsAsSingleString","Source string to parse can not be null in JSON.ParseStringToMultiple(string,ParseStringSettings)"));
			}
			if (parseStringSettings==null) {
				throw (new JArgumentNullException("parseStringSettings","Settings object can not be null in JSON.ParseStringToMultiple(string,ParseStringSettings)"));
			}
			return innerParseStringToMultiple(multipleJsonObjectsAsSingleString,parseStringSettings);
		}

		private static JSON[] innerParseStringToMultiple(string multipleJsonObjectsAsSingleString, ParseStringSettings parseStringSettings) {
			ParseStringRunner runner = new ParseStringRunner(multipleJsonObjectsAsSingleString,parseStringSettings);
			bool allowTrash = runner.isAllowNonWhiteCharactersAfterObject();
			List<JSON> lObjects=new List<JSON>();
			do {
				JSON lObject=zParse(runner,true,true,allowTrash);
				if (lObject!=null && runner.getParseDebugIDForExceptions()!=null) {
					lObject.SetDebugIDForExceptions(runner.getParseDebugIDForExceptions()+" ["+lObjects.Count+"]");
				}
				if (lObject==null) {
					return lObjects.ToArray();
				}
				lObjects.Add(lObject);
			} while (true);
		}

		internal static JSON zParse(ParseStringRunner parseStringRunner, bool expectStartOfObject, bool allowEndOfString, bool allowTrashInsteadObjectStart) {

			StringPointer sp = parseStringRunner.getStringPointer();

			char chr;
			if (expectStartOfObject) {
				if (!sp.tryGetNextNonWhiteChar(out chr)) {
					if (allowEndOfString) {
						return null; // Acceptable, but no object was parsed
					}
					throw ParseException.forInvalidStart("Parameter string didn't contain start of object '{'",parseStringRunner);
				}
				if (chr!='{') {
					if (allowTrashInsteadObjectStart) {
						return null; // Acceptable, but no object was parsed
					}
					throw ParseException.forInvalidStart("Invalid character '"+chr+"' when expecting start of object '{'",parseStringRunner);
				}
			}

			JSON lObject=new JSON();
			chr=sp.getNextNonWhiteChar();
			if (chr=='}') {
				return lObject; // All done, object was empty
			}

			string keyString=null;

			do {
				if (chr!='"') {
					throw ParseException.forInvalidCharacter("Invalid character '"+chr+"' when expecting start of key string '\"'",parseStringRunner);
				}
				keyString=JString.decode(parseStringRunner);
				chr=sp.getNextNonWhiteChar();
				if (chr!=':') {
					throw ParseException.forInvalidCharacter("Invalid character '"+chr+"' when expecting key/value separator ':'",parseStringRunner);
				}
				lObject.Add(keyString,parseStringRunner.parseValue());
				chr=sp.getNextNonWhiteChar();
				if (chr=='}') {
					return lObject; // All done, object had at least one key/value pair
				}
				if (chr!=',') {
					throw ParseException.forInvalidCharacter("Invalid character '"+chr+"' when expecting pair separator ',' or end of object '}'",parseStringRunner);
				}
				chr=sp.getNextNonWhiteChar();
			} while (true);

		}

		/// <summary>
		/// Serialize object to JSON object, using default settings.
		/// 
		/// If parameter object is class or struct, only public fields or fields marked with [SerializeField] are serialized.
		/// </summary>
		/// <param name="objectToSerialize">
		/// Object that can be serialized to JSON without ambiguity. Typically class, struct or dictionary.
		/// </param>
		/// <returns>
		/// JSON object containing fields/values from parameter object.
		/// </returns>
		/// <exception cref="SerializeException">
		/// If any exceptions occurs when trying to serialize object to new JSON.
		/// </exception>
		public static JSON Serialize(object objectToSerialize) {
			return Serialize(objectToSerialize, new SerializeSettings());
		}

		/// <summary>
		/// Serialize object to JSON object, using specific settings.
		/// 
		/// If parameter object is class or struct, only public fields or fields marked with [SerializeField] are serialized.
		/// </summary>
		/// <param name="objectToSerialize">
		/// Object that can be serialized to JSON without ambiguity. Typically class, struct or dictionary.
		/// </param>
		/// <param name="serializeSettings">
		/// Specified settings used for serialization.
		/// </param>
		/// <returns>
		/// JSON object containing fields/values from parameter object.
		/// </returns>
		/// <exception cref="SerializeException">
		/// If any exceptions occurs when trying to serialize object to new JSON.
		/// </exception>
		public static JSON Serialize(object objectToSerialize, SerializeSettings serializeSettings) {
			JValue jValue = InternalTools.serializeObject(objectToSerialize, serializeSettings);
			if (jValue==null) {
				throw (new SerializeException("Parameter object is type that can't be serialized", objectToSerialize, "objectToSerialize"));
			}
			if (!(jValue is JSON)) {
				throw (new SerializeException("Parameter object can be serialized but not to JSON", objectToSerialize, "objectToSerialize"));
			}
			return ((JSON)(jValue));
		}

		/// <summary>
		/// Deserialize this JSON to object, using default settings.
		/// 
		/// Typically this is used as opposite operation for Serialize. After Serialize is used to turn object to JSON, this method can be used to turn JSON back to object of same type.
		/// </summary>
		/// <typeparam name="T">
		/// Type of object that can hold all the content of this JSON.
		/// </typeparam>
		/// <returns>
		/// Object of wanted type.
		/// </returns>
		/// <exception cref="DeserializeException">
		/// If any exceptions occurs when trying to deserialize this JSON to object.
		/// </exception>
		public T Deserialize<T>() {
			return Deserialize<T>(new DeserializeSettings());
		}

		/// <summary>
		/// Deserialize this JSON to object, using specified settings.
		/// 
		/// Typically this is used as opposite operation for Serialize. After Serialize is used to turn object to JSON, this method can be used to turn JSON back to object of same type.
		/// </summary>
		/// <typeparam name="T">
		/// Type of object that can hold all the content of this JSON.
		/// </typeparam>
		/// <param name="deserializeSettings">
		/// Specified settings used for deserialization.
		/// </param>
		/// <returns>
		/// Object of wanted type.
		/// </returns>
		/// <exception cref="DeserializeException">
		/// If any exceptions occurs when trying to deserialize this JSON to object.
		/// </exception>
		public T Deserialize<T>(DeserializeSettings deserializeSettings) {
			object obj = zDeserialize(typeof(T), null, deserializeSettings);
			return (T)(obj);
		}

		internal override object zDeserialize(Type type, string toFieldName, DeserializeSettings deserializeSettings) {

			if (type.IsGenericType) {
				if (type.GetGenericTypeDefinition()==typeof(Dictionary<,>)) {
					Type[] dictTypes = type.GetGenericArguments();
					bool keyStringType = (dictTypes[0]==typeof(string) || dictTypes[0]==typeof(object));
					if (!deserializeSettings.AllowNonStringDictionaryKeys && !keyStringType) {
						throw (DeserializeException.forDictionaryKeyTypeNotString(dictTypes[0],toFieldName));
					}
					bool keyIntType = (dictTypes[0]==typeof(int));
					bool keyLongType = (dictTypes[0]==typeof(long));
					if (!keyStringType && !keyIntType && !keyLongType) {
						throw (DeserializeException.forDictionaryKeyTypeNotKnown(dictTypes[0],toFieldName));
					}
					object objTypeOfDict = Activator.CreateInstance(type);
					IDictionary objDict = (IDictionary)(objTypeOfDict);
					foreach (string stringKey in keyValuePairs.Keys) {
						JValue jValue = keyValuePairs[stringKey];
						object sValue = jValue.zDeserialize(dictTypes[1], toFieldName, deserializeSettings);
						if (keyIntType) {
							objDict.Add(int.Parse(stringKey), sValue);
						} else if (keyLongType) {
							objDict.Add(long.Parse(stringKey), sValue);
						} else {
							objDict.Add(stringKey, sValue);
						}
					}
					return objTypeOfDict;
				}
			}

			if (type==typeof(object) && deserializeSettings.AllowFieldsToBeObjects) {
				return this.AsDictionary();
			}

			object objTypeOfCustom = Activator.CreateInstance(type);

			int jsonValuesUsed = 0;
			FieldInfo[] fieldInfos = objTypeOfCustom.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fieldInfos) {
				if (InternalTools.isSerializing(fieldInfo, deserializeSettings.IgnoreSystemAndUnitySerializeAttributes)) {
					string fieldName = fieldInfo.Name;
					if (this.ContainsKey(fieldName)) {
						JValue jValue = this[fieldName];
						object sValue = jValue.zDeserialize(fieldInfo.FieldType, fieldName, deserializeSettings);
						fieldInfo.SetValue(objTypeOfCustom, sValue);
						jsonValuesUsed++;
					} else if (deserializeSettings.RequireAllFieldsArePopulated) {
						throw (DeserializeException.forNoMatchingField(fieldName, type));
					}
				}
			}
			if (deserializeSettings.RequireAllJSONValuesAreUsed && jsonValuesUsed<this.Count) {
				throw (DeserializeException.forNoMatchingValue(type));
			}

			return objTypeOfCustom;

		}

		/// <summary>
		/// Sets this JSON and all its childen protected (read only). New values can't be added and old values replaced or removed.
		/// </summary>
		/// <remarks>
		/// Typically this is called only for top level JSON. Protecetion can't be cancelled, once it is set, it will stay.
		/// </remarks>
		public void SetProtected() {
			thisProtected = true;
			foreach (JValue value in keyValuePairs.Values) {
				if (value is JSON) {
					((JSON)(value)).SetProtected();
				} else if (value is JArray) {
					((JArray)(value)).SetProtected();
				}
			}
		}

		/// <summary>
		/// Check whatever this JSON is protected (read only). If it is, it also means that all its childen are protected, but nothing can be assumed of parents of this object.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this object is protected, <c>false</c> otherwise
		/// </returns>
		public bool IsProtected() {
			return thisProtected;
		}

		/// <summary>
		/// Adds this JSON object to editor debug window so content of JSON object can be followed in Unity editor when application is running.
		/// Choose "Window" -> "Total JSON" -> "JSON Runtime Debug" from Unity editor menu top open this window.
		/// 
		/// Calling this method outside Unity editor doesn't do anything.
		/// </summary>
		/// <param name="debugName">
		/// Name of this object in debug window.
		/// </param>
		public void DebugInEditor(string debugName) {
#if UNITY_EDITOR
			JSONRuntimeDebugContainer jsonRuntimeDebugContainer = InternalTools.getDebugContainer();
//			string previousKey=jsonRuntimeDebugContainer.add(debugName,this);
//			if (previousKey!=null) {
//				Debug.LogWarning("TotalJSON.JSON.DebugInEditor(\""+debugName+"\"): This JSON object have been already added to editor debug with name \""+previousKey+"\"");
//			}
			jsonRuntimeDebugContainer.add(debugName, this);
#endif
		}

		/// <summary>
		/// Sets debug ID for this JSON object. This ID is added to any possible exception messages that may occur when handling this JSON.
		/// 
		/// This is typically useful in production builds where only exception message is logged but full stacktrace may not be available.
		/// Settings this debug ID helps to identify which JSON object caused exception.
		/// 
		/// Note that if this JSON object was parsed from string and ParseStringSettings.DebugIDForExceptions for already set during
		/// parse, that same ID is copied to this JSON object.
		/// </summary>
		/// <param name="debugIdForExceptions">
		/// Debug ID to add to exception messages. Setting this null will disable this feature.
		/// </param>
		public void SetDebugIDForExceptions(string debugIdForExceptions) {
			this.debugIdForExceptions = debugIdForExceptions;
		}

		private string getExceptionMessageTail() {
			return InternalTools.getExceptionMessageTailForID(debugIdForExceptions,"JSON");
		}

	}

}
