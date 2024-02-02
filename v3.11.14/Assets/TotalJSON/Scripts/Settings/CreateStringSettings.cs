//  CreateStringSettings


using System;
using UnityEngine;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Settings that can be used to make output of creating JSON-formatted string different.
	/// </summary>
	public class CreateStringSettings {

		private bool escapeForwardSlashes = false;

		/// <summary>
		/// Sets forward slashes to escaped or not. TotalJSON default is false since escaped forward slashes could cause problems if included to C# code.
		/// However, if resulting JSON string is included to for example HTML or JavaScript, it is better to set forward slash escaping on.
		/// </summary>
		/// <value>
		/// True to escape forward slashes ("\/"), false to not ("/").
		/// </value>
		public bool EscapeForwardSlashes {
			set {
				escapeForwardSlashes = value;
			}
			get {
				return escapeForwardSlashes;
			}
		}

		private bool humanReadable = false;

		/// <summary>
		/// Sets output to be more human readable. Linefeeds and indentations are added to output to make it easier for humans to read and edit.
		/// Output is still completely valid JSON that can be parsed back to JSON or JArray object.
		/// </summary>
		/// <value>
		/// True to make output human readable. Default is false.
		/// </value>
		public bool HumanReadable {
			set {
				humanReadable = value;
			}
			get {
				return humanReadable;
			}
		}

		private bool indentUsingTab = true;

		/// <summary>
		/// Sets whatever indent of human readable output should use tabs. If false, spaces are used instead of tab.
		/// </summary>
		/// <remarks>
		/// This setting have effect only if 'HumanReadable' is true.
		/// </remarks>
		/// <value>
		/// True to use tabs for indent. Default is true.
		/// </value>
		public bool IndentUsingTab {
			set {
				if (!humanReadable) {
					Debug.LogWarning("CreateStringSettings.IndentUsingTab setting have no effect when CreateStringSettings.HumanReadable is false");
				}
				indentUsingTab = value;
			}
			get {
				return indentUsingTab;
			}
		}

		private int indentSpaceCount = 4;

		/// <summary>
		/// Sets how many spaces are used for indent. Can be 0 or any positive integer.
		/// </summary>
		/// <remarks>
		/// This setting have effect only if 'HumanReadable' is true and 'IndentUsingTab' is false.
		/// </remarks>
		/// <value>
		/// Amount of spaces to use for indent. Default is 4.
		/// </value>
		public int IndentSpaceCount {
			set {
				if (!humanReadable) {
					Debug.LogWarning("CreateStringSettings.IndentSpaceCount setting have no effect when CreateStringSettings.HumanReadable is false");
				} else if (indentUsingTab) {
					Debug.LogWarning("CreateStringSettings.IndentSpaceCount setting have no effect when CreateStringSettings.IndentUsingTab is true");
				}
				indentSpaceCount = value;
			}
			get {
				return indentSpaceCount;
			}
		}

		public enum NewLineTypes {
			EnvironmentDefault,
			LF,
			CR_LF
		}

		private NewLineTypes newLine = NewLineTypes.EnvironmentDefault;

		/// <summary>
		/// Sets type of linefeeds in human readable output.
		/// </summary>
		/// <remarks>
		/// This setting have effect only if 'HumanReadable' is true.
		/// </remarks>
		/// <value>
		/// Type of linefeeds, one of values from NewLineTypes. Default is EnvironmentDefault.
		/// </value>
		public NewLineTypes NewLine {
			set {
				if (!humanReadable) {
					Debug.LogWarning("CreateStringSettings.NewLine setting have no effect when CreateStringSettings.HumanReadable is false");
				}
				newLine = value;
			}
			get {
				return newLine;
			}
		}

	}

}
