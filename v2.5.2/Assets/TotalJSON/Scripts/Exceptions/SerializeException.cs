//  SerializeException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if something goes wrong when serializing system objects to JSON.
	/// </summary>
	public class SerializeException : ArgumentException {

		internal SerializeException(string message)
			: base(message) {
		}

		internal SerializeException(string message, object problemObject)
			: base(message+", object type = "+problemObject.GetType()) {
		}

		internal SerializeException(string message, object problemObject, string paramName)
			: base(message+", object type = "+problemObject.GetType(),paramName) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
