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

		[Tooltip("Invoked when the remote host accepts the input")]
		public UnityEvent SeatAccepted;

		[Tooltip("Invoked when the remote host responds that the seat is not available for some reason")]
		public UnityEvent SeatRejected;

		[Tooltip("Invoked when the remote host responds that the seat is already taken by another user")]
		public UnityEvent SeatTaken;
		
		/// <summary>
		/// This key activates the Seat chosing flow from remote host
		/// </summary>
		private const string seatSelectKey = "SeatSelect";

		/// <summary>
		/// This is the key of the shape, rows and seats array from remote host 
		/// </summary>
		private const string seatLayoutKey = "SeatLayout";

		/// <summary>
		/// This is the key of the seats array from remote host (an alternative format)
		/// </summary>
		private const string seatSeatsKey = "SeatSeats";

		/// <summary>
		/// This key is used to send the selected seat to the host
		/// </summary>
		private const string seatInputKey = "SeatInput";

		/// <summary>
		/// This is the key of the result of chosing the seat from remote host (whether it was succesful or collided with another input)
		/// </summary>
		private const string seatInputResponseKey = "SeatInputResponse";

		/// <summary>
		/// The response contained in the seatInputResponse property if the chosen seat was accepted
		/// </summary>
		private const string seatAcceptedValue = "Accepted";

		/// <summary>
		/// The response contained in the seatInputResponse property if the chosen seat was rejected for some reason other than being taken by another user
		/// </summary>
		private const string seatRejectedValue = "Rejected";

		/// <summary>
		/// The response contained in the seatInputResponse property if the chosen seat was chosen by another user already
		/// </summary>
		private const string seatTakenValue = "Taken";

		private List<Seat> SeatList = new List<Seat> ();

		public class Seat {
			public string Row;
			public string Column;
			public string SeatNumber;
			public Dictionary<string, object> Metadata;
		}

		public void Start () {
			SendButton.onClick.AddListener (SendHostMessage);
		}

		public void Setup (string seatDataJson) {
			SeatList = JsonConvert.DeserializeObject<List<Seat>> (seatDataJson);

			gameObject.SetActive (true);
			SendButton.interactable = false;

			RowDropdown.ClearOptions ();
			SeatDropdown.ClearOptions ();

			var rowOptions = new List<Dropdown.OptionData> ();

			var usedRows = new List<string> ();

			foreach (var seat in SeatList) {
				if (!usedRows.Contains (seat.Row)) {
					usedRows.Add (seat.Row);
				}
			}

			foreach (var row in usedRows) {
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
			var seatOptions = new List<Dropdown.OptionData> ();

			foreach (Seat seat in SeatList) {
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
			if (dataObj.ContainsKey (seatSeatsKey)) {
				var seatDataJson = Encoding.ASCII.GetString (dataObj.GetByteArray (seatSeatsKey).Bytes);
				Setup (seatDataJson);
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
