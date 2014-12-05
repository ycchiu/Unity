using UnityEngine;
using System.Collections;

namespace EB.GameTalk.RPC
{
	
	public class Global
	{
		[GameTalkRPC]
		public void RegisterName(ArrayList args, Action<string,object> cb)
		{
			EB.Debug.Log( "Global.RegisterName" + args[0] );
			cb(null,"This is a test");	
		}
	}
	
}

