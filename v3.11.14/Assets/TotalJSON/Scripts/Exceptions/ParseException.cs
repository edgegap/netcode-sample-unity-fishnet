//  ParseException


using System;
using Leguar.TotalJSON.Internal;

namespace Leguar.TotalJSON {

	/// <summary>
	/// Exception thrown if parsing string to JSON or JArray fails.
	/// </summary>
	public class ParseException : ArgumentException {

		private ParseException(string message) : base(message) {
		}

		internal static ParseException forEmpty(String message, ParseStringSettings parseStrignSettings) {
			string fullMessage=message+getExceptionMessageTail(parseStrignSettings);
			return (new ParseException(fullMessage));
		}

		internal static ParseException forInvalidStart(String message, ParseStringRunner parseStrignRunner) {
			StringPointer sp=parseStrignRunner.getStringPointer();
			string fullMessage=message+" - "+sp.getLineAndColumnForException()+getExceptionMessageTail(parseStrignRunner);
			return (new ParseException(fullMessage));
		}

		internal static ParseException forInvalidCharacter(String message, ParseStringRunner parseStrignRunner) {
			StringPointer sp=parseStrignRunner.getStringPointer();
			string fullMessage=message+" - "+sp.getLineAndColumnForException()+", near: "+sp.getSubStringForException(32)+getExceptionMessageTail(parseStrignRunner);
			return (new ParseException(fullMessage));
		}

		internal static ParseException forInvalidEnd(ParseStringRunner parseStrignRunner) {
			StringPointer sp=parseStrignRunner.getStringPointer();
			string fullMessage="Unexpected end of input - "+sp.getLineAndColumnForException()+", near: "+sp.getSubStringForException(16)+getExceptionMessageTail(parseStrignRunner);
			return (new ParseException(fullMessage));
		}

		internal static ParseException forCharactersAfterEnd(ParseStringRunner parseStrignRunner) {
			StringPointer sp=parseStrignRunner.getStringPointer();
			string fullMessage="Unexpected non-white character after end of object - "+sp.getLineAndColumnForException()+", near: "+sp.getSubStringForException(32)+getExceptionMessageTail(parseStrignRunner);
			return (new ParseException(fullMessage));
		}

		private static string getExceptionMessageTail(ParseStringRunner parseStringRunner) {
			if (parseStringRunner!=null) {
				return getExceptionMessageTailForID(parseStringRunner.getParseDebugIDForExceptions());
			}
			return getExceptionMessageTailForID(null);
		}

		internal static string getExceptionMessageTail(ParseStringSettings parseStringSettings) {
			if (parseStringSettings!=null) {
				return getExceptionMessageTailForID(parseStringSettings.DebugIDForExceptions);
			}
			return getExceptionMessageTailForID(null);
		}

		internal static string getExceptionMessageTailForID(string debugIDForExceptions) {
			return InternalTools.getExceptionMessageTailForID(debugIDForExceptions,"Parse");
		}

		public override string StackTrace {
			get {
				return InternalTools.getCleanedStackTrace(base.StackTrace);
			}
		}

	}

}
