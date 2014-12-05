using UnityEngine;

namespace EB.Director
{
	public enum GroupType
	{
		None,
		Actor,
		Camera,
		Director,
		Event,
		ImageEffect,
	}
	
	public enum SpaceType
	{
		World,
		Local,
		LocalToWorld,
	}
	
	public enum TrackType
	{	
		None,
		Position,
		Rotation,
		Transform,
		FOV,
		Color,
		Event,
		Director,
		TimeScale,
		EnableComponent,
		Variable,
		OrthographicSize,
		CameraShake,
		Far,
	}
	
	public enum BlendMode
	{
		Cut,
		Linear,
		EaseIn,
		EaseOut,
		CubicIn,
		CubicOut,
		EaseOutIn,
		EaseInOut,
		CubicInOut,
		CubicOutIn,
	}
	
	public enum VariableType
	{
		None,
		Float,
		Vector2,
		Vector3,
		Vector4,
		Color,
	}
	
	public struct QuatPos
	{
		public Quaternion quat;
		public Vector3 pos;
		
		public static QuatPos FromTransformGlobal( Transform t )
		{
			QuatPos qp;
			qp.quat = t.rotation;
			qp.pos = t.position;
			return qp;
		}
		
		public static QuatPos FromTransformLocal( Transform t )
		{
			QuatPos qp;
			qp.quat = t.localRotation;
			qp.pos = t.localPosition;
			return qp;
		}
		
		public static QuatPos FromTransform( Transform t, SpaceType space )
		{
			if ( space == SpaceType.Local )
			{
				return FromTransformLocal(t);
			}
			return FromTransformGlobal(t);
		}
		
		public void Apply( Transform t, SpaceType space )
		{
			if ( space == SpaceType.Local )
			{
				t.localPosition = pos;
				t.localRotation = quat;
			}
			else if ( space == SpaceType.World)
			{
				t.position = pos;
				t.rotation = quat;
			}
		}
		
		public static QuatPos Lerp( QuatPos from, QuatPos to, float t )
		{
			QuatPos r;
			r.quat = Quaternion.Lerp(from.quat,to.quat,t);
			r.pos = Vector3.Lerp(from.pos, to.pos,t);
			return r;
		}
		
		public override string ToString ()
		{
			return "quat: " + quat.ToString() + " pos: " + pos.ToString();
		}
	}
	
	public struct CameraData
    {
        public  Matrix4x4 		projection;
        public  Vector3			position;
        public 	Quaternion 		rotation;
		public  bool			orthographic;
		public  float			orthographicSize;

        public CameraData(Camera cam)
        {
            //projection = cam.projectionMatrix;
            position = cam.transform.position;
            rotation = cam.transform.rotation;
			orthographic = cam.orthographic;
			orthographicSize = cam.orthographicSize;
			
			if ( cam.orthographic )
			{
				projection = cam.projectionMatrix; 
			}
			else
			{
				projection = Matrix4x4.Perspective(cam.fieldOfView,cam.aspect,cam.nearClipPlane,cam.farClipPlane);
			}
			
        }

        public static implicit operator CameraData(Camera cam)
        {
            CameraData c = new CameraData(cam); 
            return c;
        }
		
		public static Matrix4x4 Lerp(Matrix4x4 a, Matrix4x4 b, float t)
	    {
	        Matrix4x4 lerp = default(Matrix4x4);
	        lerp.SetRow(0, Vector4.Lerp(a.GetRow(0), b.GetRow(0), t));
	        lerp.SetRow(1, Vector4.Lerp(a.GetRow(1), b.GetRow(1), t));
	        lerp.SetRow(2, Vector4.Lerp(a.GetRow(2), b.GetRow(2), t));
	        lerp.SetRow(3, Vector4.Lerp(a.GetRow(3), b.GetRow(3), t));
	        return lerp;
	    }
		
		public static CameraData Lerp( CameraData from, CameraData to, float t )
		{
			CameraData r;
			r.projection = Lerp(from.projection, to.projection, t );
			r.position = Vector3.Lerp(from.position, to.position, t );
			r.rotation = Quaternion.Lerp(from.rotation, to.rotation, t );
			r.orthographic = to.orthographic;
			r.orthographicSize = Mathf.Lerp(from.orthographicSize, to.orthographicSize, t);
			return r;
		}
		
		public void Apply( Camera camera )
		{
			camera.orthographic = orthographic;
			camera.orthographicSize = orthographicSize;
			camera.projectionMatrix = projection;
			camera.transform.position = position;
			camera.transform.rotation = rotation;
		}
    }
	
	public static class Utils
	{
		public static bool HasGroupInput( GroupType type )
		{
			switch( type )
			{
			case GroupType.Actor:
			case GroupType.Camera:
			case GroupType.ImageEffect:
				return true;
			default:
				return false;
			}
		}
		
		public static bool HasBlendMode( TrackType type )
		{
			switch( type )
			{
			case TrackType.EnableComponent:
			case TrackType.Event:
				return false;
			default:
				return true;
			}
		}
		
		private static void SetValue( System.Reflection.FieldInfo field, object instance, object value )
		{
			try
			{
				field.SetValue( instance, value ); 
			}
			catch {}
		}
		
		public static void ApplyVariable( VariableType type, System.Reflection.FieldInfo field, object instance, float blend, EB.Director.Serialization.KeyFrame from, EB.Director.Serialization.KeyFrame to)
		{
			switch(type)
			{
			case VariableType.Float:
				SetValue( field, instance, Mathf.Lerp(from.FloatValue, to.FloatValue, blend) );
				break;
			case VariableType.Vector2:
				SetValue( field, instance, Vector2.Lerp(from.Vector2Value, to.Vector2Value, blend) );
				break;
			case VariableType.Vector3:
				SetValue( field, instance, Vector3.Lerp(from.Vector3Value, to.Vector3Value, blend) );
				break;
			case VariableType.Vector4:
				SetValue( field, instance, Vector4.Lerp(from.Vector4Value, to.Vector4Value, blend) );
				break;
			case VariableType.Color:
				SetValue( field, instance, Color.Lerp(from.ColorValue, to.ColorValue, blend) );
				break;				
			}
		}
		
		public static VariableType GetVariableType( System.Reflection.FieldInfo field ) 
		{
			if ( field != null )
			{
				if ( field.FieldType == typeof(float) )
				{
					return VariableType.Float;
				}
				else if ( field.FieldType == typeof(Vector2) )
				{
					return VariableType.Vector2;
				}
				else if ( field.FieldType == typeof(Vector3) )
				{
					return VariableType.Vector3;
				}
				else if ( field.FieldType == typeof(Vector4) )
				{
					return VariableType.Vector4;
				}
				else if ( field.FieldType == typeof(Color) )
				{
					return VariableType.Color;
				}
			}
			return VariableType.None;
		}
		
		public static System.Type GetType( string name )
		{
			return System.Type.GetType(name, false); 
		}
		
	}
	
}
