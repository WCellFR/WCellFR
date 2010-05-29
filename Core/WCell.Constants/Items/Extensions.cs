
namespace WCell.Constants.Items
{
	public static class Extensions
	{
		#region HasAnyFlag
		public static bool HasAnyFlag(this ItemSubClassMask flags, ItemSubClassMask otherFlags)
		{
			return (flags & otherFlags) != 0;
		}
		public static bool HasAnyFlag(this SocketColor flags, SocketColor otherFlags)
		{
			return (flags & otherFlags) != 0;
		}
		public static bool HasAnyFlag(this ItemBagFamilyMask flags, ItemBagFamilyMask otherFlags)
		{
			return (flags & otherFlags) != 0;
		}
		#endregion
	}
}
