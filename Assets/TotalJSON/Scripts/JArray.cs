//  JValue - JArray


using System;
using System.Collections;
using System.Collections.Generic;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// JSON array that may contain any number of JValue objects, including zero (empty array). Objects in array stay in order they are added in to the array.
	/// New values can be added, removed and replaced freely.
	/// </summary>
	public class JArray : JValue {

		private List<JValue> values;

		private bool thisProtected;

		/// <summary>
		/// Creates a new empty instance of JArray.
		/// </summary>
		public JArray() : base() {
			values = new List<JValue>();
			thisProtected = false;
		}

		/// <summary>
		/// Creates new JArray object from system list or array of objects. Note that values in dictionary need to be basic c# objects like
		/// string, booleans, numbers, or another lists or dictionaries. To turn other classes to JSON object, or for more flexible options,
		/// consider using static JArray.Serialize() method.
		/// </summary>
		/// <param name="sourceValues">
		/// Source values. Each element have to be either JValue, or any basic c# object that can be changed to JValue.
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter is null.
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If any exceptions occurs when trying to turn parameter list to new JArray object.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If any value in parameter array/list is unsupported type.
		/// </exception>
		public JArray(IList sourceValues) : this() {
			if (sourceValues==null) {
				throw (new JArgumentNullException("sourceValues","Parameter in constructor JArray.<init>(IList) can not be null"));
			}
			InternalTools.listToJArray(sourceValues, this, new List<object>());
		}

		public override string ToString() {
			if (values.Count == 0) {
				return ("[JArray: Empty array]");
			}
			Type allType = values[0].GetType();
			if (values.Count == 1) {
				return ("[JArray: Array length = 1, element type of "+allType.ToString()+"]");
			}
			for (int n=1; n<values.Count; n++) {
				if (values[n].GetType() != allType) {
					return ("[JArray: Array length = " + values.Count + ", mixed element types]");
				}
			}
			return ("[JArray: Array length = "+values.Count+", all elements type of "+allType.ToString()+"]");
		}

		/// <summary>
		/// Test if another object equals to this object. Always returns false if parameter object is null or it is not instance of JArray.
		/// Two JArray objects are equal if both contains same amount of elements and every element in same indexes equals to each other.
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
			if (!(anotherObject is JArray)) {
				return false;
			}
			JArray anotherJArray=(JArray)(anotherObject);
			if (anotherJArray.Length!=this.Length) {
				return false;
			}
			for (int n=0; n<this.Length; n++) {
				if (!values[n].Equals(anotherJArray[n])) {
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode() {
			int hashCode=103;
			foreach (JValue value in values) {
				hashCode^=value.GetHashCode();
			}
			return hashCode;
		}

		/// <summary>
		/// Gets the length of this array.
		/// </summary>
		/// <value>
		/// Array length.
		/// </value>
		public int Length {
			get {
				return values.Count;
			}
		}

		/// <summary>
		/// Gets or sets (replaces) the value specified by index.
		/// </summary>
		/// <param name="index">
		/// Index of the value to get or set.
		/// </param>
		/// <value>
		/// Any JValue object, can not be null but can be JNull.
		/// </value>
		public JValue this[int index] {
			get {
				return Get(index);
			}
			set {
				ReplaceAt(index,value);
			}
		}

		/// <summary>
		/// Get value from specific index in this array.
		/// </summary>
		/// <param name="index">
		/// Index of element to get.
		/// </param>
		/// <returns>
		/// JValue object from index. Returned value is never null, but it can be JNull.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		public JValue Get(int index) {
			if (index<0) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be negative when getting value from JArray (index = "+index+")"));
			}
			if (index>=values.Count) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be outside array length when getting value from JArray (index = "+index+", array length = "+values.Count+")"));
			}
			return values[index];
		}

		/// <summary>
		/// Gets JSON value from specific index in this array. This method will cause exception if value is anything else than JSON or JNull.
		/// </summary>
		/// <param name="index">
		/// Array index where to get JSON value.
		/// </param>
		/// <returns>
		/// JSON or null if value at index is JNull.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JSON or JNull.
		/// </exception>
		public JSON GetJSON(int index) {
			JValue jValue=Get(index);
			if (jValue is JSON) {
				return ((JSON)(jValue));
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(index,jValue,"JSON"));
		}

		/// <summary>
		/// Gets JNumber value from specific index in this array. This method will cause exception if value is anything else than JNumber or JNull.
		/// </summary>
		/// <param name="index">
		/// Array index where to get JNumber value.
		/// </param>
		/// <returns>
		/// JNumber or null if value at index is JNull.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JNumber or JNull.
		/// </exception>
		public JNumber GetJNumber(int index) {
			JValue jValue=Get(index);
			if (jValue is JNumber) {
				return ((JNumber)(jValue));
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(index,jValue,"JNumber"));
		}

		/// <summary>
		/// Gets integer number from specific index in this array. This method will cause exception if key/value pair doesn't exist, value is not number or it is outside int range.
		/// </summary>
		/// <param name="index">
		/// Array index where to get int value.
		/// </param>
		/// <returns>
		/// C# int value.
		/// </returns>
		/// <remarks>
		/// This method is for convenience. It is equal to <code>jArray.GetJNumber(index).AsInt();</code>
		/// If there is need to get number value from JArray in some very specific format, use <code>GetJNumber(index)</code> and
		/// then methods provided by JNumber class, for example <code>jArray.GetJNumber(index).AsULong();</code>
		/// </remarks>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueNullException">
		/// If value mapped to this key is null.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JNumber.
		/// </exception>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in int.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public int GetInt(int index) {
			JNumber jNumber=GetJNumber(index);
			if (jNumber==null) {
				throw (new JValueNullException(index,"int"));
			}
			return jNumber.AsInt();
		}

		/// <summary>
		/// Gets floating point number from specific index in this array. This method will cause exception if key/value pair doesn't exist, value is not number or it is outside float range.
		/// </summary>
		/// <param name="index">
		/// Array index where to get float value.
		/// </param>
		/// <returns>
		/// C# float value.
		/// </returns>
		/// <remarks>
		/// This method is for convenience. It is equal to <code>jArray.GetJNumber(index).AsFloat();</code>
		/// If there is need to get number value from JArray in some very specific format, use <code>GetJNumber(index)</code> and
		/// then use methods provided by JNumber class, for example <code>jArray.GetJNumber(index).AsDecimal();</code>
		/// </remarks>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueNullException">
		/// If value mapped to this key is null.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JNumber.
		/// </exception>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in float.
		/// </exception>
		public float GetFloat(int index) {
			JNumber jNumber=GetJNumber(index);
			if (jNumber==null) {
				throw (new JValueNullException(index,"float"));
			}
			return jNumber.AsFloat();
		}

		/// <summary>
		/// Gets C# string value from specific index in this array. This method will cause exception if value is anything else than JString or JNull.
		/// </summary>
		/// <param name="index">
		/// Array index where to get string value.
		/// </param>
		/// <returns>
		/// C# string or null if value at index is JNull.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JString or JNull.
		/// </exception>
		public string GetString(int index) {
			JValue jValue=Get(index);
			if (jValue is JString) {
				return ((JString)(jValue)).AsString();
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(index,jValue,"string"));
		}

		/// <summary>
		/// Gets C# bool value from specific index in this array. This method will cause exception if value is anything else than JBoolean.
		/// </summary>
		/// <param name="index">
		/// Array index where to get bool value.
		/// </param>
		/// <returns>
		/// C# bool value.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JBoolean.
		/// </exception>
		public bool GetBool(int index) {
			JValue jValue=Get(index);
			if (jValue is JBoolean) {
				return ((JBoolean)(jValue)).AsBool();
			}
			throw (new JValueTypeException(index,jValue,"bool"));
		}

		/// <summary>
		/// Gets JArray value from specific index in this array. This method will cause exception if value is anything else than JArray or JNull.
		/// </summary>
		/// <param name="index">
		/// Array index where to get JArray value.
		/// </param>
		/// <returns>
		/// JArray or null if value at index is JNull.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="JValueTypeException">
		/// If value in index is not JArray or JNull.
		/// </exception>
		public JArray GetJArray(int index) {
			JValue jValue=Get(index);
			if (jValue is JArray) {
				return ((JArray)(jValue));
			}
			if (jValue is JNull) {
				return null;
			}
			throw (new JValueTypeException(index,jValue,"JArray"));
		}

		/// <summary>
		/// Check if value in specified index is JNull.
		/// </summary>
		/// <param name="index">
		/// Array index to check.
		/// </param>
		/// <returns>
		/// <c>true</c> value in index is JNull, <c>false</c> otherwise.
		/// </returns>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than equal to this JArray length.
		/// </exception>
		public bool IsJNull(int index) {
			if (index<0) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be negative when testing array value for being JNull"));
			}
			if (index>=values.Count) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be outside array length when testing array value for being JNull"));
			}
			return (values[index] is JNull);
		}

		/// <summary>
		/// Add new value to end of this array.
		/// </summary>
		/// <param name="value">
		/// Value to add. Either any JValue, or any basic c# object that can be changed to JValue (string, bool, numbers and null).
		/// </param>
		/// <exception cref="ProtectedException">
		/// If this JArray object is set protected (read only).
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If adding parameter value to this JArray would cause circular JSON, meaning this JArray or recursively any of its children would contain this JArray itself.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type.
		/// </exception>
		public void Add(object value) {
			if (IsProtected()) {
				throw (new ProtectedException("Can not add new value in this JArray, this JArray is set protected (read only)"));
			}
			if (value==this) {
				throw (new JArgumentException("Trying to add JArray value to itself. This would cause circular JSON.","value"));
			}
			JValue jValue=InternalTools.objectAsJValue(value);
			if (jValue==null) {
				throw (new UnknownObjectTypeException(value,"value"));
			}
			if (jValue is JSON) {
				if (((JSON)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to add JSON value which contains this JArray itself. This would cause circular JSON.","value"));
				}
			}
			if (jValue is JArray) {
				if (((JArray)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to add JArray value which contains this JArray itself. This would cause circular JSON.","value"));
				}
			}
			values.Add(jValue);
		}

		/// <summary>
		/// Replace value to specific index in this array. Array length won't change.
		/// This can only replace existing values and can not be used to add new values to array. Trying to set value outside array size will cause exception.
		/// </summary>
		/// <param name="index">
		/// Index in array where value should be replaced.
		/// </param>
		/// <param name="value">
		/// Value to set. Either any JValue, or any basic c# object that can be changed to JValue (string, bool, numbers and null).
		/// </param>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JArray object is set protected (read only).
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If adding parameter value to this JArray would cause circular JSON, meaning this JArray or recursively any of its children would contain this JArray itself.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type.
		/// </exception>
		public void ReplaceAt(int index, object value) {
			if (index<0) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be negative in JArray.ReplaceAt(int index, JValue value)"));
			}
			if (index>=values.Count) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be outside array length in JArray.ReplaceAt(int index, JValue value)"));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not set value to this JArray, this JArray is set protected (read only)"));
			}
			if (value==this) {
				throw (new JArgumentException("Trying to set JArray value to itself. This would cause circular JSON.","value"));
			}
			JValue jValue=InternalTools.objectAsJValue(value);
			if (jValue==null) {
				throw (new UnknownObjectTypeException(value,"value"));
			}
			if (jValue is JSON) {
				if (((JSON)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JSON value which contains this JArray itself. This would cause circular JSON.","value"));
				}
			}
			if (jValue is JArray) {
				if (((JArray)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JArray value which contains this JArray itself. This would cause circular JSON.","value"));
				}
			}
			values[index]=jValue;
		}

		/// <summary>
		/// Insert new value to specific index. Just like when using Add method, nothing is replaced and array length will be one more after this.
		/// Index may be equal to current array length, in that case this method works exactly as Add(JValue)
		/// </summary>
		/// <param name="index">
		/// Index in array where to insert new value.
		/// </param>
		/// <param name="value">
		/// Value to insert. Value can't be null but it can be JNull.
		/// </param>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than this JArray length.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JArray object is set protected (read only).
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If adding parameter value to this JArray would cause circular JSON, meaning this JArray or recursively any of its children would contain this JArray itself.
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type.
		/// </exception>
		public void InsertAt(int index, object value) {
			if (index<0) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be negative in JArray.InsertAt(int index, JValue value)"));
			}
			if (index>values.Count) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be more than array length in JArray.InsertAt(int index, JValue value)"));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not insert new value in this JArray, this JArray is set protected (read only)"));
			}
			if (value==this) {
				throw (new JArgumentException("Trying to set JArray value to itself. This would cause circular JSON.","value"));
			}
			JValue jValue=InternalTools.objectAsJValue(value);
			if (jValue==null) {
				throw (new UnknownObjectTypeException(value,"value"));
			}
			if (jValue is JSON) {
				if (((JSON)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JSON value which contains this JArray itself. This would cause circular JSON.","value"));
				}
			}
			if (jValue is JArray) {
				if (((JArray)(jValue)).deepContainsObject(this)) {
					throw (new JArgumentException("Trying to set JArray value which contains this JArray itself. This would cause circular JSON.","value"));
				}
			}
			values.Insert(index,jValue);
		}

		/// <summary>
		/// Remove value from specific index. After this array length will be one less than before.
		/// </summary>
		/// <param name="index">
		/// Index in array where to remove the value.
		/// </param>
		/// <exception cref="JArrayIndexOutOfRangeException">
		/// If parameter index is negative, or more than or equal to this JArray length.
		/// </exception>
		/// <exception cref="ProtectedException">
		/// If this JArray object is set protected (read only).
		/// </exception>
		public void RemoveAt(int index) {
			if (index<0) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be negative in JArray.RemoveAt(int index)"));
			}
			if (index>=values.Count) {
				throw (new JArrayIndexOutOfRangeException("index",index,"'index' can not be outside array length in JArray.RemoveAt(int index)"));
			}
			if (IsProtected()) {
				throw (new ProtectedException("Can not remove value from this JArray, this JArray is set protected (read only)"));
			}
			values.RemoveAt(index);
		}

		/// <summary>
		/// Removes first instance of value that equals to parameter value.
		/// </summary>
		/// <param name="valueToRemove">
		/// Value to be removed.
		/// </param>
		/// <exception cref="ProtectedException">
		/// If this JArray object is set protected (read only).
		/// </exception>
		/// <exception cref="UnknownObjectTypeException">
		/// If parameter value is unsupported type and therefore can not exist in this JArray.
		/// </exception>
		/// <exception cref="JArgumentException">
		/// If can not found any value matching the parameter.
		/// </exception>
		public void RemoveValue(object valueToRemove) {
			if (IsProtected()) {
				throw (new ProtectedException("Can not remove value from this JArray, this JArray is set protected (read only)"));
			}
			JValue jValueToRemove = InternalTools.objectAsJValue(valueToRemove);
			if (jValueToRemove==null) {
				throw (new UnknownObjectTypeException(valueToRemove,"valueToRemove"));
			}
			for (int n=0; n<values.Count; n++) {
				if (values[n].Equals(jValueToRemove)) {
					values.RemoveAt(n);
					return;
				}
			}
			throw (new JArgumentException("Can not find value to remove from this JArray","valueToRemove"));
		}

		/// <summary>
		/// Remove all the values from this JArray, leaving it empty.
		/// </summary>
		/// <exception cref="ProtectedException">
		/// If this JArray object is set protected (read only).
		/// </exception>
		public void Clear() {
			if (IsProtected()) {
				throw (new ProtectedException("Can not clear this JArray, this JArray is set protected (read only)"));
			}
			values.Clear();
		}

        /// <summary>
        /// Check whatever this JArray object contains specified value.
        /// </summary>
        /// <param name="valueToSearch">
        /// Value to look for.
        /// </param>
        /// <exception cref="UnknownObjectTypeException">
        /// If parameter value is unsupported type and therefore can not exist in this JArray.
        /// </exception>
        /// <returns>
        /// <c>true</c> if this JArray contains 'valueToSearch', <c>false</c> otherwise.
        /// </returns>
        public bool ContainsValue(object valueToSearch) {
			JValue jValueToSearch = InternalTools.objectAsJValue(valueToSearch);
			if (jValueToSearch==null) {
				throw (new UnknownObjectTypeException(valueToSearch,"valueToSearch"));
			}
			foreach (JValue value in values) {
				if (value.Equals(jValueToSearch)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Check whatever this JArray object or recursively any of child objects contains specified object.
		/// </summary>
		/// <param name="obj">
		/// Object to look for.
		/// </param>
		/// <returns>
		/// <c>true</c> if this exact 'obj' object exists somewhere in JSON tree hierarchy starting from this object, <c>false</c> otherwise.
		/// </returns>
		internal bool deepContainsObject(JValue obj) {
			foreach (JValue value in values) {
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
		/// Check whatever this JArray object is empty (length is 0).
		/// </summary>
		/// <returns>
		/// <c>true</c> if this array is empty, <c>false</c> otherwise.
		/// </returns>
		public bool IsEmpty() {
			return (values.Count==0);
		}

		/// <summary>
		/// Gets all the values in this JArray object as system array.
		/// You can use this to loop through all the values in this JArray, for example:
		/// <code>foreach (JValue value in jarray.Values) {...}</code>
		/// 
		/// Array of returned values is shallow copy of internal array of this JArray object.
		/// This means you can add or remove values from this JArray while looping through the values.
		/// For example, removing all the number elements from the JArray:
		/// <code>
		/// foreach (JValue value in jarray.Values) {
		///     if (value is JNumber) {
		///         jarray.RemoveValue(value);
		///     }
		/// }
		/// </code>
		/// </summary>
		/// <value>
		/// All the values in this JArray object.
		/// </value>
		public JValue[] Values {
			get {
				int count=values.Count;
				JValue[] array=new JValue[count];
				for (int n=0; n<count; n++) {
					array[n]=values[n];
				}
				return array;
			}
		}

		/// <summary>
		/// Get this array as JSON array. All the values in this array must be JSON or JNull objects.
		/// </summary>
		/// <returns>
		/// Array of JSON elements. May contains null elements, but returning array itself can not be null.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than JSON object or null.
		/// </exception>
		public JSON[] AsJSONArray() {
			int count=values.Count;
			JSON[] array=new JSON[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JSON) {
					array[n]=(JSON)(values[n]);
				} else if (values[n] is JNull) {
					array[n]=null;
				} else {
					throw (new JValueTypeException(n,values[n],"JSON"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as integer array. All the values in this array must be numbers inside integer range.
		/// </summary>
		/// <returns>
		/// Array of integers.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than number.
		/// </exception>
		public int[] AsIntArray() {
			int count=values.Count;
			int[] array=new int[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JNumber) {
					array[n]=((JNumber)(values[n])).AsInt();
				} else {
					throw (new JValueTypeException(n,values[n],"JNumber"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as long array. All the values in this array must be numbers inside long range.
		/// </summary>
		/// <returns>
		/// Array of longs.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than number.
		/// </exception>
		public long[] AsLongArray() {
			int count=values.Count;
			long[] array=new long[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JNumber) {
					array[n]=((JNumber)(values[n])).AsLong();
				} else {
					throw (new JValueTypeException(n,values[n],"JNumber"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as float array. All the values in this array must be numbers inside float range.
		/// </summary>
		/// <returns>
		/// Array of floats.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than number.
		/// </exception>
		public float[] AsFloatArray() {
			int count=values.Count;
			float[] array=new float[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JNumber) {
					array[n]=((JNumber)(values[n])).AsFloat();
				} else {
					throw (new JValueTypeException(n,values[n],"JNumber"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as double-precision float array. All the values in this array must be numbers inside double range.
		/// </summary>
		/// <returns>
		/// Array of doubles.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than number.
		/// </exception>
		public double[] AsDoubleArray() {
			int count=values.Count;
			double[] array=new double[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JNumber) {
					array[n]=((JNumber)(values[n])).AsDouble();
				} else {
					throw (new JValueTypeException(n,values[n],"JNumber"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as decimal array. All the values in this array must be numbers inside decimal range.
		/// </summary>
		/// <returns>
		/// Array of decimals.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than number.
		/// </exception>
		public decimal[] AsDecimalArray() {
			int count=values.Count;
			decimal[] array=new decimal[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JNumber) {
					array[n]=((JNumber)(values[n])).AsDecimal();
				} else {
					throw (new JValueTypeException(n,values[n],"JNumber"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as string array. All the values in this array must be strings or nulls.
		/// </summary>
		/// <returns>
		/// Array of strings. May contains null elements, but returning array itself can not be null.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than string or null.
		/// </exception>
		public string[] AsStringArray() {
			int count=values.Count;
			string[] array=new string[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JString) {
					array[n]=((JString)(values[n])).AsString();
				} else if (values[n] is JNull) {
					array[n]=null;
				} else {
					throw (new JValueTypeException(n,values[n],"JString"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as boolean array. All the values in this array must be booleans.
		/// </summary>
		/// <returns>
		/// Array of bools.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than boolean.
		/// </exception>
		public bool[] AsBoolArray() {
			int count=values.Count;
			bool[] array=new bool[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JBoolean) {
					array[n]=((JBoolean)(values[n])).AsBool();
				} else {
					throw (new JValueTypeException(n,values[n],"JBoolean"));
				}
			}
			return array;
		}

		/// <summary>
		/// Get this array as arrays. All the values in this array must be other arrays or nulls.
		/// </summary>
		/// <returns>
		/// Array of JArray elements. May contains null elements, but returning array itself can not be null.
		/// </returns>
		/// <exception cref="JValueTypeException">
		/// If any value in array is other than array or null.
		/// </exception>
		public JArray[] AsJArrayArray() {
			int count=values.Count;
			JArray[] array=new JArray[count];
			for (int n=0; n<count; n++) {
				if (values[n] is JArray) {
					array[n]=(JArray)(values[n]);
				} else if (values[n] is JNull) {
					array[n]=null;
				} else {
					throw (new JValueTypeException(n,values[n],"JArray"));
				}
			}
			return array;
		}

		/// <summary>
		/// Gets the whole JArray object as system list. This is recursive, so if this list contains other lists or JSON objects,
		/// those will be also changed to system objects.
		/// </summary>
		/// <returns>
		/// List that doesn't contain any TotalJSON objects on any level.
		/// </returns>
		public List<object> AsList() {
			List<object> targetValues = new List<object>();
			foreach (JValue jValue in values) {
				object oValue = InternalTools.jValueAsSystemObject(jValue);
				targetValues.Add(oValue);
			}
			return targetValues;
		}

		/// <summary>
		/// Turns this JSON array to single JSON formatted string.
		/// String always starts with character '[' and ends to character ']'
		/// </summary>
		/// <returns>
		/// This array as JSON formatted string, containing only basic ascii characters between [32..126] without line feeds.
		/// </returns>
		public override string CreateString() {
			return CreateString(new CreateStringSettings());
		}

		/// <summary>
		/// Turns this JSON array to single JSON formatted string using specified settings.
		/// String always starts with character '[' and ends to character ']'
		/// </summary>
		/// <returns>
		/// This array as JSON formatted string.
		/// </returns>
		public override string CreateString(CreateStringSettings settings) {
			CreateStringRunner createStringRunner = new CreateStringRunner(settings);
			zCreate(createStringRunner);
			return createStringRunner.getFinalString();
		}

		/// <summary>
		/// Turns this JSON array to string JSON formatted, but easily human readable string.
		/// This output is not as tightly packed as string returned by CreateString() but contains all the same data and is still completely valid JArray.
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
		/// This array as JSON formatted string, containing only basic ascii characters between [32..126], line feeds and tabs.
		/// </returns>
		public string CreatePrettyString() {
			CreateStringSettings createStringSettings = new CreateStringSettings();
			createStringSettings.HumanReadable = true;
			return CreateString(createStringSettings);
		}

		internal override void zCreate(CreateStringRunner createStringRunner) {
			if (values.Count>0) {
				createStringRunner.append('[',1);
				bool first = true;
				foreach (JValue value in values) {
					if (!first) {
						createStringRunner.append(',',0);
					} else {
						first = false;
					}
					value.zCreate(createStringRunner);
				}
				createStringRunner.append(-1,']');
			} else {
				createStringRunner.append('[',']');
			}
		}

		/// <summary>
		/// Turns JSON array formatted string to new JSON array object.
		/// </summary>
		/// <param name="jsonArrayInSingleString">
		/// Source string where to read array.
		/// </param>
		/// <returns>
		/// New JSON array object.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JArray.
		/// </exception>
		public static JArray ParseString(string jsonArrayInSingleString) {
			if (jsonArrayInSingleString==null) {
				throw (new JArgumentNullException("jsonArrayInSingleString","Source string can not be null"));
			}
			return innerParseString(jsonArrayInSingleString, null);
		}

		/// <summary>
		/// Turns JSON array formatted string to new JSON array object. Debug ID is added to this parse operation, which will be added to any possible exceptions that may occur during parse.
		/// </summary>
		/// <remarks>
		/// This method is for convenience. This method will create new ParseStringSettings object and set value for DebugIDForExceptions,
		/// then call <code>ParseString(string jsonArrayInSingleString, ParseStringSettings parseStringSettings)</code> method.
		/// </remarks>
		/// <param name="jsonArrayInSingleString">
		/// Source string where to read array.
		/// </param>
		/// <param name="debugIDForExceptions">
		/// ID that will be added to any possible exception message. This value have no effect to resulting JArray object.
		/// </param>
		/// <returns>
		/// New JSON array object.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JArray.
		/// </exception>
		public static JArray ParseString(string jsonArrayInSingleString, string debugIDForExceptions) {
			if (jsonArrayInSingleString==null) {
				throw (new JArgumentNullException("jsonArrayInSingleString","Source string can not be null"+ParseException.getExceptionMessageTailForID(debugIDForExceptions)));
			}
			ParseStringSettings parseStringSettings=new ParseStringSettings();
			parseStringSettings.DebugIDForExceptions = debugIDForExceptions;
			return innerParseString(jsonArrayInSingleString,parseStringSettings);
		}

		/// <summary>
		/// Turns JSON array formatted string to new JSON array object, using specified settings.
		/// </summary>
		/// <param name="jsonArrayInSingleString">
		/// Source string where to read array.
		/// </param>
		/// <param name="parseStringSettings">
		/// Settings to be used when parsing the string.
		/// </param>
		/// <returns>
		/// New JSON array object.
		/// </returns>
		/// <exception cref="JArgumentNullException">
		/// If parameter string or settings object is null.
		/// </exception>
		/// <exception cref="ParseException">
		/// If input string can not be parsed to JArray.
		/// </exception>
		public static JArray ParseString(string jsonArrayInSingleString, ParseStringSettings parseStringSettings) {
			if (jsonArrayInSingleString==null) {
				throw (new JArgumentNullException("jsonArrayInSingleString","Source string can not be null"+ParseException.getExceptionMessageTail(parseStringSettings)));
			}
			if (parseStringSettings==null) {
				throw (new JArgumentNullException("parseStringSettings","Settings can not be null in JArray.ParseString(string,ParseStringSettings)"+ParseException.getExceptionMessageTail(parseStringSettings)));
			}
			return innerParseString(jsonArrayInSingleString, parseStringSettings);
		}

		private static JArray innerParseString(string jsonArrayInSingleString, ParseStringSettings parseStringSettings) {
			int length = jsonArrayInSingleString.Length;
			if (length==0) {
				throw ParseException.forEmpty("Parameter string 'jsonArrayInSingleString' is empty (length 0)",parseStringSettings);
			}
			ParseStringRunner parseStringRunner = new ParseStringRunner(jsonArrayInSingleString,parseStringSettings);
			if (!parseStringRunner.containsNonWhiteChars()) {
				throw ParseException.forEmpty("Parameter string 'jsonArrayInSingleString' is not empty (length "+length+") but doesn't contain any non-white characters",parseStringSettings);
			}
			JArray jArray=zParse(parseStringRunner,true);
			if (!parseStringRunner.isAllowNonWhiteCharactersAfterObject()) {
				char chr;
				StringPointer sp = parseStringRunner.getStringPointer();
				if (sp.tryGetNextNonWhiteChar(out chr)) {
					throw ParseException.forCharactersAfterEnd(parseStringRunner);
				}
			}
			return jArray;
		}

		internal static JArray zParse(ParseStringRunner parseStringRunner, bool expectStartOfArray) {

			StringPointer sp = parseStringRunner.getStringPointer();

			char chr;
			if (expectStartOfArray) {
				if (!sp.tryGetNextNonWhiteChar(out chr)) {
					throw ParseException.forInvalidStart("Parameter string didn't contain any non-white characters",parseStringRunner);
				}
				if (chr!='[') {
					throw ParseException.forInvalidStart("Invalid character '"+chr+"' when expecting start of array '['",parseStringRunner);
				}
			}
			JArray array=new JArray();
			chr=sp.getNextNonWhiteChar();
			if (chr==']') {
				return array; // Empty array
			}
			sp.stepBack();
			do {
				array.Add(parseStringRunner.parseValue());
				chr=sp.getNextNonWhiteChar();
				if (chr==']') {
					return array; // At least one element in array
				}
				if (chr!=',') {
					throw ParseException.forInvalidCharacter("Invalid character '"+chr+"' when expecting value separator ',' or end of array ']'",parseStringRunner);
				}
			} while (true);

		}

		/// <summary>
		/// Serialize object to new JArray object, using default settings.
		/// </summary>
		/// <param name="objectToSerialize">
		/// Object that can be serialized to JArray such as list or system array.
		/// </param>
		/// <returns>
		/// JArray object containing items from parameter object.
		/// </returns>
		/// <exception cref="SerializeException">
		/// If any exceptions occurs when trying to serialize object to new JArray.
		/// </exception>
		public static JArray Serialize(object objectToSerialize) {
			return Serialize(objectToSerialize, new SerializeSettings());
		}

		/// <summary>
		/// Serialize object to JArray object, using specific settings.
		/// </summary>
		/// <param name="objectToSerialize">
		/// Object that can be serialized to JArray such as list or system array.
		/// </param>
		/// <param name="serializeSettings">
		/// Settings used for serialization.
		/// </param>
		/// <returns>
		/// JArray object containing items from parameter object.
		/// </returns>
		/// <exception cref="SerializeException">
		/// If any exceptions occurs when trying to serialize object to new JArray.
		/// </exception>
		public static JArray Serialize(object objectToSerialize, SerializeSettings serializeSettings) {
			JValue jValue = InternalTools.serializeObject(objectToSerialize, serializeSettings);
			if (jValue==null) {
				throw (new SerializeException("Parameter object is type that can't be serialized", objectToSerialize, "objectToSerialize"));
			}
			if (!(jValue is JArray)) {
				throw (new SerializeException("Parameter object can be serialized but not to JArray", objectToSerialize, "objectToSerialize"));
			}
			return ((JArray)(jValue));
		}

		/// <summary>
		/// Deserialize this JArray to object, using default settings.
		/// </summary>
		/// <typeparam name="T">
		/// Type of object that can hold all the content of this JArray.
		/// </typeparam>
		/// <returns>
		/// Object of wanted type.
		/// </returns>
		/// <exception cref="DeserializeException">
		/// If any exceptions occurs when trying to deserialize this JArray to object.
		/// </exception>
		public T Deserialize<T>() {
			return Deserialize<T>(new DeserializeSettings());
		}

		/// <summary>
		/// Deserialize this JArray to object, using specified settings.
		/// </summary>
		/// <typeparam name="T">
		/// Type of object that can hold all the content of this JArray.
		/// </typeparam>
		/// <param name="deserializeSettings">
		/// Settings used for deserialization.
		/// </param>
		/// <returns>
		/// Object of wanted type.
		/// </returns>
		/// <exception cref="DeserializeException">
		/// If any exceptions occurs when trying to deserialize this JArray to object.
		/// </exception>
		public T Deserialize<T>(DeserializeSettings deserializeSettings) {
			object obj = zDeserialize(typeof(T), null, deserializeSettings);
			return (T)(obj);
		}

		internal override object zDeserialize(Type type, string toFieldName, DeserializeSettings deserializeSettings) {

			Type listGenericType = getListGenericType(type);

			if (type.IsArray) {
				Array array = Array.CreateInstance(listGenericType,this.Length);
				for (int n=0; n<this.Length; n++) {
					array.SetValue(this[n].zDeserialize(listGenericType,toFieldName+"["+n+"]",deserializeSettings),n);
				}
				return array;
			}

			object obj = Activator.CreateInstance(type);

			if (!(obj is IList)) {
				if (type==typeof(object) && deserializeSettings.AllowFieldsToBeObjects) {
					return this.AsList();
				}
				throw (DeserializeException.forNonMatchingType(this,type,toFieldName));
			}
			IList iList = (IList)(obj);

			foreach (JValue jValue in this.values) {
				iList.Add(jValue.zDeserialize(listGenericType, toFieldName, deserializeSettings));
			}

			return obj;

		}

		private Type getListGenericType(Type type) {
			Type iListType = typeof(IList<>);
			Type[] interfaceTypes = type.GetInterfaces();
			foreach (Type interfaceType in interfaceTypes) {
				if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition()==iListType) {
					Type[] genericArguments = type.GetGenericArguments();
					if (genericArguments.Length>0) {
						return genericArguments[0];
					}
					Type elementType = type.GetElementType();
					if (elementType!=null) {
						return elementType;
					}
				}
			}
			return (typeof(object));
		}

		/// <summary>
		/// Sets this JArray and all its childen protected (read only). New values can't be added and old values replaced or removed.
		/// </summary>
		/// <remarks>
		/// Typically this is called only for top level JSON. Protecetion can't be cancelled, once it is set, it will stay.
		/// </remarks>
		public void SetProtected() {
			thisProtected = true;
			foreach (JValue value in values) {
				if (value is JSON) {
					((JSON)(value)).SetProtected();
				} else if (value is JArray) {
					((JArray)(value)).SetProtected();
				}
			}
		}

		/// <summary>
		/// Check whatever this JArray is protected (read only). If it is, it also means that all its childen are protected, but nothing can be assumed of parents of this object.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this object is protected, <c>false</c> otherwise
		/// </returns>
		public bool IsProtected() {
			return thisProtected;
		}

		/// <summary>
		/// Adds this JArray object to editor debug window so content of JSON object can be followed in Unity editor when application is running.
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
			jsonRuntimeDebugContainer.add(debugName, this);
#endif
		}

	}

}
