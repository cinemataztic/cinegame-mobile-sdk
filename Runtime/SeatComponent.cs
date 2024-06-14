using System.Collections.Generic;
using System.Text;
using System.Linq;

using Newtonsoft.Json;

using Sfs2X.Entities.Data;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {
	/// <summary>
	/// Ask the user to select the row and seat number in a cinema. The rows and seats configuration is received from remote host, and the selected row and seat number will be sent back to the remote host, which will respond with either an Accepted, Rejected or Taken status.")]
	/// </summary>
	[ComponentReference ("Ask the user to select the row and seat number in a cinema. The rows and seats configuration is received from remote host, and the selected row and seat number will be sent back to the remote host, which will respond with either an Accepted, Rejected or Taken status.")]
	public class SeatComponent : ReplicatedComponent {

		public Dropdown RowDropdown;
		public Dropdown SeatDropdown;
		public Button SendButton;

		[Tooltip ("Invoked when the remote host accepts the input")]
		public UnityEvent SeatAccepted;

		[Tooltip ("Invoked when the remote host responds that the seat is not available for some reason")]
		public UnityEvent SeatRejected;

		[Tooltip ("Invoked when the remote host responds that the seat is already taken by another user")]
		public UnityEvent SeatTaken;

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

		public override void InitReplication () {
			base.InitReplication ();
			SendButton.onClick.AddListener (SendHostMessage);
			SendButton.interactable = false;
			RowDropdown.interactable = false;
			SeatDropdown.interactable = false;
		}

		void Setup (string seatDataJson) {
			SeatList = JsonConvert.DeserializeObject<List<Seat>> (seatDataJson);

			gameObject.SetActive (true);

			var usedRows = SeatList.Select (seat => seat.Row).ToHashSet ();

			RowDropdown.ClearOptions ();
			RowDropdown.AddOptions (usedRows.Select (row => new Dropdown.OptionData {
				text = row,
			}).ToList ());

			RowDropdown.interactable = true;

			RowDropdown.onValueChanged.AddListener (delegate {
				UpdateSeatDropdown ();
			});

			UpdateSeatDropdown ();
		}

		void UpdateSeatDropdown () {
			SeatDropdown.ClearOptions ();
			var selectedRow = RowDropdown.options [RowDropdown.value].text;
			var seatOptions = SeatList.Where (seat => seat.Row == selectedRow).Select (seat => new Dropdown.OptionData {
				text = seat.SeatNumber
			});
			SeatDropdown.AddOptions (seatOptions.ToList ());

			SeatDropdown.interactable = true;
			SendButton.interactable = true;
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (seatSeatsKey)) {
				var seatDataJson = Encoding.ASCII.GetString (dataObj.GetByteArray (seatSeatsKey).Bytes);
				Setup (seatDataJson);
			}

			if (dataObj.ContainsKey (seatInputResponseKey)) {
				string status = dataObj.GetUtfString (seatInputResponseKey);

				if (status == seatAcceptedValue) SeatAccepted?.Invoke ();
				else if (status == seatRejectedValue) SeatRejected?.Invoke ();
				else if (status == seatTakenValue) SeatTaken?.Invoke ();
				else LogError ("Unknown SeatInputResponse from host: " + status);
			}
		}

		void SendHostMessage () {
			var seatInput = $"R{RowDropdown.options [RowDropdown.value].text}S{SeatDropdown.options [SeatDropdown.value].text}";

			Log ($"SeatComponent sending input '{seatInput}'");
			Send (seatInputKey, seatInput);
			SendButton.interactable = false;
		}
	}

}
