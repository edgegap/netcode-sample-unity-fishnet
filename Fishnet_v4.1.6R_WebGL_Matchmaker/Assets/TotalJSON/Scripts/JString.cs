//  JValue - JString


using System;
using System.Text;
using System.Collections.Generic;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {
	
	/// <summary>
	/// Class to store string value in JSON format. Once JString instance is created, its value can't be changed.
	/// </summary>
	public class JString : JValue { // , IEquatable<JString> {

		private const string HEX="0123456789ABCDEF";

		private string stringValue;

		/// <summary>
		/// Creates new instance of JString object.
		/// 
		/// Parameter can't be null. If you wish to add null to JSON or JArray object, create <code>new JNull()</code> and add that one.
		/// </summary>
		/// <param name="stringValue">
		/// C# string value to be stored in this object.
		/// </param>
		/// <exception cref="JArgumentNullException">
		/// If parameter is null.
		/// </exception>
		public JString(string stringValue) : base() {
			if (stringValue==null) {
				throw (new JArgumentNullException("stringValue","Parameter can not be null in constructor JString.<init>(string)"));
			}
			this.stringValue=stringValue;
		}

		public override string ToString() {
			int length = stringValue.Length;
			if (length==0) {
				return ("[JString: Empty string]");
			} else {
				return ("[JString: \""+stringValue+"\" ("+length+" character"+(length>1?"s":"")+")]");
			}
		}

        /// <summary>
        /// Test if another object equals to this object. Always returns false if parameter object is null or it is not instance of JString.
        /// Two JString objects are equal if both contains exactly same string.
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
			if (!(anotherObject is JString)) {
				return false;
			}
			JString anotherJString=(JString)(anotherObject);
			return (stringValue.Equals(anotherJString.AsString()));
		}

        public override int GetHashCode() {
			return stringValue.GetHashCode();
		}

		/// <summary>
		/// Get value of this JSON string as c# system string.
		/// </summary>
		/// <returns>
		/// c# string value, can not be null.
		/// </returns>
		public string AsString() {
			return stringValue;
		}

		internal override void zCreate(CreateStringRunner createStringRunner) {
			encode(createStringRunner,stringValue);
		}

		internal static void encode(CreateStringRunner createStringRunner, string str) {
			createStringRunner.append('"');
			foreach (char chr in str) {
				if (chr=='"') {
					createStringRunner.append("\\\"");
				} else if (chr=='\\') {
					createStringRunner.append("\\\\");
				} else if (chr=='/' && createStringRunner.isEscapeForwardSlashes()) {
					createStringRunner.append("\\/");
				} else if (chr=='\b') {
					createStringRunner.append("\\b");
				} else if (chr=='\f') {
					createStringRunner.append("\\f");
				} else if (chr=='\n') {
					createStringRunner.append("\\n");
				} else if (chr=='\r') {
					createStringRunner.append("\\r");
				} else if (chr=='\t') {
					createStringRunner.append("\\t");
				} else if (chr<32 || chr>126) { // } else if (chr<32 || (chr>126 && createStringRunner.isEncodeNon7bitCharacters())) {
					if (chr<16) {
						createStringRunner.append("\\u000");
						createStringRunner.append(HEX[chr]);
					} else if (chr<256) {
						createStringRunner.append("\\u00");
						createStringRunner.append(HEX[chr/16]);
						createStringRunner.append(HEX[chr%16]);
					} else if (chr<4096) {
						createStringRunner.append("\\u0");
						createStringRunner.append(HEX[chr/256]);
						createStringRunner.append(HEX[(chr%256)/16]);
						createStringRunner.append(HEX[chr%16]);
					} else {
						createStringRunner.append("\\u");
						createStringRunner.append(HEX[chr/4096]);
						createStringRunner.append(HEX[(chr%4096)/256]);
						createStringRunner.append(HEX[(chr%256)/16]);
						createStringRunner.append(HEX[chr%16]);
					}
				} else {
					createStringRunner.append(chr);
				}
			}
			createStringRunner.append('"');
		}

		internal static JString zParse(ParseStringRunner parseStringRunner, bool expectStartOfString) {

			StringPointer sp = parseStringRunner.getStringPointer();

			if (expectStartOfString) {
				char chr;
				if (!sp.tryGetNextNonWhiteChar(out chr)) {
					throw ParseException.forInvalidStart("Parameter string didn't contain any non-white characters",parseStringRunner);
				}
				if (chr!='"') {
					throw ParseException.forInvalidStart("Invalid character '"+chr+"' when expecting start of string '\"'",parseStringRunner);
				}
			}

			return (new JString(decode(parseStringRunner)));

		}

		internal static string decode(ParseStringRunner parseStringRunner) {

			// At this point, StringPointer has passed starting "
			StringPointer sp = parseStringRunner.getStringPointer();

			StringBuilder sb=new StringBuilder();
			do {
				char chr=sp.getNextChar();
				if (chr=='"') {
					return sb.ToString();
				}
				if (chr=='\\') {
					chr=sp.getNextChar();
					if (chr=='"' || chr=='\\' || chr=='/') {
						// 'chr' works as is
					} else if (chr=='b') {
						chr='\b';
					} else if (chr=='f') {
						chr='\f';
					} else if (chr=='n') {
						chr='\n';
					} else if (chr=='r') {
						chr='\r';
					} else if (chr=='t') {
						chr='\t';
					} else if (chr=='u') {
						int ucode=0;
						for (int n=0; n<4; n++) {
							chr=sp.getNextChar();
							int uvalue=HEX.IndexOf(char.ToUpperInvariant(chr));
							if (uvalue<0) {
								throw ParseException.forInvalidCharacter("Invalid hexadecimal character '"+chr+"' after '\\u' in string value",parseStringRunner);
							}
							ucode=ucode*16+uvalue;
						}
						chr=(char)(ucode);
					} else {
						throw ParseException.forInvalidCharacter("Invalid character '"+chr+"' after '\\' in string value",parseStringRunner);
					}
				}
				sb.Append(chr);
			} while (true);

		}

		internal override object zDeserialize(Type type, string toFieldName, DeserializeSettings deserializeSettings) {

			if (type==typeof(string)) {
				return this.AsString();
			}

			if (type==typeof(object) && deserializeSettings.AllowFieldsToBeObjects) {
				return this.AsString();
			}

			throw (DeserializeException.forNonMatchingType(this,type,toFieldName));

		}

	}

}
