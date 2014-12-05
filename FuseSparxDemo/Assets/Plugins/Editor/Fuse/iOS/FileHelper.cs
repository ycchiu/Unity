using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EB.Editor
{
	public class FileHelper
	{		
		protected List<string> _lines;
		protected string 		 _path;
		
		public FileHelper( string path )
		{
			_path	  = path;
			var lines = File.ReadAllLines(path);
			_lines = new List<string>(lines);
		}
				
		public void Save()
		{
			Save(_path);
		}
		
		public void Save( string path )
		{
			var stringBuilder = new System.Text.StringBuilder();
			foreach( var line in _lines )
			{
				stringBuilder.AppendLine(line);
			}
			
			var bytes = Encoding.GetBytes( stringBuilder.ToString() ); 
			File.WriteAllBytes( path, bytes);
		}
		
		public void InsertLine( string section, int offset, string line )
		{
			var index = FindSection( section );
			_lines.Insert(index+offset, line);
		}
		
		public void InsertLines( string section, int offset, string[] lines )
		{
			var index = FindSection( section );
			_lines.InsertRange(index+offset, lines);
		}
		
		public void RemoveLines( string search )
		{
			while (true)
			{
				var index = FindSection(search);
				if ( index < 0 )
				{
					return;
				}
				_lines.RemoveAt(index);
			}
		}
		
		public int FindSection( string section )
		{
			for( int i = 0; i < _lines.Count; ++i )
			{
				if ( _lines[i].Contains(section) )
				{
					return i;
				}
			}
			return -1;
		}
		
		public void ReplaceAll( string find, string replace )
		{
			for( int i = 0; i < _lines.Count; ++i )
			{
				_lines[i] = _lines[i].Replace(find,replace);
			}
		}
		
	}
}
	