using UnityEngine;
using System.Collections;
using System.Reflection;

namespace EB
{
	// actions
	public delegate void Action();
	public delegate void Action<T>( T obj );
	public delegate void Action<T,U>( T obj, U obj1 );
	public delegate void Action<T,U,V>( T obj, U obj1, V obj2 );
	public delegate void Action<T,U,V,X>( T obj, U obj1, V obj2, X obj3 );
	public delegate void Action<T,U,V,X,Y>( T obj, U obj1, V obj2, X obj3, Y obj4 );
	public delegate void Action<T,U,V,X,Y,Z>( T obj, U obj1, V obj2, X obj3, Y obj4, Z obj5 );
	
	public delegate R  Function<R>();
	public delegate R  Function<R,A1>(A1 a1);
	public delegate R  Function<R,A1,A2>(A1 a1, A2 a2);
	public delegate R  Function<R,A1,A2,A3>(A1 a1, A2 a2, A3 a3);
	
	// predicate
	public delegate bool Predicate<T> (T obj);
	
	// serializable class
	public interface ISerializable
	{
		object Serialize ();
		void Deserialize (object src);
	}
	
#if false
	// sort of the same class as the System.Ling.Expression
	public static class Expressions
	{
		public static System.Type GetActionType( MethodInfo method ) 
		{
			var parameters = method.GetParameters();			
			var types = new System.Type[parameters.Length];
			for( int i = 0; i < parameters.Length; ++i )
			{
				types[i] = parameters[i].ParameterType;
			}
			
			return GetActionType( types ); 
		}
		
		public static System.Type GetActionType( System.Type[] parameters ) 
		{
			System.Type actionType = null;			
			switch( parameters.Length )
			{
			case 0:
				return typeof(Action);
			case 1:
				actionType = typeof(Action<>); break;
			case 2:
				actionType = typeof(Action<,>); break;
			case 3:
				actionType = typeof(Action<,,>); break;	
			case 4:
				actionType = typeof(Action<,,,>); break;
			case 5:
				actionType = typeof(Action<,,,,>); break;	
			case 6:
				actionType = typeof(Action<,,,,,>); break;		
			default:
				throw new System.ArgumentException("too many parameters");
			}
			
			return actionType.MakeGenericType( parameters ); 
		}
	}
#endif
}

//This is a proxy definition that gets fixed up in the Mono runtime. Causes a warning which is unfortunate
public class MonoPInvokeCallbackAttribute : System.Attribute
{
	protected System.Type type;
	public MonoPInvokeCallbackAttribute( System.Type t ) { type = t; }
}
