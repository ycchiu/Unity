using System;
using System.Xml.Serialization;

#if UNITY_EDITOR
// Load and Save the OtherLevels SDK Preferences
[Serializable]
public class OtherLevelsPreferences
{
	public bool 	enabled; 
	public string 	appKey;
	public bool push_enabled;

	public OtherLevelsPreferences()
	{
		// Enable the OtherLevels SDK
		enabled = true;
		// Your application key (String) from your OtherLevels.com account
		appKey = "";
		// Enable the OtherLevels SDK with Push
		push_enabled = true;
	}
	
	public void Save()
	{
		string path = "./OtherLevelsPreferences.xml";
		System.IO.Stream fileStream = new System.IO.FileStream(path, System.IO.FileMode.Create);
		
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(OtherLevelsPreferences));
		xmlSerializer.Serialize(fileStream, this);
		fileStream.Close();
	}
	
	public static OtherLevelsPreferences Load()
	{
		string path = "./OtherLevelsPreferences.xml";
		System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);
		if(!fileInfo.Exists)
		{
			return new OtherLevelsPreferences();
		}
		
		System.IO.Stream fileStream = new System.IO.FileStream(path, System.IO.FileMode.Open);
		
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(OtherLevelsPreferences));
		var preferences = (OtherLevelsPreferences)xmlSerializer.Deserialize(fileStream);
		fileStream.Close();
		
		return preferences;
	}
}
#endif
