using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EB
{	
	public static class ProfanityFilter 
	{
		private static List<string> _naughtyList = new List<string>();
		
		public static void Init( string words ) 
		{
			foreach( var word in words.Split(new char[]{'\n','\r'}, System.StringSplitOptions.RemoveEmptyEntries) )
			{
				_naughtyList.Add(word);	
			}
		}
		
		public static  bool IsOk( string text )
		{
			foreach (string word in _naughtyList)
            {
				string wordNoSpaces = word.Replace(" ","");
				if ( text.IndexOf(word, System.StringComparison.OrdinalIgnoreCase)>= 0 ||
					 text.IndexOf(wordNoSpaces, System.StringComparison.OrdinalIgnoreCase)>= 0 	
					)
				{
					return false;
				}
			}
			return true;
		}
		
		public static  string Filter( string text )
		{
			string censoredText = text;

            foreach (string word in _naughtyList)
            {
                string replacement = new string('*', word.Length);
                int index = 0;

                // TODO: handle the space's... ie F U C K;

                while ( (index = censoredText.IndexOf(word, System.StringComparison.OrdinalIgnoreCase)) >= 0 )
                {
                    censoredText = censoredText.Substring(0, index) + replacement + censoredText.Substring(index + replacement.Length);
                }
            }

            return censoredText;
		}
	}
}


