#if (UNITY_EDITOR || UNITY_IPHONE)

using System;
using U3DXT.iOS.Internals;
using U3DXT.iOS.Native.GameKit;
using System.Linq;

namespace U3DXT.iOS.GameKit {

	/// <summary>
	/// This class represents a participant in a turn-based Game Center match.
	/// </summary>
	public class TurnBasedParticipant : NSObjectWrapper {

		internal Player _player = null;

		/// <summary>
		/// Default constructor.  You should not have to call this manually.
		/// You get participants by calling a TurnBasedMatch instance's Participants property.
		/// </summary>
		public TurnBasedParticipant() {
		}

		/// <summary>
		/// The low-level GKTurnBasedParticipant object if you need more advanced functionalities.
		/// </summary>
		/// <value>The native participant.</value>
		public GKTurnBasedParticipant gkTurnBasedParticipant {
			get { return _nsObject as GKTurnBasedParticipant; }
		}

		/// <summary>
		/// The date and time that this participant last took a turn in the game.
		/// </summary>
		/// <value>The last turn date.</value>
		public DateTime lastTurnDate {
			get { return gkTurnBasedParticipant.lastTurnDate; }
		}

		/// <summary>
		/// The end-state of this participant in the match.
		/// </summary>
		/// <value>The match outcome.</value>
		public GKTurnBasedMatchOutcome matchOutcome {
			get { return gkTurnBasedParticipant.matchOutcome; }
			set { gkTurnBasedParticipant.matchOutcome = value; }
		}

		/// <summary>
		/// The player for this participant.
		/// It may be null if this slot in the match has not yet been filled by an actual player.
		/// </summary>
		/// <value>The player.</value>
		public Player player {
			get {
				if (_player != null) {
					if (_player.playerID == playerID)
						return _player;
					_player = null;
				}
				return _player;
			}
		}

		/// <summary>
		/// The player identifier for this participant.
		/// It may be empty if this slot in the match has not yet been filled by an actual player.
		/// </summary>
		/// <value>The player I.</value>
		public string playerID {
			get { return gkTurnBasedParticipant.playerID; }
		}

		/// <summary>
		/// The current status of the participant.
		/// </summary>
		/// <value>The status.</value>
		public GKTurnBasedParticipantStatus status {
			get { return gkTurnBasedParticipant.status; }
		}

		/// <summary>
		/// The date and time that the participant’s turn times out.
		/// </summary>
		/// <value>The timeout date.</value>
		public DateTime timeoutDate {
			get { return gkTurnBasedParticipant.timeoutDate; }
		}

		internal static GKTurnBasedParticipant[] ToGKParticipants(TurnBasedParticipant[] participants) {
			return participants.Select(x => x.gkTurnBasedParticipant).ToArray();
		}

		internal static TurnBasedParticipant[] ToParticipants(object[] gkParticipants) {
			return gkParticipants.Select(x => NSObjectWrapper.CreateWrapper(typeof(TurnBasedParticipant), x as GKTurnBasedParticipant) as TurnBasedParticipant).ToArray();
		}
	}
}

#endif
