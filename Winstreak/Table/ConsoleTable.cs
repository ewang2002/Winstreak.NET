using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Winstreak.Table
{
	public class ConsoleTable
	{
		private const string TopLeftJoint = "┌";
		private const string TopRightJoint = "┐";
		private const string BottomLeftJoint = "└";
		private const string BottomRightJoint = "┘";
		private const string TopJoint = "┬";
		private const string BottomJoint = "┴";
		private const string LeftJoint = "├";
		private const string MiddleJoint = "┼";
		private const string RightJoint = "┤";
		private const string HorizontalLine = "─";
		private const string VerticalLine = "│";

		/// <summary>
		/// The rows in this table.
		/// </summary>
		public IList<Row> Rows { get; }

		/// <summary>
		/// The maximum length of each column.
		/// </summary>
		public int[] MaxTextLengthPerColumn { get; }

		/// <summary>
		/// Creates a new Table object. 
		/// </summary>
		/// <param name="columnPerRow">The number of columns per row.</param>
		public ConsoleTable(int columnPerRow)
		{
			if (columnPerRow <= 0)
				throw new ArgumentException("You cannot have a row with 0 or less columns.", nameof(columnPerRow));
			Rows = new List<Row>();
			MaxTextLengthPerColumn = new int[columnPerRow];
		}

		/// <summary>
		/// Adds a separator to the table.
		/// </summary>
		/// <returns>This object.</returns>
		public ConsoleTable AddSeparator()
		{
			Rows.Add(new Row { RowValues = new object[0], SeparateHere = true });
			return this;
		}

		/// <summary>
		/// Adds a row to the table.
		/// </summary>
		/// <param name="row">The row to add. The number of elements per row must be equal to the defined columns per row (found in the constructor).</param>
		/// <returns>This object.</returns>
		public ConsoleTable AddRow(params object[] row)
		{
			if (row.Length != MaxTextLengthPerColumn.Length)
				throw new ArgumentException("Incorrect rows given.");

			for (var c = 0; c < row.Length; ++c)
			{
				var str = row[c].ToString() ?? string.Empty;
				if (str.Length > MaxTextLengthPerColumn[c])
					MaxTextLengthPerColumn[c] = str.Length;
			}

			Rows.Add(new Row { RowValues = row, SeparateHere = false });
			return this;
		}

		/// <summary>
		/// Adds a row to the table. Unlike the <code>AddRow</code> method, this particular method accounts for any new lines found in any columns.
		/// </summary>
		/// <param name="row">The row to add. The number of elements per row must be equal to the defined columns per row (found in the constructor).</param>
		/// <returns>This object.</returns>
		public ConsoleTable AddRowContainingNewLine(params object[] row)
		{
			if (row.Length != MaxTextLengthPerColumn.Length)
				throw new ArgumentException("Incorrect rows given.");

			var strArr = row.Select(x => x.ToString() ?? string.Empty).ToArray();
			var strArr2d = strArr
				.Select(x => x.Split("\n"))
				.ToArray();
			var maxLength = strArr2d
				.Select(x => x.Length)
				.OrderByDescending(x => x)
				.First();

			var parsedRows = new List<object[]>(maxLength);
			for (var i = 0; i < maxLength; ++i)
			{
				parsedRows.Add(new object[row.Length]);
				Array.Fill(parsedRows[i], string.Empty);
			}

			for (var r = 0; r < strArr2d.Length; ++r)
			{
				for (var c = 0; c < strArr2d[r].Length; ++c)
					parsedRows[c][r] = strArr2d[r][c];
			}

			foreach (var elem in parsedRows)
				AddRow(elem);

			return this;
		}

		/// <summary>
		/// Returns the string representation of this Table; that is, returns the constructed table. 
		/// </summary>
		/// <returns>The table.</returns>
		public override string ToString()
		{
			for (var c = 0; c < MaxTextLengthPerColumn.Length; ++c)
				MaxTextLengthPerColumn[c] += 2;

			var sb = new StringBuilder();
			sb.Append(GenerateLine(Position.Top))
				.AppendLine();

			for (var r = 0; r < Rows.Count; ++r)
			{
				if (Rows[r].SeparateHere)
					sb.AppendLine(GenerateLine(Position.Between));
				else
				{
					for (var c = 0; c < Rows[r].RowValues.Length; ++c)
					{
						sb.Append(VerticalLine);
						var objStr = Rows[r].RowValues[c].ToString() ?? string.Empty;
						sb.Append(objStr);
						sb.Append(Repeat(" ", MaxTextLengthPerColumn[c] - objStr.Length));
					}

					sb.Append(VerticalLine)
						.AppendLine();
				}
			}

			sb.Append(GenerateLine(Position.Bottom));
			return sb.ToString();
		}

		/// <summary>
		/// Generates a line based on the position (of the table) given. For example, if you specify the top position (the top of the table), this method will return the formatted top part of the table. 
		/// </summary>
		/// <param name="position">The position.</param>
		/// <returns>The formatted line.</returns>
		public string GenerateLine(Position position)
		{
			var sb = new StringBuilder();
			switch (position)
			{
				case Position.Top:
					{
						sb.Append(TopLeftJoint);
						for (var c = 0; c < MaxTextLengthPerColumn.Length; ++c)
						{
							sb.Append(Repeat(HorizontalLine, MaxTextLengthPerColumn[c]));
							if (c + 1 != MaxTextLengthPerColumn.Length)
								sb.Append(TopJoint);
						}

						sb.Append(TopRightJoint);
						break;
					}
				case Position.Between:
					{
						sb.Append(LeftJoint);
						for (var c = 0; c < MaxTextLengthPerColumn.Length; ++c)
						{
							sb.Append(Repeat(HorizontalLine, MaxTextLengthPerColumn[c]));
							if (c + 1 != MaxTextLengthPerColumn.Length)
								sb.Append(MiddleJoint);
						}

						sb.Append(RightJoint);
						break;
					}
				case Position.Bottom:
					{
						sb.Append(BottomLeftJoint);
						for (var c = 0; c < MaxTextLengthPerColumn.Length; ++c)
						{
							sb.Append(Repeat(HorizontalLine, MaxTextLengthPerColumn[c]));
							if (c + 1 != MaxTextLengthPerColumn.Length)
								sb.Append(BottomJoint);
						}

						sb.Append(BottomRightJoint);
						break;
					}
				default:
					throw new ArgumentOutOfRangeException(nameof(position), position, null);
			}

			return sb.ToString();
		}

		private static string Repeat(string str, int count)
			=> string.Join("", Enumerable.Repeat(str, count));
	}

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

	public enum Position
	{
		/// <summary>
		/// The top part of the table.
		/// </summary>
		Top,

		/// <summary>
		/// Any area of the table that isn't the top or bottom.
		/// </summary>
		Between,

		/// <summary>
		/// The bottom part of the table.
		/// </summary>
		Bottom
	}
}