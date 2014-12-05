using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public enum SubSystemState
	{
		Disconnected,
		Connecting,
		Connected,
		Error,
	}
	
	public abstract class SubSystem : Manager
	{
		public SubSystemState State {get;set;}
		
		public abstract void Connect();
		public abstract void Disconnect(bool isLogout);
				
		protected void FatalError( string error ) 
		{
			EB.Debug.LogError("SubSystem Error: {0}", error );
			State = SubSystemState.Error;
			Hub.FatalError( error);
		}
	}
}
