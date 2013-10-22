using System;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.AVFoundation;

namespace U3DXT.iOS.Speech {
	/// <summary>
	/// Speech will speak event arguments.
	/// </summary>
	public class SpeechWillSpeakEventArgs : SpeechEventArgs {

		/// <summary>
		/// The character range.
		/// </summary>
		public readonly NSRange characterRange;

		/// <summary>
		/// Initializes a new instance of the <see cref="U3DXT.iOS.Speech.SpeechWillSpeakEventArgs"/> class.
		/// </summary>
		/// <param name="utterance">Utterance.</param>
		/// <param name="characterRange">Character range.</param>
		public SpeechWillSpeakEventArgs(AVSpeechUtterance utterance, NSRange characterRange) : base(utterance) {
			this.characterRange = characterRange;
		}
	}
}

