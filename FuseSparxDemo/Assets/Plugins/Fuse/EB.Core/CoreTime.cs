namespace EB
{
	// a time class that sync'd with the server (in UTC)
	public static class Time
	{
		private static int 				_offset = -1;
		private static System.DateTime 	_epoc = new System.DateTime(1970, 1, 1);
		
		public static bool Valid
		{
			get { return _offset > 0; }
		}
		
		public static int Now
		{
			get
			{
				return GetNow();
			}
			set
			{	
				SetNow(value);
			}
		}
		
		private static int GetNow()
		{
			if ( !Valid )
			{
				return ToPosixTime(System.DateTime.UtcNow); 
			}
			try {
				return UnityEngine.Mathf.FloorToInt(UnityEngine.Time.realtimeSinceStartup) + _offset;	
			}
			catch {
				return ToPosixTime(System.DateTime.UtcNow); 
			}
		}
		
		public static int Since( System.DateTime dt )
		{
			var span = System.DateTime.Now - dt;
			var diff = (int)span.TotalSeconds;
			return UnityEngine.Mathf.Max(0, diff);
		}
		
		public static int Since( int t )
		{
			return UnityEngine.Mathf.Max(0, Now-t);
		}
		
		private static void SetNow(int value)
		{
			_offset = value - (int)UnityEngine.Mathf.FloorToInt(UnityEngine.Time.realtimeSinceStartup); 
		}
				

		public static int ToPosixTime( System.DateTime time )
        {
            var span = time - _epoc;
            return (int)span.TotalSeconds;
        }

        public static System.DateTime FromPosixTime(int time)
        {
			return _epoc + System.TimeSpan.FromSeconds(time);
        }
		
		public static float deltaTime {get { return UnityEngine.Time.deltaTime; }}
		public static float timeScale {get { return UnityEngine.Time.timeScale; } set { UnityEngine.Time.timeScale = value; } }
		public static float realtimeSinceStartup {get { return UnityEngine.Time.realtimeSinceStartup; }}
	}
}

