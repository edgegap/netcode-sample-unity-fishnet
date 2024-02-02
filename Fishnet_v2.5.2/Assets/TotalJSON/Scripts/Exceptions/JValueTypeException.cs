//  JValueTypeException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception thrown if trying to read value which is different type than requested. For example trying to read bool (JBoolean) as string (JString).
	/// </summary>
	public class JValueTypeException : InvalidCastException {

		internal JValueTypeException(string key, JValue realValue, string triedType, string exceptionMessageTail)
			: base("Can not cast value mapped to key \""+key+"\" to "+triedType+", value type is "+realValue.GetType()+exceptionMessageTail) {
		}

		internal JValueTypeException(int arrayIndex, JValue realValue, string triedType)
			: base("Can not cast array element at index "+arrayIndex+" to "+triedType+", element type is "+realValue.GetType()) {
		}

		protected JValueTypeException(string message)
			: base(message) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
