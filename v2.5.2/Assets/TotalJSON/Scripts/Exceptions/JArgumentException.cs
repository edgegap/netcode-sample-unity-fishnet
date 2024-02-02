//  JArgumentException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if some JSON, JArray or JNumber parameter is invalid.
	/// </summary>
	public class JArgumentException : ArgumentException {

		internal JArgumentException(string message, string paramName)
			: base(message,paramName) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
