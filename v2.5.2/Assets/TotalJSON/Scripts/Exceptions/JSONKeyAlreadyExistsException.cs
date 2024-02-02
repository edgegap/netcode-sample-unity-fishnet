//  JSONKeyAlreadyExistsException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if JSON already contains certain key and trying to add it again.
	/// </summary>
	public class JSONKeyAlreadyExistsException : ArgumentException {

		internal JSONKeyAlreadyExistsException(string message)
			: base(message,"key") {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
