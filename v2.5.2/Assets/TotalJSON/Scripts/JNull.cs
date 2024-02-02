//  JValue - JNull


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {
	
	/// <summary>
	/// Class to store null value in JSON format.
	/// </summary>
	public class JNull : JValue {

		private const string NULL="null";

		/// <summary>
		/// Creates new JNull object.
		/// </summary>
		public JNull() : base() {
		}

		public override string ToString() {
			return ("[JNull]");
		}

        /// <summary>
        /// Test if another object equals to this object. This method always returns true if parameter object is instance of JNull, but false if parameter is system null.
        /// </summary>
        /// <param name="anotherObject">
        /// Another object that is compared to this one.
        /// </param>
        /// <returns>
        /// True if objects are equal, false otherwise.
        /// </returns>
        public override bool Equals(object anotherObject) {
			if (anotherObject==null) {
				return false;
			}
			if (!(anotherObject is JNull)) {
				return false;
			}
			return true;
		}

		public override int GetHashCode() {
			return 101;
		}

		internal override void zCreate(CreateStringRunner createStringRunner) {
			createStringRunner.append(NULL);
		}

		internal static JNull zParse(ParseStringRunner parseStringRunner) {

			StringPointer sp = parseStringRunner.getStringPointer();

			if (sp.isNextChars(NULL.Substring(1))) {
				return (new JNull());
			} else {
				throw ParseException.forInvalidCharacter("Invalid string when expecting '"+NULL+"'",parseStringRunner);
			}

		}

		internal override object zDeserialize(Type type, string toFieldName, DeserializeSettings deserializeSettings) {

			// Do at least some basic checking that not trying to add null to number or boolean field, as trying to set null to these objects using FieldInfo doesn't cause exception.
			// If field type is nullable like "bool?" or "int?", type is also System.Nullable so this check doesn't prevent adding null value to those fields.
			if (type==typeof(float) || type==typeof(double) || type==typeof(decimal)
				|| type==typeof(int) || type==typeof(long) || type==typeof(short) || type==typeof(byte)
				|| type==typeof(uint) || type==typeof(ulong) || type==typeof(ushort) || type==typeof(sbyte)
				|| type==typeof(bool)) {
				throw (DeserializeException.forNonMatchingType(this,type,toFieldName));
			}

			// bool canBeNull = !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);

			return null;

		}

	}

}
