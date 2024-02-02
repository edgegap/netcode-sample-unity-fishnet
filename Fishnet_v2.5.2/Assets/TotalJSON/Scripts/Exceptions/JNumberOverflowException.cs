//  JNumberOverflowException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if trying to read JNumber value in format where it doesn't fit. For example trying to read number 2147483648 as int.
	/// </summary>
	public class JNumberOverflowException : OverflowException {

		internal JNumberOverflowException(string valueAsString, string wantedType, string minValue, string maxValue)
			: base("JNumber value ("+valueAsString+") is outside "+wantedType+" range ["+minValue+".."+maxValue+"]") {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
