using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EB
{
	public static class Util
	{
		public static T[] GetEnumValues<T>()
		{
			return (T[])System.Enum.GetValues(typeof(T));
		}
		
		public static T GetEnumValueFromString<T>(string s)
		{
			
			try
			{
				return (T)System.Enum.Parse( typeof(T), s, true );
			}
			catch
			{
				return default( T );
			}
		}
		
		public static Hashtable FromList( params object[] list )
		{
			if ( (list.Length&1) == 1 ) 
			{
				throw new System.Exception("List must be even length!");
			}
			var ht = new Hashtable();
			var count = list.Length / 2;
			for( var i = 0; i < count; ++i )
			{
				ht.Add( list[i*2], list[i*2+1] );
			}
			return ht;
		}
		
		public static Rect RectFromPointSize( Vector2 pt, Vector2 size ) 
		{
			var half = size*0.5f;
			return new Rect(pt.x-half.x,pt.y-half.y, size.x,size.y);
		}
		
		public static Vector2 ScreenSize 
		{
			get { return new Vector2(Screen.width, Screen.height); }	
		}
		
		public static Vector2 ScreenCenter 
		{
			get { return ScreenSize*0.5f; }	
		}
		
		public static void RecursiveOp(GameObject obj, EB.Function<bool,GameObject> fnObj)
		{
			if (obj != null && fnObj(obj))
			{
				foreach (Transform t in obj.transform)
				{
					RecursiveOp(t.gameObject, fnObj);
				}
			}
		}

		public static void SetActiveRecursive(GameObject obj, bool active)
		{
			if(obj != null)
			{
				obj.SetActive( active );
				foreach(Transform t in obj.transform)
				{
					SetActiveRecursive(t.gameObject, active);
				}
			}
		}
		
		public static void SetActiveRecursive(GameObject obj, string name, bool active)
		{
			SetActiveRecursive( GetObject(obj, name, true), active); 
		}
		
		public static void SetLayerRecursive(GameObject obj, string name, int layer)
		{
			SetLayerRecursive( GetObject(obj,name), layer );    
		}
		
		public static void SetLayerRecursive(GameObject obj, int layer)
		{
			if (obj != null)
			{
				obj.layer = layer;
				foreach (Transform t in obj.transform)
				{
					SetLayerRecursive(t.gameObject, layer);
				}
			}
		}
		
		public static void SetLayerRecursive(GameObject obj, int layer, int checkLayer)
		{
			if (obj != null && obj.layer == checkLayer)
			{
				obj.layer = layer;
				foreach (Transform t in obj.transform)
				{
					SetLayerRecursive(t.gameObject, layer, checkLayer);
				}
			}
		}

		public static void SetLayerRecursiveCheckRoot(GameObject obj, string name, int layer)
		{
			if (obj != null && obj.layer != layer)
			{
				SetLayerRecursive(obj, name, layer);
			}
		}
		
		public static void SetLayerRecursiveCheckRoot(GameObject root, int layer)
		{
			if (root != null && root.layer != layer)
			{
				SetLayerRecursive(root, layer);
			}
		}

		public static T FindComponent<T>( GameObject obj, string name ) where T : Component
		{
			return FindComponent<T>( GetObject(obj,name) );
		}
		
		/// Finds components of type 'T' under a gameObject. Does not return
		/// components on the "start" object.
		///
		/// The search will not go deeper once a matching component is found, 
		/// but will continue down other transform nodes in the hierarchy tree.
		/// The intention being that this function can then be called again on 
		/// the resulting objects if desired to dig deeper.
		public static List<T> FindNestedComponents<T>(GameObject start) where T : Component
		{
			List<T> nestedComponents = new List<T>();
			List<Transform> currentDepth = new List<Transform>();
			
			currentDepth.Add(start.transform);
			while (currentDepth.Count > 0)
			{
				List<Transform> nextDepth = new List<Transform>();
				foreach (Transform current in currentDepth)
				{
					foreach (Transform child in current)
					{
						nextDepth.Add(child);
					}
				}
				
				currentDepth.Clear();
				foreach (Transform child in nextDepth)
				{
					T nestedComponent = child.GetComponent<T>();
					// Don't go any deeper. The recursive nature of our system will deal with children of this component.
					if (nestedComponent != null)
					{
						nestedComponents.Add(nestedComponent);
					}
					else
					{
						currentDepth.Add(child);
					}
				}
			}
			
			return nestedComponents;
		}
		
		public static T FindComponentUnder<T>( GameObject obj, string containerName ) where T : Component
		{
			
			return FindComponent<T>(GetObjectExactMatch(obj, containerName));
		}
		
		public static T FindComponent<T>( GameObject obj ) where T : Component
		{
			if ( obj != null )
			{
				var comp = obj.GetComponent<T>();
				if (comp != null)
				{
					return comp;
				}
				
				foreach (Transform child in obj.transform)
				{
					comp = FindComponent<T>(child.gameObject);
					if (comp != null)
					{
						return comp;
					}
				}
				
			}
			return null;
		}
		
		public static Component FindComponent(GameObject obj, System.Type type)
		{
			if ( obj != null )
			{
				Component comp = obj.GetComponent(type);
				if (comp != null)
				{
					return comp;
				}
				
				foreach (Transform child in obj.transform)
				{
					comp = FindComponent(child.gameObject, type);
					if (comp != null)
					{
						return comp;
					}
				}
				
			}
			return null;
		}
		
		public static T FindComponentUpwards<T>( GameObject obj ) where T : Component
		{
			if ( obj != null )
			{
				var comp = obj.GetComponent<T>();
				if (comp != null)
				{
					return comp;
				}
				
				{
					Transform parent = obj.transform.parent;
					if (parent != null)
					{
						comp = FindComponentUpwards<T>(obj.transform.parent.gameObject);
						if (comp != null)
						{
							return comp;
						}
					}
				}
				
			}
			return null;
		}
		
		public static Component[] FindAllComponents(GameObject obj, System.Type type)
		{
			List<Component> foundElements = new List<Component>();
			
			if ( obj != null )
			{
				Component[] components = obj.GetComponents(type);
				if (components != null)
				{
					foreach( var com in components )
					{
						foundElements.Add(com);
					}
				}
				
				foreach (Transform child in obj.transform)
				{
					components = FindAllComponents(child.gameObject, type);
					if (components != null)
					{
						foreach( var com in components )
						{
							foundElements.Add(com);
						}
					}
				}
			}
			
			return foundElements.ToArray();
		}
		
		public static T[] FindAllComponents<T>( GameObject obj ) where T : Component
		{
			List<T> foundElements = new List<T>();
			
			if ( obj != null )
			{
				T[] components = obj.GetComponents<T>();
				if (components != null)
				{
					foreach( var com in components )
					{
						foundElements.Add(com);
					}
				}
				
				foreach (Transform child in obj.transform)
				{
					components = FindAllComponents<T>(child.gameObject);
					if (components != null)
					{
						foreach( var com in components )
						{
							foundElements.Add(com);
						}
					}
				}
				
			}
			
			return foundElements.ToArray();
		}
		
		public static void Parent(GameObject parent, string name, GameObject child)
		{
			Parent( GetObjectExactMatch(parent,name), child); 
		}
		
		public static void Parent(Transform parent, Transform child)
		{
			Vector3 localPosition = child.localPosition;
			Quaternion localRotation = child.localRotation;
			Vector3 localScale = child.localScale;
			child.parent = parent;
			child.localPosition = localPosition;
			child.localRotation = localRotation;
			child.localScale = localScale;
		}
		
		public static void Parent(GameObject parent, GameObject child)
		{
			if ( parent != null && child != null )
			{
				Parent(parent.transform, child.transform);
			}
		}
		
		/// This method is for when you have numerous gameObjects under a single parent
		/// that share a prefix, eg. "Item0", "Item1", "Item2" and need to be found in 
		/// the order defined by the postfix index.
		public static List<GameObject> GetSortedChildren(GameObject container, string prefix)
		{
			List<GameObject> sortedChildren = new List<GameObject>();
			foreach (Transform t in container.transform)
			{
				if (t.name.StartsWith(prefix))
				{
					sortedChildren.Add(t.gameObject);
				}
			}
			
			sortedChildren.Sort(delegate(GameObject child1, GameObject child2) {
				int index1 = 0;
				int index2 = 0;
				
				int.TryParse(child1.name.Substring(prefix.Length), out index1);
				int.TryParse(child2.name.Substring(prefix.Length), out index2);
				
				return index1.CompareTo(index2);
			});
			
			return sortedChildren;
		}
		
		public static GameObject[] GetObjects(GameObject obj, string name = null)
		{
			List<GameObject> list = new List<GameObject>();
			
			if ( obj == null ) return list.ToArray();
			
			if (null == name || obj.name.Contains(name))
			{
				list.Add(obj);
			}
			
			foreach (Transform t in obj.transform)
			{
				var tmp = GetObjects(t.gameObject,name);
				foreach( var tt in tmp )
				{
					list.Add(tt);
				}
			}
			
			return list.ToArray();
		}
		
		public static GameObject[] GetObjectsExactMatch(GameObject obj, string name)
		{
			List<GameObject> list = new List<GameObject>();
			
			if ( obj == null ) return list.ToArray();
			
			if (obj.name.Equals(name))
			{
				list.Add(obj);
			}
			
			foreach (Transform t in obj.transform)
			{
				var tmp = GetObjectsExactMatch(t.gameObject,name);
				foreach( var tt in tmp )
				{
					list.Add(tt);
				}
			}
			
			return list.ToArray();
		}
		
		
		public static GameObject GetObject(GameObject obj, string name)
		{
			return GetObject(obj, name, false);
		}
		
		public static GameObject GetObjectExactMatch(GameObject obj, string name)
		{
			if (name.Contains("/"))
			{
				// This is a path.
				string[] pathElements = name.Split('/');
				GameObject link = obj;
				foreach (string path in pathElements)
				{
					link = GetObject(link, path, true);
				}
				return link;
			}
			else
			{
				return GetObjectBreadthFirst(obj, name, true);
			}
		}
		
		public static GameObject GetObjectBreadthFirst(GameObject obj, string name, bool bExactMatch = true)
		{
			if ( obj == null || string.IsNullOrEmpty(name) ) 
			{
				return null;
			}
			
			List<Transform> checking = new List<Transform>();
			checking.Add(obj.transform);
			
			while (checking.Count > 0)
			{
				List<Transform> nextChecks = new List<Transform>();
				foreach (Transform t in checking)
				{
					if (!bExactMatch)
					{
						if (t.name.Contains(name))
						{
							return t.gameObject;
						}
					}
					else if (t.name == name)
					{
						return t.gameObject;
					}
					
					foreach (Transform c in t)
					{
						nextChecks.Add(c);
					}
				}
				checking = nextChecks;
			}
			
			return null;
		}
		
		public static GameObject GetObject(GameObject obj, string name, bool bExactMatch )
		{
			if ( obj == null || string.IsNullOrEmpty(name) ) 
			{
				return null;
			}
			
			if (!bExactMatch)
			{
				if (obj.name.Contains(name))
				{
					return obj;
				}
			}
			else if (obj.name == name)
			{
				return obj;
			}
			
			if (obj.transform)
			{
				foreach (Transform t in obj.transform)
				{
					if (!t) continue;
					
					GameObject result = GetObject(t.gameObject, name, bExactMatch);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}
		
		public static Material[] GatherMaterials( GameObject obj, string name)
		{
			List<Material> materials = new List<Material>();
			
			if ( obj != null )
			{
				if (obj.renderer != null)
				{
					if (string.IsNullOrEmpty(name))
					{
						foreach( var mat in obj.renderer.materials)
						{
							materials.Add(mat);
						}
					}
					else
					{
						foreach (Material material in obj.renderer.materials)
						{
							if (material.name.Contains(name))
							{
								materials.Add(material);
							}
						}
					}
				}
				
				foreach (Transform child in obj.transform)
				{
					var tmp = GatherMaterials(child.gameObject, name);
					foreach( var tt in tmp )
					{
						materials.Add(tt);
					}
				}
			}
			return materials.ToArray();
		}
		
		public static int[] GetIntArray(ArrayList data)
		{
			List<int> list = new List<int>();
			if ( data != null)
			{
				foreach (object d in data)
				{
					if ( d != null )
					{
						int value = 0;
						if ( int.TryParse(d.ToString(), out value) )
						{
							list.Add(value);
						}
					}
				}
			}
			return list.ToArray();
		}
		
		public static void BroadcastMessage(string message)
		{
			foreach( GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)) )
			{
				if (go)
				{
					go.SendMessage(message, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
		
		public static void BroadcastMessage(string message, object value)
		{
			foreach( GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)) )
			{
				if (go)
				{
					go.SendMessage(message, value,SendMessageOptions.DontRequireReceiver);
				}
			}
		}
		
		public static float[] GetFloatArray(ArrayList data)
		{
			List<float> list = new List<float>();
			if ( data != null)
			{
				foreach (object d in data)
				{
					if ( d != null )
					{
						float value = 0;
						if ( float.TryParse(d.ToString(), out value) )
						{
							list.Add(value);
						}
					}
				}
			}
			return list.ToArray();
		}
		
		
		public static Transform Ascend(string ancestorName, Transform child)
		{
			if (child.transform.parent != null)
			{
				if (child.transform.parent.name == ancestorName)
				{
					return child.transform.parent;
				}
				else
				{
					return Ascend(ancestorName, child.transform.parent);
				}
			}
			return null;
		}
		
		public static bool FloatEquals(float a, float b, float epsilon)
		{
			return (Mathf.Abs(a - b) <= epsilon);
		}
		
		public static Color ColourFromString(string rgba, Color defaultValue)
		{
			if( ( rgba != null ) && ( rgba.StartsWith( "rgba(" ) == true ) && ( rgba.EndsWith( ")" ) == true ) )
			{
				string inner = rgba.Substring( 5, rgba.Length - 6 );
				var parts = inner.Split( ',' );
				if( parts.Length == 4 )
				{
					int r,g,b;
					float a;
					if( int.TryParse( parts[ 0 ], out r ) && int.TryParse( parts[ 1 ], out g ) && int.TryParse( parts[ 2 ], out b ) && float.TryParse( parts[ 3 ], out a ) )
					{
						UnityEngine.Color c = UnityEngine.Color.white;
						c.r = r / 255.0f;
						c.g = g / 255.0f;
						c.b = b / 255.0f;
						c.a = a;
						return c;
					}
				}
			}
			return defaultValue;
		}
		
		/// <summary>
		/// Given a position/rotation and a second position in local space, returns that second position in world space
		/// </summary>
		/// <returns>
		/// The point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='localPosition'>
		/// Local position.
		/// </param>
		public static Vector3 TransformPoint(Vector3 forward, Vector3 up, Vector3 startingPosition, Vector3 localPosition, Vector3 lossyScale)
		{
			Quaternion rotation = Quaternion.LookRotation(forward, up);
			
			return TransformPoint(rotation, startingPosition, localPosition, lossyScale);
		}
		
		/// <summary>
		/// Given a position/rotation and a second position in local space, returns that second position in world space
		/// </summary>
		/// <returns>
		/// The point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='localPosition'>
		/// Local position.
		/// </param>
		public static Vector3 TransformPoint(Quaternion rotation, Vector3 startingPosition, Vector3 localPosition, Vector3 lossyScale)
		{
			return startingPosition + rotation * Vector3.Scale(lossyScale, localPosition);
		}
		
		/// <summary>
		/// Given a position/rotation and a second position in local space, returns that second position in world space
		/// </summary>
		/// <returns>
		/// The point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='localPosition'>
		/// Local position.
		/// </param>
		public static Vector3 TransformPoint(Vector3 forward, Vector3 up, Vector3 startingPosition, Vector3 localPosition)
		{
			Quaternion rotation = Quaternion.LookRotation(forward, up);
			
			return TransformPoint(rotation, startingPosition, localPosition);
		}
		
		/// <summary>
		/// Given a position/rotation and a second position in local space, returns that second position in world space
		/// </summary>
		/// <returns>
		/// The point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='localPosition'>
		/// Local position.
		/// </param>
		public static Vector3 TransformPoint(Quaternion rotation, Vector3 startingPosition, Vector3 localPosition)
		{
			return startingPosition + rotation * localPosition;
		}
		
		/// <summary>
		/// Given a position/rotation and a second position in world space, returns the second position locally to the first
		/// </summary>
		/// <returns>
		/// The transform point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='worldPosition'>
		/// World position.
		/// </param>
		public static Vector3 InverseTransformPoint(Vector3 forward, Vector3 up, Vector3 startingPosition, Vector3 worldPosition)
		{
			Quaternion rotation = Quaternion.LookRotation(forward, up);
			
			return InverseTransformPoint(rotation, startingPosition, worldPosition);
		}
		
		/// <summary>
		/// Given two points in world space, returns the second position locally to the first
		/// </summary>
		/// <returns>
		/// The transform point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='worldPosition'>
		/// World position.
		/// </param>
		public static Vector3 InverseTransformPoint(Quaternion rotation, Vector3 startingPosition, Vector3 worldPosition)
		{
			return Quaternion.Inverse(rotation)*(worldPosition - startingPosition);
		}
		
		/// <summary>
		/// Given a position/rotation and a second position in world space, returns the second position locally to the first
		/// </summary>
		/// <returns>
		/// The transform point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='worldPosition'>
		/// World position.
		/// </param>
		public static Vector3 InverseTransformPoint(Vector3 forward, Vector3 up, Vector3 startingPosition, Vector3 worldPosition, Vector3 lossyScale)
		{
			Quaternion rotation = Quaternion.LookRotation(forward, up);
			
			return InverseTransformPoint(rotation, startingPosition, worldPosition, lossyScale);
		}
		
		/// <summary>
		/// Given two points in world space, returns the second position locally to the first
		/// </summary>
		/// <returns>
		/// The transform point.
		/// </returns>
		/// <param name='forward'>
		/// Forward.
		/// </param>
		/// <param name='up'>
		/// Up.
		/// </param>
		/// <param name='startingPosition'>
		/// Starting position.
		/// </param>
		/// <param name='worldPosition'>
		/// World position.
		/// </param>
		public static Vector3 InverseTransformPoint(Quaternion rotation, Vector3 startingPosition, Vector3 worldPosition, Vector3 lossyScale)
		{
			return Vector3.Scale(new Vector3(1.0f/lossyScale.x, 1.0f/lossyScale.y, 1.0f/lossyScale.z), (Quaternion.Inverse(rotation)*(worldPosition - startingPosition)));
		}
	}
}
