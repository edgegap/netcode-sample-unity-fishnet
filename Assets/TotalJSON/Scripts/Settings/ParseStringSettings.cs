//  ParseStringSettings


using System;
using UnityEngine;

namespace Leguar.TotalJSON {
	
	/// <summary>
	/// Settings that can be used to make changes how string is parsed to JSON or JArray.
	/// </summary>
	public class ParseStringSettings {

		private bool fixRoundedFloatingPointMinMaxValues = true;

		/// <summary>
		/// If set true, numeric values that seem to be just rounded float.MinValue, float.MaxValue, double.MinValue or double.MaxValue are set to those exact values.
		/// 
		/// C# floating point numbers rounding sometimes causes unwanted effects. In some systems, for example double.Parse(double.MaxValue.ToString()) will cause number overflow exception.
		/// TotalJSON will use exact values when creating JSON formatted string using CreateString() method. But if JSON is created using other methods and it is possible JSON may contain
		/// rounded floating point min/max values, it is better to set this setting true so that parsed values are what they are expected to be.
		/// </summary>
		/// <value>
		/// True to fix rounded floating point values to float/double min/max values. False to parse numbers exactly as they are. Default is true.
		/// </value>
		public bool FixRoundedFloatingPointMinMaxValues {
			set {
				fixRoundedFloatingPointMinMaxValues = value;
			}
			get {
				return fixRoundedFloatingPointMinMaxValues;
			}
		}

		private int parseStartIndex = 0;

		/// <summary>
		/// Sets index of input string where parsing JSON or JArray object is started.
		/// </summary>
		/// <value>
		/// The index of the parse start. Default is 0.
		/// </value>
		public int ParseStartIndex {
			set {
				parseStartIndex = value;
			}
			get {
				return parseStartIndex;
			}
		}

		private bool allowNonWhiteCharactersAfterObject = false;

		/// <summary>
		/// Sets whatever it is acceptable for input string to have other than non-white characters after end of JSON or JArray object.
		/// </summary>
		/// <value>
		/// If set to true, non-white characters are accepted at end of object. Default is false, so expecting that input string ends at the end of JSON or JArray object.
		/// </value>
		public bool AllowNonWhiteCharactersAfterObject {
			set {
				allowNonWhiteCharactersAfterObject = value;
			}
			get {
				return allowNonWhiteCharactersAfterObject;
			}
		}

		private string debugIDForExceptions = null;

		/// <summary>
		/// Sets debug ID for this string parse and resulting JSON object. If any exception occurres during parsing or JSON handling after
		/// succesful parse, this debug ID will be added to exception message.
		/// 
		/// This is typically useful in production builds where only exception message is logged but full stacktrace may not be available.
		/// Typical parse error could be that source string is null or empty for some reason. Without adding this debug id to the parse,
		/// exception is just "ParseException: Source string is empty" which isn't very helpful if project is parsing lots of incoming JSON
		/// and so it is not clear which one causes this error. When adding this debug id to the parse, above exception would be for example
		/// "ParseException: Source string is empty - Parse Debug ID: Backend own currency settings", which pinpoints the problem instantly.
		/// </summary>
		/// <value>
		/// Debug ID. Default is null.
		/// </value>
		public string DebugIDForExceptions {
			set {
				debugIDForExceptions = value;
			}
			get {
				return debugIDForExceptions;
			}
		}

	}

}
