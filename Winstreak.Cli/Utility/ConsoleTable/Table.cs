using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Winstreak.Cli.Utility.ConsoleTable
{
	/// <summary>
	/// A class that can create a table which can be displayed in the Console.
	/// </summary>
	public class Table
	{
		private const int ExtraSpace = 1; 
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

		private static readonly Regex AnsiRegex = new(@"\x1B\[[^@-~]*[@-~]");

		/// <summary>
		/// The rows in this table.
		/// </summary>
		public IList<Row> Rows { get; }

		/// <summary>
		/// The maximum length of each column.
		/// </summary>
		public int[] MaxTextLengthPerColumn { get; }

		/// <summary>
		/// The new line string to use for this instance.
		/// </summary>
		public string NewLine { get; }

		/// <summary>
		/// Creates a new Table object. 
		/// </summary>
		/// <param name="columnPerRow">The number of columns per row.</param>
		public Table(int columnPerRow) : this(columnPerRow, "\n")
		{
		}

		/// <summary>
		/// Creates a new Table object.
		/// </summary>
		/// <param name="columnPerRow">The number of columns per row.</param>
		/// <param name="newLine">The new line definition to use.</param>
		public Table(int columnPerRow, string newLine)
		{
			if (columnPerRow <= 0)
				throw new ArgumentException("You cannot have a row with 0 or less columns.", nameof(columnPerRow));
			Rows = new List<Row>();
			MaxTextLengthPerColumn = new int[columnPerRow];
			NewLine = newLine;
		}

		/// <summary>
		/// Adds a separator to the table.
		/// </summary>
		/// <returns>This object.</returns>
		public Table AddSeparator()
		{
			Rows.Add(new Row {RowValues = new object[0], SeparateHere = true});
			return this;
		}

		/// <summary>
		/// Adds a row to the table.
		/// </summary>
		/// <param name="row">The row to add. The number of elements per row must be equal to the defined columns per row (found in the constructor).</param>
		/// <returns>This object.</returns>
		public Table AddRow(params object[] row)
		{
			if (row.Length != MaxTextLengthPerColumn.Length)
				throw new ArgumentException("Incorrect rows given.");

			for (var c = 0; c < row.Length; ++c)
			{
				var str = row[c].ToString() ?? string.Empty;
				var removedAnsiStr = AnsiRegex.Replace(str, string.Empty);
				if (removedAnsiStr.Length > MaxTextLengthPerColumn[c])
					MaxTextLengthPerColumn[c] = removedAnsiStr.Length;
			}

			Rows.Add(new Row {RowValues = row, SeparateHere = false});
			return this;
		}

		/// <summary>
		/// Adds a row to the table. Unlike the <code>AddRow</code> method, this particular method accounts for any new lines found in any columns.
		/// </summary>
		/// <param name="row">The row to add. The number of elements per row must be equal to the defined columns per row (found in the constructor).</param>
		/// <returns>This object.</returns>
		public Table AddRowContainingNewLine(params object[] row)
		{
			if (row.Length != MaxTextLengthPerColumn.Length)
				throw new ArgumentException("Incorrect rows given.");

			var strArr = row.Select(x => x.ToString() ?? string.Empty).ToArray();
			var strArr2d = strArr
				.Select(x => x.Split(NewLine))
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
				MaxTextLengthPerColumn[c] += ExtraSpace;

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
						sb.Append(Repeat(" ",
							MaxTextLengthPerColumn[c] - AnsiRegex.Replace(objStr, string.Empty).Length));
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
}