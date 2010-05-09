using System;
using System.Data;
using NHibernate.SqlTypes;

namespace NHibernate.Type
{
	[Serializable]
	public abstract class AbstractStringType: ImmutableType, IDiscriminatorType
	{
		public AbstractStringType(SqlType sqlType)
			: base(sqlType)
		{
		}

		public override void Set(IDbCommand cmd, object value, int index)
		{
			if (value == null)
			{
				value = "";
			}
			((IDataParameter)cmd.Parameters[index]).Value = value;
		}

		public override object Get(IDataReader rs, int index)
		{
			var value = Convert.ToString(rs[index]);
			if (value == null)
			{
				value = "";
			}
			return value;
		}

		public override object Get(IDataReader rs, string name)
		{
			var value = Convert.ToString(rs[name]);
			if (value == null)
			{
				value = "";
			}
			return value;
		}

		public override string ToString(object val)
		{
			return (string)val;
		}

		public override object FromStringValue(string xml)
		{
			return xml;
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(string); }
		}

		#region IIdentifierType Members

		public object StringToObject(string xml)
		{
			return xml;
		}

		#endregion

		#region ILiteralType Members

		public string ObjectToSQLString(object value, Dialect.Dialect dialect)
		{
			return "'" + (string)value + "'";
		}

		#endregion
	}
}
