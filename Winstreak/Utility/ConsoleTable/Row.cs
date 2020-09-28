namespace Winstreak.Utility.ConsoleTable
{
	public struct Row
	{
		/// <summary>
		/// The elements in this row.
		/// </summary>
		public object[] RowValues;

		/// <summary>
		/// Whether this row happens to be a separator. 
		/// </summary>
		public bool SeparateHere;
	}
}