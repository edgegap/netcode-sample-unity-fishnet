//  ProtectedException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception thrown if trying to change anything in protected JSON or JArray objects.
	/// </summary>
	public class ProtectedException : Exception {

		internal ProtectedException(string message) : base(message) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
