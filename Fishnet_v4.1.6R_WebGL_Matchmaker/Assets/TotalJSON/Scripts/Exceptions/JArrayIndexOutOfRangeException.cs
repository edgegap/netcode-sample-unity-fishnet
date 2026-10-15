//  JArrayIndexOutOfRangeException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if some JArray method parameter is out of range.
	/// </summary>
	public class JArrayIndexOutOfRangeException : ArgumentOutOfRangeException {

		internal JArrayIndexOutOfRangeException(string paramName, int actualValue, string message)
			: base(paramName,actualValue,message) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
