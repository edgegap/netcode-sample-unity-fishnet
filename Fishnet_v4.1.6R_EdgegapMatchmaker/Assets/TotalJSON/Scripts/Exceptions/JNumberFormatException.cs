//  JNumberFormatException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if trying to read JNumber value in wrong format, for example float as int.
	/// </summary>
	public class JNumberFormatException : FormatException {

		internal JNumberFormatException(string valueAsString, string wantedType)
			: base("JNumber value ("+valueAsString+") is floating point number and can't be returned as "+wantedType) {
		}

		internal JNumberFormatException(string valueAsString)
			: base("JNumber value ("+valueAsString+") is not valid decimal number") { // (contains E/e notation, consider reading this value as double or float)
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
