//  JValue - JNumber


using System;
using System.Text;
using System.Globalization;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Class to store number value in JSON format. Once JNumber instance is created, its value can't be changed.
	/// 
	/// There is no limit how long or big numbers can be, but trying to read too big number for example as long will cause exception.
	/// Oversized numbers can be still read out and handled as strings.
	/// </summary>
	public class JNumber : JValue {

		private readonly string valueAsString;

		/// <summary>
		/// Creates new JSON number value from string. There is no limits in number size as long as it follows json number format.
		/// </summary>
		/// <param name="numberAsString">
		/// Value for this JNumber object in string format.
		/// </param>
		/// <exception cref="JArgumentException">
		/// If parameter string is not valid number.
		/// </exception>
		public JNumber(string numberAsString) : base() {
			string errorMessage=numberFormatCheck(numberAsString);
			if (errorMessage!=null) {
				throw (new JArgumentException("Parameter \""+numberAsString+"\" is not valid JSON format number: "+errorMessage,"numberAsString"));
			}
			valueAsString = numberAsString;
		}

		/// <summary>
		/// Creates new JSON number value from c# int value.
		/// </summary>
		/// <param name="numberAsInt">
		/// Value for this JNumber object.
		/// </param>
		public JNumber(int numberAsInt) : base() {
			valueAsString=safeLongAsString(numberAsInt);
		}

		/// <summary>
		/// Creates new JSON number value from c# long value.
		/// </summary>
		/// <param name="numberAsLong">
		/// Value for this JNumber object.
		/// </param>
		public JNumber(long numberAsLong) : base() {
			valueAsString=safeLongAsString(numberAsLong);
		}

		/// <summary>
		/// Creates new JSON number value from c# float value.
		/// </summary>
		/// <param name="numberAsFloat">
		/// Value for this JNumber object.
		/// </param>
		/// <exception cref="JArgumentException">
		/// If parameter is NaN or Infinity.
		/// </exception>
		public JNumber(float numberAsFloat) : base() {
			if (float.IsNaN(numberAsFloat)) {
				throw (new JArgumentException("Can not create new JNumber from float that is NaN","numberAsFloat"));
			}
			if (float.IsPositiveInfinity(numberAsFloat)) {
				throw (new JArgumentException("Can not create new JNumber from float that is infinity","numberAsFloat"));
			}
			if (float.IsNegativeInfinity(numberAsFloat)) {
				throw (new JArgumentException("Can not create new JNumber from float that is negative infinity","numberAsFloat"));
			}
			valueAsString=safeFloatAsString(numberAsFloat);
		}

		/// <summary>
		/// Creates new JSON number value from c# double value.
		/// </summary>
		/// <param name="numberAsDouble">
		/// Value for this JNumber object.
		/// </param>
		/// <exception cref="JArgumentException">
		/// If parameter is NaN or Infinity.
		/// </exception>
		public JNumber(double numberAsDouble) : base() {
			if (double.IsNaN(numberAsDouble)) {
				throw (new JArgumentException("Can not create new JNumber from double that is NaN","numberAsDouble"));
			}
			if (double.IsPositiveInfinity(numberAsDouble)) {
				throw (new JArgumentException("Can not create new JNumber from double that is infinity","numberAsDouble"));
			}
			if (double.IsNegativeInfinity(numberAsDouble)) {
				throw (new JArgumentException("Can not create new JNumber from double that is negative infinity","numberAsDouble"));
			}
			valueAsString=safeDoubleAsString(numberAsDouble);
		}

		/// <summary>
		/// Creates new JSON number value from c# decimal value.
		/// </summary>
		/// <param name="numberAsDecimal">
		/// Value for this JNumber object.
		/// </param>
		public JNumber(decimal numberAsDecimal) : base() {
			valueAsString=addDecimalPoint(numberAsDecimal.ToString(CultureInfo.InvariantCulture));
		}

		private JNumber(string safeValueAsString, ParseStringRunner parseStringRunner) : base() {
			valueAsString=parseStringRunner.getPossiblyFixedNumber(safeValueAsString);
		}

		public override string ToString() {
			return ("[JNumber: "+valueAsString+"]");
		}

        /// <summary>
        /// Test if another object equals to this object. Always returns false if parameter object is null or it is not instance of JNumber.
        /// Two JNumber objects are equal if both contains value which string representation is exactly equal.
        /// For example JNumber that contains "1" is not equal to JNumber that contains "1.0"
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
			if (!(anotherObject is JNumber)) {
				return false;
			}
			JNumber anotherJNumber=(JNumber)(anotherObject);
			return (valueAsString.Equals(anotherJNumber.AsString()));
		}

		public override int GetHashCode() {
			return valueAsString.GetHashCode();
		}

		/// <summary>
		/// Gets value of this number object as long. This will throw exception if number is floating point number or outside long range.
		/// </summary>
		/// <returns>
		/// Value as long.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in long.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public long AsLong() {
			try {
				return long.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"long",""+long.MinValue,""+long.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"long"));
			}
		}

		/// <summary>
		/// Gets value of this number object as unsigned long. This will throw exception if number is floating point number or outside ulong range.
		/// </summary>
		/// <returns>
		/// Value as ulong.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in ulong.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public ulong AsULong() {
			try {
				return ulong.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"ulong",""+ulong.MinValue,""+ulong.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"ulong"));
			}
		}

		/// <summary>
		/// Gets value of this number object as int. This will throw exception if number is floating point number or outside int range.
		/// </summary>
		/// <returns>
		/// Value as int.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in int.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public int AsInt() {
			try {
				return int.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"int",""+int.MinValue,""+int.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"int"));
			}
		}

		/// <summary>
		/// Gets value of this number object as unsigned int. This will throw exception if number is floating point number or outside uint range.
		/// </summary>
		/// <returns>
		/// Value as uint.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in uint.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public uint AsUInt() {
			try {
				return uint.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"uint",""+uint.MinValue,""+uint.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"uint"));
			}
		}

		/// <summary>
		/// Gets value of this number object as short. This will throw exception if number is floating point number or outside short range.
		/// </summary>
		/// <returns>
		/// Value as short.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in short.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public short AsShort() {
			try {
				return short.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"short",""+short.MinValue,""+short.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"short"));
			}
		}

		/// <summary>
		/// Gets value of this number object as unsigned short. This will throw exception if number is floating point number or outside ushort range.
		/// </summary>
		/// <returns>
		/// Value as ushort.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in ushort.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public ushort AsUShort() {
			try {
				return ushort.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"ushort",""+ushort.MinValue,""+ushort.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"ushort"));
			}
		}

		/// <summary>
		/// Gets value of this number object as byte. This will throw exception if number is floating point number or outside byte range.
		/// </summary>
		/// <returns>
		/// Value as byte.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in byte.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public byte AsByte() {
			try {
				return byte.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"byte",""+byte.MinValue,""+byte.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"byte"));
			}
		}

		/// <summary>
		/// Gets value of this number object as signed byte. This will throw exception if number is floating point number or outside sbyte range.
		/// </summary>
		/// <returns>
		/// Value as sbyte.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in sbyte.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber is floating point number.
		/// </exception>
		public sbyte AsSByte() {
			try {
				return sbyte.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"sbyte",""+sbyte.MinValue,""+sbyte.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString,"sbyte"));
			}
		}

		/// <summary>
		/// Gets value of this number object as double. This will throw exception if number is outside double range.
		/// </summary>
		/// <returns>
		/// Value as double.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in double.
		/// </exception>
		public double AsDouble() {
			try {
				double value=double.Parse(valueAsString,CultureInfo.InvariantCulture);
				if (double.IsInfinity(value)) {
					throw (new OverflowException());
				}
				return value;
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"double",""+double.MinValue,""+double.MaxValue));
			}
		}

		/// <summary>
		/// Gets value of this number object as float. This will throw exception if number is outside float range.
		/// </summary>
		/// <returns>
		/// Value as float.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in float.
		/// </exception>
		public float AsFloat() {
			try {
				float value=float.Parse(valueAsString,CultureInfo.InvariantCulture);
				if (float.IsInfinity(value)) {
					throw (new OverflowException());
				}
				return value;
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"float",""+float.MinValue,""+float.MaxValue));
			}
		}

		/// <summary>
		/// Gets value of this number object as decimal. This will throw exception if number is outside decimal range or number contains E/e notation.
		/// </summary>
		/// <returns>
		/// Value as decimal.
		/// </returns>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in decimal.
		/// </exception>
		/// <exception cref="JNumberFormatException">
		/// If number stored to this JNumber uses E/e notation (like 1.234567e89)
		/// </exception>
		public decimal AsDecimal() {
			try {
				return decimal.Parse(valueAsString,CultureInfo.InvariantCulture);
			}
			catch (OverflowException) {
				throw (new JNumberOverflowException(valueAsString,"decimal",""+decimal.MinValue,""+decimal.MaxValue));
			}
			catch (FormatException) {
				throw (new JNumberFormatException(valueAsString));
			}
		}

		/// <summary>
		/// Gets value of this number as string.
		/// </summary>
		/// <returns>
		/// Value as string.
		/// </returns>
		public string AsString() {
			return valueAsString;
		}

		/// <summary>
		/// Gets value of this number as object. First fitting value of these are returned: int, long, float, double
		/// </summary>
		/// <exception cref="JNumberOverflowException">
		/// If number stored to this JNumber doesn't fit in double.
		/// </exception>
		/// <returns>
		/// Value as object, that may be one of the 4 basic number objects.
		/// </returns>
		public object AsObject() {
			int iValue;
			if (int.TryParse(valueAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out iValue)) {
				return iValue;
			}
			long lValue;
			if (long.TryParse(valueAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out lValue)) {
				return lValue;
			}
			float fValue;
			if (float.TryParse(valueAsString, NumberStyles.Float, CultureInfo.InvariantCulture, out fValue)) {
				if (!float.IsInfinity(fValue)) {
					return fValue;
				}
			}
			// This could fail too in extreme cases, but custom exception is thrown
			return this.AsDouble();
		}

		internal override void zCreate(CreateStringRunner createStringRunner) {
			createStringRunner.append(this.AsString());
		}

		internal static JNumber zParse(ParseStringRunner parseStringRunner, char firstChr) {

			StringPointer sp = parseStringRunner.getStringPointer();

			int start=sp.getCurrentIndex()-1; // -1 since first character is already read
			bool valid=parseCheck(sp,firstChr);
			if (!valid) {
				throw ParseException.forInvalidCharacter("Invalid number value \""+sp.getSubStringStartingFrom(start)+"\"",parseStringRunner);
			} else {
				sp.stepBack();
				string validNumber=sp.getSubStringStartingFrom(start);
				return (new JNumber(validNumber,parseStringRunner));
			}

		}

		private static bool parseCheck(StringPointer sp, char chr) {
			int state;
			if (chr=='-') {
				chr=sp.getNextChar();
			}
			if (chr=='0') {
				chr=sp.getNextChar();
				if (chr=='.') {
					state=5;
				} else if (chr=='e' || chr=='E') {
					state=7;
				} else {
					return true; // (-)0
				}
			} else if (chr>='1' && chr<='9') {
				state=4;
			} else {
				return false;
			}
			do {
				chr=sp.getNextChar();
				if (state==4) {
					if (chr>='0' && chr<='9') {
						state=4;
					} else if (chr=='.') {
						state=5;
					} else if (chr=='e' || chr=='E') {
						state=7;
					} else {
						return true; // (-)##
					}
				} else if (state==5) {
					if (chr>='0' && chr<='9') {
						state=6;
					} else {
						return false;
					}
				} else if (state==6) {
					if (chr>='0' && chr<='9') {
						state=6;
					} else if (chr=='e' || chr=='E') {
						state=7;
					} else {
						return true; // (-)(##).##
					}
				} else if (state==7) {
					if (chr=='+' || chr=='-') {
						chr=sp.getNextChar();
						if (chr>='0' && chr<='9') {
							state=9;
						} else {
							return false;
						}
					} else if (chr>='0' && chr<='9') {
						state=9;
					} else {
						return false;
					}
				} else if (state==9) {
					if (chr>='0' && chr<='9') {
						state=9;
					} else {
						return true; // (-)(##).##[e/E](+/-)##
					}
				}
			} while (true);
		}

		private static string numberFormatCheck(string str) {

			int count=str.Length,
			state=1;

			for (int index=0; index<count; index++) {
				char chr=str[index];
				if (state==1) { // Minus or first digit
					if (chr=='-') {
						state=2;
					} else if (chr=='0') {
						state=3;
					} else if (chr>='1' && chr<='9') {
						state=4;
					} else {
						return ("Invalid first character '"+chr+"', should be digit or minus sign");
					}
				} else if (state==2) { // First digit after minus
					if (chr=='0') {
						state=3;
					} else if (chr>='1' && chr<='9') {
						state=4;
					} else {
						return ("Expecting at least one digit after minus sign, got '"+chr+"' instead");
					}
				} else if (state==3) { // Number started with (minus) zero, only decimal point or E can follow
					if (chr=='.') {
						state=5;
					} else if (chr=='e' || chr=='E') {
						state=7;
					} else {
						return ("Only decimal point or E/e can follow number starting with 0 or -0, got '"+chr+"' instead");
					}
				} else if (state==4) { // Digits before decimal point or E
					if (chr=='.') {
						state=5;
					} else if (chr=='e' || chr=='E') {
						state=7;
					} else if (chr<'0' || chr>'9') {
						return ("Invalid character '"+chr+"' (before possible decimal point or E/e)");
					}
				} else if (state==5) { // Need at least one digit after decimal point
					if (chr>='0' && chr<='9') {
						state=6;
					} else {
						return ("Need at least one digit after decimal point, got '"+chr+"' instead");
					}
				} else if (state==6) { // Following digits after decimal point
					if (chr=='e' || chr=='E') {
						state=7;
					} else if (chr<'0' || chr>'9') {
						return ("Invalid character '"+chr+"' after decimal point");
					}
				} else if (state==7) { // Plus, minus or digit after E
					if (chr=='+' || chr=='-') {
						state=8;
					} else if (chr>='0' && chr<='9') {
						state=9;
					} else {
						return ("Expecting digit or plus/minus sign after E/e, got '"+chr+"' instead");
					}
				} else if (state==8) { // Need at least one digit after E plus/minus, 
					if (chr>='0' && chr<='9') {
						state=9;
					} else {
						return ("Expecting digit after plus/minus sign after E/e, got '"+chr+"' instead");
					}
				} else if (state==9) { // Following digits after E
					if (chr<'0' || chr>'9') {
						return ("Invalid character '"+chr+"' in digits after E/e");
					}
				}
			}

			if (state==1) {
				return "String is empty";
			}
			if (state==2) {
				return "String contains only minus sign";
			}
			if (state==5) {
				return "No digits after decimal point";
			}
			if (state==7) {
				return "Need at least one digit after E/e";
			}
			if (state==8) {
				return "Need at least one digit after plus/minus sign after E/e";
			}

			return null;

		}

		private string safeFloatAsString(float value) {
			// Basic change that doesn't add too many decimals
			string basicStr=value.ToString(CultureInfo.InvariantCulture);
			basicStr=addDecimalPoint(basicStr);
			try {
				float reverse=float.Parse(basicStr,CultureInfo.InvariantCulture);
				if (reverse.Equals(value)) { // May not be true because rounding
					return basicStr;
				}
			}
			catch (OverflowException) {
				// This happens because rounding if value is close min/max
			}
			// Try out with more decimals
			string rStr=value.ToString("R",CultureInfo.InvariantCulture);
			rStr=addDecimalPoint(rStr);
			try {
				float reverse=float.Parse(rStr,CultureInfo.InvariantCulture);
				if (reverse.Equals(value)) {
					return rStr;
				}
			}
			catch (Exception) {
			}
			// If neither works, fall back to simplest
			return basicStr;
		}

		private string safeDoubleAsString(double value) {
			// Basic change that doesn't add too many decimals
			string basicStr=value.ToString(CultureInfo.InvariantCulture);
			basicStr=addDecimalPoint(basicStr);
			try {
				double reverse=double.Parse(basicStr,CultureInfo.InvariantCulture);
				if (reverse.Equals(value)) {
					return basicStr;
				}
			}
			catch (OverflowException) {
				// This happens because rounding if value is close min/max
			}
			// Try out with more decimals
			string rStr=value.ToString("R",CultureInfo.InvariantCulture);
			rStr=addDecimalPoint(rStr);
			try {
				double reverse=double.Parse(rStr,CultureInfo.InvariantCulture);
				if (reverse.Equals(value)) {
					return rStr;
				}
			}
			catch (Exception) {
			}
			// If neither works, fall back to simplest
			return basicStr;
		}
		
		private static string addDecimalPoint(string str) {
			if (str.IndexOf('E')==-1 && str.IndexOf('e')==-1 && str.IndexOf('.')==-1) {
				str+=".0";
			}
			return str;
		}

		private static string safeLongAsString(long value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		private static string safeULongAsString(ulong value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		internal override object zDeserialize(Type type, string toFieldName, DeserializeSettings deserializeSettings) {

			// In case type is nullable type, for example "int?"
			Type nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null) {
				return this.zDeserialize(nullableType, toFieldName, deserializeSettings);
			}

			if (type==typeof(float)) {
				return this.AsFloat();
			}
			if (type==typeof(double)) {
				return this.AsDouble();
			}
			if (type==typeof(decimal)) {
				return this.AsDecimal();
			}

			if (type==typeof(int)) {
				return this.AsInt();
			}
			if (type==typeof(long)) {
				return this.AsLong();
			}
			if (type==typeof(short)) {
				return this.AsShort();
			}
			if (type==typeof(byte)) {
				return this.AsByte();
			}

			if (type==typeof(uint)) {
				return this.AsUInt();
			}
			if (type==typeof(ulong)) {
				return this.AsULong();
			}
			if (type==typeof(ushort)) {
				return this.AsUShort();
			}
			if (type==typeof(sbyte)) {
				return this.AsSByte();
			}

			if (type==typeof(object) && deserializeSettings.AllowFieldsToBeObjects) {
				return this.AsObject();
			}

			throw (DeserializeException.forNonMatchingType(this,type,toFieldName));

		}

	}

}
