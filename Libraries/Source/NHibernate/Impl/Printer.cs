using System.Collections;
using log4net;
using NHibernate.Engine;
using NHibernate.Metadata;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Impl
{
	public sealed class Printer
	{
		private readonly ISessionFactoryImplementor _factory;
		private static readonly ILog log = LogManager.GetLogger(typeof(Printer));

		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity">an actual entity object, not a proxy!</param>
		/// <param name="entityMode"></param>
		/// <returns></returns>
		public string ToString(object entity, EntityMode entityMode)
		{
			IClassMetadata cm = _factory.GetClassMetadata(entity.GetType());
			if (cm == null)
			{
				return entity.GetType().FullName;
			}

			IDictionary result = new Hashtable();

			if (cm.HasIdentifierProperty)
			{
				result[cm.IdentifierPropertyName] =
					cm.IdentifierType.ToLoggableString(cm.GetIdentifier(entity, entityMode), _factory);
			}

			IType[] types = cm.PropertyTypes;
			string[] names = cm.PropertyNames;
			object[] values = cm.GetPropertyValues(entity, entityMode);

			for (int i = 0; i < types.Length; i++)
			{
				result[names[i]] = types[i].ToLoggableString(values[i], _factory);
			}

			return cm.EntityName + CollectionPrinter.ToString(result);
		}

		public string ToString(IType[] types, object[] values)
		{
			IList list = new ArrayList(types.Length);
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] != null)
				{
					list.Add(types[i].ToLoggableString(values[i], _factory));
				}
			}
			return CollectionPrinter.ToString(list);
		}

		public string ToString(IDictionary namedTypedValues)
		{
			IDictionary result = new Hashtable(namedTypedValues.Count);

			foreach (DictionaryEntry me in namedTypedValues)
			{
				TypedValue tv = (TypedValue) me.Value;
				result[me.Key] = tv.Type.ToLoggableString(tv.Value, _factory);
			}

			return CollectionPrinter.ToString(result);
		}

		public void ToString(IEnumerator enumerator, EntityMode entityMode)
		{
			if (!log.IsDebugEnabled || !enumerator.MoveNext())
			{
				return;
			}

			log.Debug("listing entities:");
			int i = 0;

			do
			{
				if (i++ > 20)
				{
					log.Debug("more......");
					break;
				}
				log.Debug(ToString(enumerator.Current, entityMode));
			} while (enumerator.MoveNext());
		}

		public Printer(ISessionFactoryImplementor factory)
		{
			_factory = factory;
		}
	}
}