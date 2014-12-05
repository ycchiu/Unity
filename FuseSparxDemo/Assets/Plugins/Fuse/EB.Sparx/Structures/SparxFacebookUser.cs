using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class FacebookUser
	{
		public Id UserId {get;private set;}
		public string FacebookId {get;private set;}
		public string FirstName {get;private set;}
		public string LastName {get;private set;}
		public string Email {get; private set;}
		public string Locale {get; private set;}
		
		public bool IsAppUser { get; private set; }
		
		public string GameCenterId {get;private set;}
				
		public FacebookUser( string fbId )
		{
			FacebookId = fbId;
		}
		
		public void Update( object data )
		{
			UserId = new Id( Dot.Find("uid", data ) );
			FirstName = Dot.String("first_name", data, FirstName);
			LastName = Dot.String("last_name", data, LastName);
			IsAppUser = Dot.Bool("is_app_user", data, IsAppUser);
			Email = Dot.String("email", data, string.Empty);
			Locale = Dot.String("locale", data, string.Empty);
		}
		
		public enum ImageType
		{
			square,  	// (50x50)
			small,		// (50 pixels wide, variable height)
			normal,		// (100 pixels wide, variable height)
			large,		// (about 200 pixels wide, variable height)
		}
		
		public string GetImageUrl( ImageType type )
		{
			return "https://graph.facebook.com/"+FacebookId+"/picture?return_ssl_resources=1&type="+type.ToString();
		}
		
		public static string GetImageUrlForUser(string fbId, ImageType type)
		{
			return "https://graph.facebook.com/"+fbId+"/picture?return_ssl_resources=1&type="+type.ToString();
		}
		
		public static string GetImageUrlForUser(string fbId, int width, int height)
		{
			return "https://graph.facebook.com/"+fbId+"/picture?return_ssl_resources=1&width=" + width.ToString() + "&height=" + height.ToString();
		}
	}
}

