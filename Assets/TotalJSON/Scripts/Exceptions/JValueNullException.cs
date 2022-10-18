//  JValueNullException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception thrown if trying to read value which is null while that is not allowed.
	/// </summary>
	public class JValueNullException : JValueTypeException {

		internal JValueNullException(string key, string triedType, string exceptionMessageTail)
			: base("Can not read value mapped to key \""+key+"\" as "+triedType+", value is null"+exceptionMessageTail) {
		}

		internal JValueNullException(int index, string triedType)
			: base("Can not read value at index "+index+" as "+triedType+", value is null") {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
