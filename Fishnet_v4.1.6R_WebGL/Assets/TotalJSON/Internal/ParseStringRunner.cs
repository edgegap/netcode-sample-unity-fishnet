//  ParseStringRunner


namespace Leguar.TotalJSON.Internal {

	class ParseStringRunner {

		private ParseStringSettings settings;
		private StringPointer stringPointer;

		internal ParseStringRunner(string sourceString, ParseStringSettings settings) {
			if (settings==null) {
				settings = new ParseStringSettings();
			}
			this.settings = settings;
			stringPointer=new StringPointer(sourceString,settings.ParseStartIndex,this);
		}

		internal StringPointer getStringPointer() {
			return stringPointer;
		}

		internal bool containsNonWhiteChars() {
			return stringPointer.containsNonWhiteChars();
		}

		internal string getPossiblyFixedNumber(string originalNumber) {
			if (settings.FixRoundedFloatingPointMinMaxValues) {
				string testNumber = originalNumber.Replace('e','E');
				if (testNumber.Equals("3.402823E+38")) { // Rounded float.MaxValue
					return            "3.40282347E+38";  // Exact   float.MaxValue
				}
				if (testNumber.Equals("-3.402823E+38")) { // Rounded float.MinValue
					return            "-3.40282347E+38";  // Exact   float.MinValue
				}
				if (testNumber.Equals("1.79769313486232E+308")) { // Rounded double.MaxValue
					return            "1.7976931348623157E+308";  // Exact   double.MaxValue (that doesn't throw OverflowException if read as double)
				}
				if (testNumber.Equals("-1.79769313486232E+308")) { // Rounded double.MinValue
					return            "-1.7976931348623157E+308";  // Exact   double.MinValue (that doesn't throw OverflowException if read as double)
				}
			}
			return originalNumber;
		}

		internal bool isAllowNonWhiteCharactersAfterObject() {
			return settings.AllowNonWhiteCharactersAfterObject;
		}

		internal JValue parseValue() {
			char chr=stringPointer.getNextNonWhiteChar();
			if (chr=='"') {
				return JString.zParse(this,false);
			} else if (chr=='-' || (chr>='0' && chr<='9')) {
				return JNumber.zParse(this,chr);
			} else if (chr=='{') {
				return JSON.zParse(this,false,false,false);
			} else if (chr=='[') {
				return JArray.zParse(this,false);
			} else if (chr=='t' || chr=='f') {
				return JBoolean.zParse(this,(chr=='t'));
			} else if (chr=='n') {
				return JNull.zParse(this);
			} else {
				throw ParseException.forInvalidCharacter("Invalid character '"+chr+"' when expecting start of any value",this);
			}
		}

		internal string getParseDebugIDForExceptions() {
			return settings.DebugIDForExceptions;
		}

	}

}
