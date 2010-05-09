using System;
using System.IO;
using System.Runtime.Serialization;
using NHibernate.Engine;
using NHibernate.Persister.Entity;
using NHibernate.Util;
using NHibernate.Impl;

namespace NHibernate.Action
{
	/// <summary>
	/// Base class for actions relating to insert/update/delete of an entity
	/// instance.
	/// </summary>
	[Serializable]
	public abstract class EntityAction : IExecutable, IComparable<EntityAction>, IDeserializationCallback
	{
		private readonly string entityName;
		private readonly object id;
		private readonly object instance;
		private readonly ISessionImplementor session;

		[NonSerialized]
		private IEntityPersister persister;

		/// <summary>
		/// Instantiate an action.
		/// </summary>
		/// <param name="session">The session from which this action is coming.</param>
		/// <param name="id">The id of the entity</param>
		/// <param name="instance">The entity instance</param>
		/// <param name="persister">The entity persister</param>
		protected internal EntityAction(ISessionImplementor session, object id, object instance, IEntityPersister persister)
		{
			entityName = persister.EntityName;
			this.id = id;
			this.instance = instance;
			this.session = session;
			this.persister = persister;
		}

		/// <summary>
		/// Entity name accessor
		/// </summary>
		public string EntityName
		{
			get { return entityName; }
		}

		/// <summary>
		/// Entity Id accessor
		/// </summary>
		public object Id
		{
			get
			{
				if (id is DelayedPostInsertIdentifier)
				{
					return session.PersistenceContext.GetEntry(instance).Id;
				}
				return id;
			}
		}

		/// <summary>
		/// Entity Instance
		/// </summary>
		public object Instance
		{
			get { return instance; }
		}

		/// <summary>
		/// Session from which this action originated
		/// </summary>
		public ISessionImplementor Session
		{
			get { return session; }
		}

		/// <summary>
		/// The entity persister.
		/// </summary>
		public IEntityPersister Persister
		{
			get { return persister; }
		}

		protected internal abstract bool HasPostCommitEventListeners { get;}

		#region IExecutable Members

		public object[] PropertySpaces
		{
			get { return persister.PropertySpaces; }
		}

		public void BeforeExecutions()
		{
			throw new AssertionFailure("BeforeExecutions() called for non-collection action");
		}

		public abstract void Execute();

		public bool HasAfterTransactionCompletion()
		{
			return persister.HasCache || HasPostCommitEventListeners;
		}

		public abstract void AfterTransactionCompletion(bool success);

		#endregion

		#region IComparable<EntityAction> Members

		public virtual int CompareTo(EntityAction other)
		{
			//sort first by entity name
			int roleComparison = entityName.CompareTo(other.entityName);
			if (roleComparison != 0)
			{
				return roleComparison;
			}
			else
			{
				//then by id
				return persister.IdentifierType.Compare(id, other.id, session.EntityMode);
			}
		}

		#endregion

		#region IDeserializationCallback Members

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			try
			{
				persister = session.GetEntityPersister(instance);
			}
			catch (MappingException e)
			{
				throw new IOException("Unable to resolve class persister on deserialization", e);
			}
		}

		#endregion

		public override string ToString()
		{
			return StringHelper.Unqualify(GetType().FullName) + MessageHelper.InfoString(entityName, id);
		}
	}
}
