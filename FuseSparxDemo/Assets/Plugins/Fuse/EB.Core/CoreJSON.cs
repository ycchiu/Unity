using UnityEngine;
using System.Collections;
using System.Text;

namespace EB
{	
	public static class JSON
	{
		public class Fragment
		{			
			public string JsonString { get; private set; }
			
			public Fragment(string json)
			{
				JsonString = json;
			}
		}
		
	    enum Token
	    {
	        None,
	        CurlyOpen,
	        CurlyClose,
	        SquaredOpen,
	        SquaredClose,
	        Colon,
	        Comma,
	        String,
	        Number,
	        True,
	        False,
	        Null
	    }
	
	    const int BUILDER_CAPACITY = 2048;
		
	    /// <summary>
	    /// Parses the string json into a value
	    /// </summary>
	    /// <param name="json">A JSON string.</param>
	    /// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
	    public static object Parse(string json)
	    {
	        // save the string for debug information	
	        if (json != null)
	        {
	            char[] charArray = json.ToCharArray();
	            int index = 0;
	            bool success = true;
	            object value = ParseValue(charArray, ref index, ref success);
	            return success ? value : null;
	        }
	        else
	        {
	            return null;
	        }
	    }
	
	    /// <summary>
	    /// Converts a Hashtable / ArrayList object into a JSON string
	    /// </summary>
	    /// <param name="json">A Hashtable / ArrayList</param>
	    /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
	    public static string Stringify(object json)
	    {
	        StringBuilder builder = new StringBuilder(BUILDER_CAPACITY);
	        bool success = SerializeValue(json, builder);
	        return success ? builder.ToString() : null;
	    }
	
	    static Hashtable ParseObject(char[] json, ref int index)
	    {
	        Hashtable table = new Hashtable();
	        Token token;
	
	        // {
	        NextToken(json, ref index);
	
	        bool done = false;
	        while (!done)
	        {
	            token = LookAhead(json, index);
	
	            switch (token)
	            {
	                case Token.None:
	                    return null;
	            }
	
	            if (token == Token.None)
	            {
	                return null;
	            }
	            else if (token == Token.Comma)
	            {
	                NextToken(json, ref index);
	            }
	            else if (token == Token.CurlyClose)
	            {
	                NextToken(json, ref index);
	                return table;
	            }
	            else
	            {
	                // name
	                string name = ParseString(json, ref index);
	
	                if (name == null)
	                {
	                    return null;
	                }
	
	                // :
	                token = NextToken(json, ref index);
	
	                if (token != Token.Colon)
	                {
	                    return null;
	                }
	
	                // value
	                bool success = true;
	                object value = ParseValue(json, ref index, ref success);
	
	                if (!success)
	                {
	                    return null;
	                }
	
	                table[name] = value;
	            }
	        }
	
	        return table;
	    }
	
	    static ArrayList ParseArray(char[] json, ref int index)
	    {
	        ArrayList array = new ArrayList();
	
	        // [
	        NextToken(json, ref index);
	
	        bool done = false;
	        while (!done)
	        {
	            Token token = LookAhead(json, index);
	            if (token == Token.None)
	            {
	                return null;
	            }
	            else if (token == Token.Comma)
	            {
	                NextToken(json, ref index);
	            }
	            else if (token == Token.SquaredClose)
	            {
	                NextToken(json, ref index);
	                break;
	            }
	            else
	            {
	                bool success = true;
	                object value = ParseValue(json, ref index, ref success);
	
	                if (!success)
	                {
	                    return null;
	                }
	
	                array.Add(value);
	            }
	        }
	
	        return array;
	    }
	
	    static object ParseValue(char[] json, ref int index, ref bool success)
	    {
	        switch (LookAhead(json, index))
	        {
	            case Token.String:
	                return ParseString(json, ref index);
	
	            case Token.Number:
	                return ParseNumber(json, ref index);
	
	            case Token.CurlyOpen:
	                return ParseObject(json, ref index);
	
	            case Token.SquaredOpen:
	                return ParseArray(json, ref index);
	
	            case Token.True:
	                NextToken(json, ref index);
	                return (object)true;
	
	            case Token.False:
	                NextToken(json, ref index);
	                return (object)false;
	
	            case Token.Null:
	                NextToken(json, ref index);
	                return null;
	
	            case Token.None:
	                break;
	        }
	
	        success = false;
	        return null;
	    }
	
	    static string ParseString(char[] json, ref int index)
	    {
	        char c;
	
	        EatWhitespace(json, ref index);
	
	        // "
	        c = json[index];
			index += 1;
			
			var tmp = new System.Collections.Generic.List<char>();
			
			int surrogate = 0;
			
	        bool complete = false;
	        while (!complete)
	        {
	            if (index == json.Length)
	            {
	                break;
	            }
	
	            c = json[index];
				index += 1;
	            if (c == '"')
	            {
	                complete = true;
	                break;
	            }
	            else if (c == '\\')
	            {
	                if (index == json.Length)
	                {
	                    break;
	                }
	
	                c = json[index];
					index += 1;
	                if (c == '"')
	                {
	                    tmp.Add('"');
	                }
	                else if (c == '\\')
	                {
	                    tmp.Add('\\');
	                }
	                else if (c == '/')
	                {
	                    tmp.Add('/');
	                }
	                else if (c == 'b')
	                {
	                    tmp.Add('\b');
	                }
	                else if (c == 'f')
	                {
	                    tmp.Add('\f');
	                }
	                else if (c == 'n')
	                {
	                    tmp.Add('\n');
	                }
	                else if (c == 'r')
	                {
	                    tmp.Add('\r');
	                }
	                else if (c == 't')
	                {
	                    tmp.Add('\t');
	                }
	                else if (c == 'u')
	                {
	                    int remainingLength = json.Length - index;
	
	                    if (remainingLength >= 4)
	                    {
	                        // fetch the next 4 chars
	                        char[] unicodeCharArray = new char[4];
	                        System.Array.Copy(json, index, unicodeCharArray, 0, 4);
	                        // parse the 32 bit hex into an integer codepoint
							int codePoint = 0;
							for ( int i = 0; i < unicodeCharArray.Length; ++i )
							{
								codePoint *= 16;
								
								var v  = unicodeCharArray[i];
								if ( v >= 'A' && v <= 'F' )
								{
									codePoint += 10 + (v - 'A');
								}
								else if ( v >= 'a' && v <= 'f' )
								{
									codePoint += 10 + (v - 'a');
								}
								else if ( v >= '0' && v <= '9' )
								{
									codePoint += (v - '0');
								}
							}
							
							// convert the integer codepoint to a unicode char and add to string
							if ( codePoint < 0xD7FF || codePoint >= 0xE000 )
							{
								tmp.Add( (char)codePoint);
								surrogate = 0;
							}
							else if (surrogate != 0 && char.IsLowSurrogate( (char)codePoint ) )
							{
								tmp.Add( (char)surrogate );
								tmp.Add( (char)codePoint );
								surrogate  = 0;
							}
							else if (char.IsHighSurrogate( (char)codePoint) ) 
							{
								surrogate = codePoint;
							}
							else
							{
								// crap
								surrogate = 0;
							}
							
	                        // skip 4 chars
	                        index += 4;
	                    }
	                    else
	                    {
	                        break;
	                    }
	                }
	            }
	            else
	            {
					tmp.Add(c);
	            }
	        }
	
	        if (!complete)
	        {
	            return null;
	        }
	
	        return new string( tmp.ToArray() );
	    }
	
	    static double ParseNumber(char[] json, ref int index)
	    {
	        EatWhitespace(json, ref index);
	
	        int lastIndex = GetLastIndexOfNumber(json, index);
			int startIndex = index;
			index = lastIndex + 1;
			
			var str = new string(json, startIndex, lastIndex-startIndex+1);
			return double.Parse(str);
	    }
	
	    static int GetLastIndexOfNumber(char[] json, int index)
	    {
	        int lastIndex;
	        for (lastIndex = index; lastIndex < json.Length; ++lastIndex)
	        {
	            if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
	            {
	                break;
	            }
	        }
	        return lastIndex - 1;
	    }
	
	    static void EatWhitespace(char[] json, ref int index)
	    {
	        for (; index < json.Length; index+=1)
	        {
	            if (" \t\n\r".IndexOf(json[index]) == -1)
	            {
	                break;
	            }
	        }
	    }
	
	    static Token LookAhead(char[] json, int index)
	    {
	        int saveIndex = index;
	        return NextToken(json, ref saveIndex);
	    }
	
	    static Token NextToken(char[] json, ref int index)
	    {
	        EatWhitespace(json, ref index);
	
	        if (index == json.Length)
	        {
	            return Token.None;
	        }
	
	        char c = json[index];
	        index += 1;
	        switch (c)
	        {
	            case '{':
	                return Token.CurlyOpen;
	            case '}':
	                return Token.CurlyClose;
	            case '[':
	                return Token.SquaredOpen;
	            case ']':
	                return Token.SquaredClose;
	            case ',':
	                return Token.Comma;
	            case '"':
	                return Token.String;
	            case '0':
	            case '1':
	            case '2':
	            case '3':
	            case '4':
	            case '5':
	            case '6':
	            case '7':
	            case '8':
	            case '9':
	            case '-':
	                return Token.Number;
	            case ':':
	                return Token.Colon;
	        }
	        index--;
	
	        int remainingLength = json.Length - index;
	
	        // false
	        if (remainingLength >= 5)
	        {
	            if (json[index] == 'f' &&
	                json[index + 1] == 'a' &&
	                json[index + 2] == 'l' &&
	                json[index + 3] == 's' &&
	                json[index + 4] == 'e')
	            {
	                index += 5;
	                return Token.False;
	            }
	        }
	
	        // true
	        if (remainingLength >= 4)
	        {
	            if (json[index] == 't' &&
	                json[index + 1] == 'r' &&
	                json[index + 2] == 'u' &&
	                json[index + 3] == 'e')
	            {
	                index += 4;
	                return Token.True;
	            }
	        }
	
	        // null
	        if (remainingLength >= 4)
	        {
	            if (json[index] == 'n' &&
	                json[index + 1] == 'u' &&
	                json[index + 2] == 'l' &&
	                json[index + 3] == 'l')
	            {
	                index += 4;
	                return Token.Null;
	            }
	        }
	
	        return Token.None;
	    }
	
	    static bool SerializeObject(object obj, StringBuilder builder)
	    {
			var enumerator = AOT.GetDictionaryEnumerator(obj);
			if (enumerator == null )
			{
				EB.Debug.LogError("Object does not implement IDictionaryEnumerator interface: " + obj.GetType());
				return false;				
			}

	        builder.Append("{");
			
	        bool first = true;
			bool result = true;
	        while (enumerator.MoveNext())
	        {
	            string key = enumerator.Key.ToString();
	            object value = enumerator.Value;
	
	            if (!first)
	            {
	                builder.Append(",");
	            }
	
	            SerializeString(key, builder);
	            builder.Append(":");
	
	            if (!SerializeValue(value, builder))
	            {
					builder.Append("null");
	                result = false;
	            }
	
	            first = false;
	        }
			
			// cleanup
			if (enumerator is System.IDisposable )
			{
				var disposable = (System.IDisposable)enumerator;
				disposable.Dispose();
			}		
	
	        builder.Append("}");
			
	        return result;
	    }
	
	    static bool SerializeArray(object arr, StringBuilder builder)
	    {
			var enumerator = AOT.GetEnumerator(arr);
			if (enumerator == null )
			{
				EB.Debug.LogError("Object does not implement IEnumerable interface: " + arr.GetType());
				return false;				
			}
			
			builder.Append("[");
					
			bool result = true;
			bool first = true;
			while (enumerator.MoveNext())
			{
				object value = enumerator.Current;
	
	            if (!first)
	            {
	                builder.Append(",");
	            }
	
	            if (!SerializeValue(value, builder))
	            {
	                builder.Append("null");
	                result = false;
	            }
	
	            first = false;
			}
			
			// cleanup
			if (enumerator is System.IDisposable )
			{
				var disposable = (System.IDisposable)enumerator;
				disposable.Dispose();
			}
			
			builder.Append("]");
			
	        return result;
	    }
	
	    static bool SerializeValue(object value, StringBuilder builder)
	    {			
			if (value == null)
	        {
	            builder.Append("null");
				return true;
	        }
			else if ( value is Fragment)
			{
				var fragment = (Fragment)value;
				builder.Append(fragment.JsonString);
				return true;	
			}
			else if ( value is string )
			{
				SerializeString((string)value, builder);
				return true;	
			}
			else if ( value is IDictionary )
			{
				return SerializeObject( value, builder );
			}
			else if ( value is ICollection )
			{
				return SerializeArray( value, builder );
			}
			else if ( value is System.Enum )
			{
				var n = System.Convert.ChangeType(value, typeof(int) );
				SerializeNumber(n.ToString(), builder);	
				return true;	
			}
			else if ( value is bool )
			{
				bool b = (bool)value;
	            builder.Append( b ? "true" : "false");
				return true;
			}
			else if ( IsNumeric(value))
			{
				 SerializeNumber(value.ToString(), builder);
				return true;
			}
						
			EB.Debug.LogError("Failed to serialize type: " + value.GetType() );
	        return false;
	    }
	
	    static void SerializeString(string aString, StringBuilder builder)
	    {
	        builder.Append("\"");
	
	        char[] charArray = aString.ToCharArray();
	
	        for (int i = 0; i < charArray.Length; ++i)
	        {
	            char c = charArray[i];
	            if (c == '"')
	            {
	                builder.Append("\\\"");
	            }
	            else if (c == '\\')
	            {
	                builder.Append("\\\\");
	            }
	            else if (c == '\b')
	            {
	                builder.Append("\\b");
	            }
	            else if (c == '\f')
	            {
	                builder.Append("\\f");
	            }
	            else if (c == '\n')
	            {
	                builder.Append("\\n");
	            }
	            else if (c == '\r')
	            {
	                builder.Append("\\r");
	            }
	            else if (c == '\t')
	            {
	                builder.Append("\\t");
	            }
	            else
	            {
					var codepoint = (int)c;
	                if ((codepoint >= 32) && (codepoint <= 126))
	                {
	                    builder.Append(c);
	                }
	                else
	                {
	                    builder.AppendFormat("\\u{0:X4}", codepoint);
	                }
	            }
	        }
	
	        builder.Append("\"");
	    }
		
	    static void SerializeNumber(string number, StringBuilder builder)
	    {
	        builder.Append(number);
	    }
	
	    /// <summary>
	    /// Determines if a given object is numeric in any way
	    /// (can be integer, double, etc). C# has no pretty way to do this.
	    /// </summary>
	   	static bool IsNumeric(object o)
	    {
			if( o != null )
			{
				double result;
				return double.TryParse(o.ToString(), out result);
			}
			return false;
	    }
	}	
}

