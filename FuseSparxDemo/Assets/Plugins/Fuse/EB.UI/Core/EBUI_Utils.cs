using UnityEngine;
using System.Collections.Generic;

namespace EB
{
	public static class UIUtils
	{
		public static void ApplyTints(GameObject container, string identifier, Color tint)
		{
			foreach (UIWidget widget in EB.Util.FindAllComponents<UIWidget>(container))
			{
				widget.color = tint;
			}
		}
		
		public static void SetAllAlphaValues(GameObject container, float alpha)
		{
			foreach (UIWidget widget in EB.Util.FindAllComponents<UIWidget>(container))
			{
				widget.alpha = alpha;
			}
		}

		public static void SetLabelContents(GameObject container, string labelName, object labelText) 
		{
			GameObject obj = EB.Util.GetObjectExactMatch(container, labelName);
			if (obj != null)
			{
				UILabel label = EB.Util.FindComponent<UILabel>(obj);
				if (label != null)
				{
					var str = string.Empty;
					if ( labelText != null )
					{
						str = labelText.ToString();
						if (Application.isPlaying && str.StartsWith("ID_"))
						{
							str = EB.Localizer.GetString(str);
						}
					}
					label.text = str;
				}
			}
		}
		
		public static string GetLabelContents(GameObject container, string labelName) 
		{
			var contents = string.Empty;
			GameObject obj = EB.Util.GetObjectExactMatch(container, labelName);
			if (obj != null)
			{
				UILabel label = EB.Util.FindComponent<UILabel>(obj);
				if (label != null)
				{
					contents = label.text;
				}
			}
			return contents;
		}

		public static Bounds GetWidgetWorldBounds(UIWidget w)
		{
			Bounds b = new Bounds();
			if (w == null)
			{
				EB.Debug.LogError("Null widget passed into GetWidgetWorldBounds");
				return b;
			}

			Vector3[] corners = null;
			UILabel label = w as UILabel;
			if (label != null)
			{
				corners = GetLabelWorldCorners(label);
			}
			else
			{
				corners = w.worldCorners;
			}

			if (corners.Length < 1)
			{
				EB.Debug.LogError("No corners found!");
				return b;
			}

			b = new Bounds(corners[0], Vector3.zero);
			for (int i = 1; i < corners.Length; ++i)
			{
				b.Encapsulate(corners[i]);
			}

			return b;
		}
		
		// Gets the world coordinates of the used area of a textfield.
		public static Vector3[] GetLabelWorldCorners(UILabel label)
		{
			// This code is a mix of UILabel's ApplyOffset and UIWidget's worldCorners getter.
			Vector2 size = label.printedSize;
			Vector2 po = label.pivotOffset;
			float height = label.height;
			float width = label.width;
			
			float fx = Mathf.Lerp(0f, -width, po.x);
			float fy = Mathf.Lerp(height, 0f, po.y) + Mathf.Lerp((label.printedSize.y - height), 0f, po.y);
			
			fx = Mathf.Round(fx);
			fy = Mathf.Round(fy);

			float x0 = fx;
			float y0 = + fy;
			float x1 = size.x + fx;
			float y1 = - size.y + fy;

			Transform wt = label.cachedTransform;
			Vector3[] worldCorners = new Vector3[4];
			worldCorners[0] = wt.TransformPoint(x0, y0, 0f);
			worldCorners[1] = wt.TransformPoint(x0, y1, 0f);
			worldCorners[2] = wt.TransformPoint(x1, y1, 0f);
			worldCorners[3] = wt.TransformPoint(x1, y0, 0f);

			return worldCorners;
		}

		public static string GetFullName(GameObject obj)
		{
			if (obj == null)
			{
				return "<null>";
			}
			
			string name = obj.name;
			
			while (obj != null)
			{
				Transform p = obj.transform.parent;
				if (p != null)
				{
					obj = p.gameObject;
					name = obj.name + "/" + name;
				}
				else
				{
					obj = null;
				}
			}
			
			return name;
		}
		
		// Gets a list of components in gameObjects that match a specific name within the container.
		public static List<T> GetComponentsFromContainer<T>(GameObject container, string name) where T : Component
		{
			List<GameObject> matchingObjects = new List<GameObject>( EB.Util.GetObjects(container, name) );
			matchingObjects.Sort(delegate(GameObject a, GameObject b)
			{
				return string.Compare(a.name, b.name);
			});
			
			List<T> results = new List<T>();
			foreach (GameObject g in matchingObjects)
			{
				T item = g.GetComponent<T>();
				if (item != null)
				{
					results.Add(item);
				}
			}
			
			return results;
		}

		public static List<UIDependency> GetUIDependencies(Component container)
		{
			Component[] componentsArray = EB.Util.FindAllComponents(container.gameObject, typeof(UIDependency));
			// Being dependent on ourself is a bad thing.
			List<Component> components = EB.ArrayUtils.ToList<Component>(componentsArray);
			components.Remove(container);

			List<UIDependency> dependencies = new List<UIDependency>();
			foreach (Component component in components)
			{
				if (component.gameObject.activeInHierarchy)
				{
					UIDependency dependency = (UIDependency)component;
					dependencies.Add(dependency);
				}
			}

			return dependencies;
		}

		public static void WaitForUIDependencies(EB.Action dependenciesReadyCb, List<UIDependency> dependencies)
		{
			if (dependencies.Count < 1)
			{
				dependenciesReadyCb();
			}
			else
			{
				int callbacksPending = dependencies.Count;
				foreach (UIDependency dependency in dependencies)
				{
					EB.Action handleDependencyReady = null;
					Component component = (Component)dependency;
					if (!dependency.IsReady() && (component == null || component.gameObject.activeInHierarchy))
					{
						UIDependency closureDependency = dependency;
						handleDependencyReady = delegate() {
							closureDependency.onReadyCallback -= handleDependencyReady;
							closureDependency.onDeactivateCallback -= handleDependencyReady;
							--callbacksPending;
							if (callbacksPending < 1)
							{
								dependenciesReadyCb();
							}
						};
						dependency.onReadyCallback += handleDependencyReady;
						dependency.onDeactivateCallback += handleDependencyReady;
					}
					else // Don't need to wait for this ready component.
					{
						--callbacksPending;
						if (callbacksPending < 1)
						{
							dependenciesReadyCb();
						}
					}
				}
			}
		}
		
		// Assigns the given texture to the first UITextureRef component found in the container
		public static void SetTextureRef(GameObject container, string texturePath)
		{
			if (container == null) return;
			if( string.IsNullOrEmpty(texturePath)) return;
			
			UITextureRef textureRef = EB.Util.FindComponent<UITextureRef>(container);
			if(textureRef != null)
			{
				textureRef.baseTexturePath = texturePath;
			}
		}
		
		// Assigns the given texture to the first UITexture component found in the container.
		public static void SetTexture(GameObject container, Texture2D uiTexture, bool resizeContainerToFit = true)
		{
			if (uiTexture == null) return;
			if (container == null) return;

			UITexture textureHolder = EB.Util.FindComponent<UITexture>(container);
			UITextureRef textureRefHolder = EB.Util.FindComponent<UITextureRef>(container);
			if (textureHolder == null && textureRefHolder == null)
			{
				// Nowhere to put it!
				EB.Debug.LogWarning("SetTexture: Nowhere to put texture!", container);
				return;
			}
			
			if(textureHolder != null)
			{
				textureHolder.mainTexture = uiTexture;
			}
			else if (textureRefHolder != null)
			{
				textureRefHolder.mainTexture = uiTexture;
			}
			
			if (resizeContainerToFit)
			{
				Vector3 scale = new Vector3(uiTexture.width, uiTexture.height, 1f);
				if(textureHolder != null)
				{
					textureHolder.width = Mathf.RoundToInt(scale.x);
					textureHolder.height = Mathf.RoundToInt(scale.y);
				}
				else if (textureRefHolder != null)
				{
					textureRefHolder.width = Mathf.RoundToInt(scale.x);
					textureRefHolder.height = Mathf.RoundToInt(scale.y);
				}
			}
		}
		
		// Caveats: icon and label must be set 'pivot = left'.
		[System.Obsolete]
		public static void CenterSpriteAndLabel(GameObject button, string iconName, string labelName)
		{
			GameObject icon = EB.Util.GetObjectExactMatch(button, iconName);
			GameObject label = EB.Util.GetObjectExactMatch(button, labelName);
			float textWidth = label.GetComponent<UILabel>().width;
			float iconWidth = icon.GetComponent<UISprite>().width;
			float paddingBetweenIconAndLabel = label.transform.localPosition.x - icon.transform.localPosition.x - iconWidth;
			float requiredWidth = iconWidth + paddingBetweenIconAndLabel + textWidth;
			
			Vector3 iconPos = icon.transform.localPosition;
			iconPos.x = - (requiredWidth / 2f);
			icon.transform.localPosition = iconPos;

			Vector3 labelPos = label.transform.localPosition;
			labelPos.x = iconPos.x + iconWidth + paddingBetweenIconAndLabel;
			label.transform.localPosition = labelPos;
		}
		
		// Used for caching references easily.
		public static T GetOrAssign<T>(ref T result, GameObject container, string childName) where T : Component
		{
			if (result != null)
			{
				return result;
			}
			
			if (childName != "")
			{
				container = EB.Util.GetObjectExactMatch(container, childName);
			}
			
			result = EB.Util.FindComponent<T>(container);
			return result;
		}
		
		const string validRegularTextChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890 ";
		
		public static char TextValidatorForTeamName(string prevStr, char nextChar)
		{
			const int nameMaxLen = 22;
			if (prevStr.Length >= nameMaxLen)
			{
				return (char)0;
			}
			
			// Disallow double spaces.
			if (nextChar.ToString() == " ")
			{
				if (prevStr.EndsWith(" "))
				{
					return (char)0;
				}
				else
				{
					return nextChar; //ToUpper();
				}
			}
			
			if (validRegularTextChars.Contains(nextChar.ToString()))
			{
				return nextChar;
			}
			
			return (char)0;
		}
		
		public static char TextValidatorForEmail(string prevStr, char nextChar)
		{
			const int emailMaxLen = 45;
			if (prevStr.Length >= emailMaxLen)
			{
				return (char)0;
			}
			
			if (validRegularTextChars.Contains(nextChar.ToString()))
			{
				return nextChar;
			}
			
			const string validEmailChars = "!#$%&'*+-/=?^_`{|}~\"(),:;<>@[\\]. ";
			if (validEmailChars.Contains(nextChar.ToString()))
			{
				return nextChar;
			}
			
			return (char)0;
		}
		
		public static char TextValidatorForPassword(string prevStr, char nextChar)
		{
			const int passwordMaxLen = 25;
			if (prevStr.Length >= passwordMaxLen)
			{
				return (char)0;
			}
			
			if (char.IsLetterOrDigit(nextChar) || char.IsPunctuation(nextChar) || char.IsWhiteSpace(nextChar))
			{
				return nextChar;
			}
			
			return (char)0;
		}
	}
}

