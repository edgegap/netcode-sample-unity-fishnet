//  JArgumentNullException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if object added to JSON or JArray is type that can't be converted to any JValue.
	/// </summary>
	public class UnknownObjectTypeException : JArgumentException {

		internal UnknownObjectTypeException(object unknownValue, string paramName)
			: base("Parameter object is unknown type '"+unknownValue.GetType().ToString()+"'",paramName) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
