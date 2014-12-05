using UnityEngine;
using System.Collections;
using System;

namespace EB
{

	public static class Flag
	{
		public static bool IsSet<EnumType>(EnumType val, EnumType flag) 
		{
			return ((Convert.ToInt32(val) & Convert.ToInt32(flag)) != 0);
		}
		
		public static EnumType Set<EnumType>(ref EnumType val, EnumType flag)
		{
			val = Set<EnumType>(val, flag);
			return val;
		}

		public static EnumType Set<EnumType>(EnumType val, EnumType flag) 
		{
			val = (EnumType)(object)(Convert.ToInt32(val) | Convert.ToInt32(flag));
			return val;
		}		
						
		public static EnumType Unset<EnumType>(ref EnumType val, EnumType flag)
		{
			val = Unset<EnumType>(val, flag);
			return val;
		}
		
		public static EnumType Unset<EnumType>(EnumType val, EnumType flag) 
		{
			val = (EnumType)(object)(Convert.ToInt32(val) & ~Convert.ToInt32(flag));
			return val;
		}
		
		public static EnumType Set<EnumType>(ref EnumType val, EnumType flag, bool set)
		{
			val = set ? Set(ref val, flag) : Unset(ref val, flag);
			return val;
		}
		
		public static EnumType Set<EnumType>(EnumType val, EnumType flag, bool set) 
		{
			val = set ? Set(ref val, flag) : Unset(ref val, flag);
			return val;
		}
	}

}
