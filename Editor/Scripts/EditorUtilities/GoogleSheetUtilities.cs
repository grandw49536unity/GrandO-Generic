using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using GrandO.Generic;

namespace GrandO.Generic.Editor {

    public static class GoogleSheetUtilities {

        public const string GOOGLE_SHEET_CSV_URL = "https://docs.google.com/spreadsheet/ccc?key={0}&usp=sharing&output=csv&id=KEY&gid={1}";

        public static async void LoadGoogleSheetRawData(string _sheetId, string _gridId, UnityAction<string> _callback) {
            UnityWebRequest request = UnityWebRequest.Get(string.Format(GOOGLE_SHEET_CSV_URL, _sheetId, _gridId));
            await request.SendWebRequest();
            _callback.Invoke(request.downloadHandler.text);
        }

        public static async void LoadGoogleSheet(string _sheetId, string _gridId, UnityAction<string[][]> _callback) {
            UnityWebRequest request = UnityWebRequest.Get(string.Format(GOOGLE_SHEET_CSV_URL, _sheetId, _gridId));
            await request.SendWebRequest();
            _callback.Invoke(CSVUtilities.GetCSVTable(request.downloadHandler.text));
        }

    }

}