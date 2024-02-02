//  DeserializeException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception that is thrown if something goes wrong when deserializing JSON to objects.
	/// </summary>
	public class DeserializeException : ArgumentException {

		private DeserializeException(string message) : base(message) {
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

		internal static DeserializeException forDictionaryKeyTypeNotString(Type type, string toFieldName) {
			string fullMessage = "Can not deserialize to dictionary where key type is '"+type.GetType()+"'"+getToFieldNameString(toFieldName)+". Key type need to be string, or allow more loose options using DeserializeSettings";
			return (new DeserializeException(fullMessage));
		}

		internal static DeserializeException forDictionaryKeyTypeNotKnown(Type type, string toFieldName) {
			string fullMessage = "Can not deserialize to dictionary where key type is '"+type.GetType()+"'"+getToFieldNameString(toFieldName)+". Key type is none of the supported";
			return (new DeserializeException(fullMessage));
		}

		internal static DeserializeException forNonMatchingType(JValue jValue, Type type, string toFieldName) {
			string fullMessage = "Can not deserialize '"+jValue.GetType()+"' to object which type is '"+type+"'"+getToFieldNameString(toFieldName);
			return (new DeserializeException(fullMessage));
		}

		internal static DeserializeException forNoMatchingField(string fieldName, Type type) {
			string fullMessage = "Can't find field named '"+fieldName+"' needed for object type '"+type+"'. Values for all fields need to exist, or allow more loose options using DeserializeSettings";
			return (new DeserializeException(fullMessage));
		}

		internal static DeserializeException forNoMatchingValue(Type type) {
			string fullMessage = "Not all JSON values were used when populating object type '"+type+"'. Used DeserializeSettings requires that all fields are used";
			return (new DeserializeException(fullMessage));
		}

		private static string getToFieldNameString(string toFieldName) {
			return (string.IsNullOrEmpty(toFieldName) ? "" : " (field \""+toFieldName+"\")");
		}

	}

}
