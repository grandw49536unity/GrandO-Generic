using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace GrandO.Generic {

	public static class CSVUtilities {

		public static string[][] GetCSVTable(string _csvRawData, bool _removeFirstRow = true) {
			Regex regex = new Regex(",(?=(?:[^\\\"]*\\\"[^\\\"]*\\\")*[^\\\"]*$)");
			string[] rows = _csvRawData.Split("\n");
			int rowCount = rows.Length;
			string[][] table = new string[rowCount][];
			for (int i = 0; i < rowCount; i++) {
				string[] row = regex.Split(rows[i]);
				for (int j = 0; j < row.Length; j++) {
					row[j] = row[j].Replace("\r", "");
				}
				table[i] = row;
			}
			if (_removeFirstRow && rowCount >= 1) {
				string[][] tableTrim = new string[rowCount - 1][];
				for (int i = 0; i < rowCount - 1; i++) {
					tableTrim[i] = table[i + 1];
				}
				return tableTrim;
			}
			return table;
		}

		public static string CreateCSVData(string[][] _table) {
			string csvData = "";
			int rowCount = _table.Length;
			for (int i = 0; i < rowCount; i++) {
				string[] row = _table[i];
				int colCount = row.Length;
				for (int j = 0; j < colCount; j++) {
					string col = row[j];
					if (col.Contains(',') || col.Contains('\"')) {
						col = $"\"{col}\"";
					}
					csvData += col;
					if (j != colCount - 1) {
						csvData += ",";
					}
				}
				if (i != rowCount - 1) {
					csvData += "\n";
				}
			}
			return csvData;
		}

	}

}