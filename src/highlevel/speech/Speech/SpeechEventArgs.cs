using System;
using U3DXT.iOS.Native.AVFoundation;

namespace U3DXT.iOS.Speech {
	/// <summary>
	/// Speech event arguments.
	/// </summary>
	public class SpeechEventArgs : EventArgs {

		/// <summary>
		/// The utterance.
		/// </summary>
		public readonly AVSpeechUtterance utterance;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Speech.SpeechEventArgs"/> class.
		/// </summary>
		/// <param name="utterance">Utterance.</param>
		public SpeechEventArgs(AVSpeechUtterance utterance) {
			this.utterance = utterance;
		}
	}
}

