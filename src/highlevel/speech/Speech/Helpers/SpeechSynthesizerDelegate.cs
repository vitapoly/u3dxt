using System;
using U3DXT.iOS.Native.AVFoundation;
using U3DXT.iOS.Native.Foundation;

namespace U3DXT.iOS.Speech.Helpers {
	internal class SpeechSynthesizerDelegate : AVSpeechSynthesizerDelegate {

		private static SpeechSynthesizerDelegate _instance;
		public static SpeechSynthesizerDelegate instance {
			get {
				if (_instance == null)
					_instance = new SpeechSynthesizerDelegate();
				return _instance;
			}
		}

		public SpeechSynthesizerDelegate() {
		}

		public override void DidCancel(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance) {
			SpeechXT._OnCancelled(utterance);
		}

		public override void DidContinue(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance) {
			SpeechXT._OnContinued(utterance);
		}

		public override void DidFinish(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance) {
			SpeechXT._OnFinished(utterance);
		}

		public override void DidPause(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance) {
			SpeechXT._OnPaused(utterance);
		}

		public override void DidStart(AVSpeechSynthesizer synthesizer, AVSpeechUtterance utterance) {
			SpeechXT._OnStarted(utterance);
		}

		public override void WillSpeakRangeOfSpeechString(AVSpeechSynthesizer synthesizer, NSRange characterRange, AVSpeechUtterance utterance) {
			SpeechXT._OnWillSpeak(utterance, characterRange);
		}
	}
}

