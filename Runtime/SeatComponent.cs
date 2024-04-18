using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Sfs2X.Entities.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	public class SeatComponent : ReplicatedComponent {
		
		
		
		public Dropdown RowDropdown;
		public Dropdown SeatDropdown;
		public Button SendButton;
		public UnityEvent SeatAccepted;
		public UnityEvent SeatRejected;
		public UnityEvent SeatTaken;
		
		// These keys are sent from/to CineGame Host:
		// This key prompts you to select a seat.
		private const string seatSelectKey = "SeatSelect";

		// This key contains the shape, rows and seats in the cinema hall (This is not used for this version of the SeatComponent).
		private const string seatLayoutKey = "SeatLayout";

		// This key contains the seats in the cinema hall.
		private const string seatSeatsKey = "SeatSeats";

		// This key is sent to CineGame Host and contains information about which seat has been selected.
		private const string seatInputKey = "SeatInput";

		// This key contains response on whether the user got his/her seat or not.
		private const string seatInputResponseKey = "SeatInputResponse";

		// These are the different values ​​that seat response can contain.
		private const string seatAcceptedValue = "Accepted";
		private const string seatRejectedValue = "Rejected";
		private const string seatTakenValue = "Taken";

		private List<Seat> seatList = new List<Seat> ();

		public class Seat {
			public string Row;
			public string Column;
			public string SeatNumber;
			public Dictionary<string, object> Metadata;
		}

		public void Start () {
			SendButton.onClick.AddListener (SendHostMessage);

		}

		public void Setup (ISFSObject seatData) {
			gameObject.SetActive (true);
			SendButton.interactable = false;

			string seatDataString = Encoding.ASCII.GetString (seatData.GetByteArray (seatSeatsKey).Bytes);
			seatList = JsonConvert.DeserializeObject<List<Seat>> (seatDataString);

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
			if (dataObj.ContainsKey (seatSelectKey)) {
				Setup (dataObj);
			}

			if (dataObj.ContainsKey (seatInputResponseKey)) {
				string status = dataObj.GetUtfString (seatInputResponseKey);
				
				if (status == seatAcceptedValue) 		SeatAccepted?.Invoke();
				else if (status == seatRejectedValue) 	SeatRejected?.Invoke();
				else if (status == seatTakenValue) 		SeatTaken?.Invoke();
			}
		}

		void SendHostMessage () {
			string seatInput = "R" + RowDropdown.options [RowDropdown.value].text + "S" + SeatDropdown.options [SeatDropdown.value].text;

			if (Application.isEditor) {
				Debug.LogFormat ("{0} SendSeatInputComponent: Sending host message '{1}'", gameObject.GetScenePath (), seatInput);
			} else {
				Send (seatInputKey, seatInput);
				SendButton.interactable = false;
			}
		}
	}

}
