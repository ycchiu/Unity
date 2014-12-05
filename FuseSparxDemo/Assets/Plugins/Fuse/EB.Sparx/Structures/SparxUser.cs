using UnityEngine;
using System.Collections;

namespace EB.Sparx
{
	public class User
	{
		public Id Id {get;private set;}
		public string Name {get;private set;}
		public bool HasName {get{return !string.IsNullOrEmpty(Name);}}
		
		public string Email {get;private set;}
		public bool HasEmail {get{return !string.IsNullOrEmpty(Email);}}
		
		public string FacebookId {get;private set;}
		public bool HasFacebookId {get{return !string.IsNullOrEmpty(FacebookId);}}
		
		public bool IsGuest { get{ return !HasEmail; } }
		
		public string GameCenterId {get;private set;}
		
		public int CohortDate  	{get; private set;}
		public int RevenueDate 	{get; private set;}
		public int LoginDate 	{get; private set;}
		public int Level 		{get; private set;}
		
		public int Revenue 		{get; private set;}
		public string Naid 		{get; private set;}
		
		public User(Id id)
		{
			Id = id;
			Name = string.Empty;
			Email = string.Empty;
			GameCenterId = string.Empty;
		}
		
		public void Update( object data )
		{
			Name 		= Dot.String("name", data, Name);  
			Email 		= Dot.String("email", data, Email);
			GameCenterId= Dot.String("gcid", data, GameCenterId);
			FacebookId  = Dot.String("fbid", data, FacebookId);
			CohortDate 	= Dot.Integer("cohort", data, CohortDate);
			Revenue 	= Dot.Integer("revenue", data, Revenue);
			RevenueDate = Dot.Integer("time_revenue", data, RevenueDate);
			LoginDate 	= Dot.Integer("time_last", data, LoginDate);
			Level 		= Dot.Integer("level", data, Level);
			Naid 		= Dot.String("naid", data, Naid);
		}
		
		Hashtable ToHashtable()
		{
			Hashtable data = new Hashtable();
			data["uid"] = Id;
			data["name"] = Name;
			return data;
		}
		
		public void Load( string key )  
		{
			Update( SecurePrefs.GetJSON(key) ); 
		}
		
		public void Save( string key )
		{
			SecurePrefs.SetJSON( key, ToHashtable() );   
		}
		
	}
}

