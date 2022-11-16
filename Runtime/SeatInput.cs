using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Sfs2X.Entities.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	public class SeatInput : ReplicatedComponent {
		public Dropdown RowDropdown;
		public Dropdown SeatDropdown;
		public Button SendButton;
		public UnityEvent SeatRejected;
		public UnityEvent SeatTaken;

		private string openkey = "SeatOpenScreen";
		private string sendkey = "SeatInput";
		private string getkey = "SeatInputFeedback";
		private List<SeatRow> rowList = new List<SeatRow> ();
		private List<Seat> seatList = new List<Seat> ();

		public class SeatRow {
			public string Row;
			public List<object> Data;
		}

		public class Seat {
			public string Row;
			public string Column;
			public string SeatNumber;
			public Dictionary<string, object> Metadata;
		}

		public void Start () {
			SendButton.onClick.AddListener (SendHostMessage);
		}

		public void Setup (byte [] seatData) {
			gameObject.SetActive (true);
			SendButton.interactable = false;

			string seatDataString = Encoding.ASCII.GetString (seatData);
			rowList = JsonConvert.DeserializeObject<List<SeatRow>> (seatDataString);

			foreach (SeatRow row in rowList) {
				foreach (object seat in row.Data) {
					if (seat != null) {
						Dictionary<string, object> seatInfo = JsonConvert.DeserializeObject<Dictionary<string, object>> (seat.ToString ());
						Seat newSeat = new Seat ();
						newSeat.Row = seatInfo ["row"].ToString ();
						newSeat.Column = seatInfo ["col"].ToString ();
						newSeat.SeatNumber = seatInfo ["seat"].ToString ();
						newSeat.Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>> (seatInfo ["metadata"].ToString ());

						seatList.Add (newSeat);
					}
				}
			}

			RowDropdown.ClearOptions ();
			SeatDropdown.ClearOptions ();

			List<Dropdown.OptionData> rowOptions = new List<Dropdown.OptionData> ();

			List<string> UsedRows = new List<string> ();

			foreach (Seat seat in seatList) {
				if (!UsedRows.Contains (seat.Row)) {
					UsedRows.Add (seat.Row);
				}
			}

			foreach (string row in UsedRows) {
				rowOptions.Add (new Dropdown.OptionData {
					text = row
				});
			}

			RowDropdown.AddOptions (rowOptions);

			RowDropdown.onValueChanged.AddListener (delegate {
				UpdateSeatDropdown ();
			});

			UpdateSeatDropdown ();
		}

		private void UpdateSeatDropdown () {
			SeatDropdown.ClearOptions ();
			List<Dropdown.OptionData> seatOptions = new List<Dropdown.OptionData> ();

			foreach (Seat seat in seatList) {
				if (seat.Row == RowDropdown.options [RowDropdown.value].text) {
					seatOptions.Add (new Dropdown.OptionData {
						text = seat.SeatNumber
					});
				}
			}

			SeatDropdown.AddOptions (seatOptions);

			SendButton.interactable = true;
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (openkey)) {
				Setup (dataObj.GetByteArray (openkey).Bytes);
			}

			if (dataObj.ContainsKey (getkey)) {
				string status = dataObj.GetUtfString (getkey);

				switch (status) {
				case "Accepted":
					gameObject.SetActive (false);
					break;
				case "Rejected":
					SeatRejected.Invoke ();
					break;
				case "Taken":
					SeatTaken.Invoke ();
					break;
				default:
					break;
				}

				SendButton.interactable = true;
			}
		}

		void SendHostMessage () {
			string seatInput = "R" + RowDropdown.options [RowDropdown.value].text + "S" + SeatDropdown.options [SeatDropdown.value].text;

			if (Application.isEditor) {
				Debug.LogFormat ("{0} SendSeatInputComponent: Sending host message '{1}'", gameObject.GetScenePath (), seatInput);
			} else {
				Send (sendkey, seatInput);
				SendButton.interactable = false;
			}
		}
	}

}
