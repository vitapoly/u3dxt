using System;
using System.Reflection;
using System.Runtime.InteropServices;
using U3DXT.Core;
using U3DXT.iOS.Native.UIKit;
using U3DXT.iOS.Native.Foundation;
using UnityEngine;
using U3DXT.Utils;
using U3DXT.iOS.Native.AVFoundation;
using U3DXT.iOS.Speech.Helpers;
using System.Linq;

namespace U3DXT.iOS.Speech {
	/// <summary>
	/// Main class for iOS Speech plugin. High-level API.</summary>
	/// <remarks>
	/// </remarks>
	public class SpeechXT {

#region events

		private static EventHandler<SpeechEventArgs> _speechStoppedHandlers;
		/// <summary>
		/// Occurs when a speech has stopped.
		/// </summary>
		public static event EventHandler<SpeechEventArgs> SpeechStopped {
			add { _speechStoppedHandlers += value; }
			remove { _speechStoppedHandlers -= value; }
		}

		private static EventHandler<SpeechEventArgs> _speechContinuedHandlers;
		/// <summary>
		/// Occurs when a speech has continued.
		/// </summary>
		public static event EventHandler<SpeechEventArgs> SpeechContinued {
			add { _speechContinuedHandlers += value; }
			remove { _speechContinuedHandlers -= value; }
		}

		private static EventHandler<SpeechEventArgs> _speechFinishedHandlers;
		/// <summary>
		/// Occurs when a speech has finished.
		/// </summary>
		public static event EventHandler<SpeechEventArgs> SpeechFinished {
			add { _speechFinishedHandlers += value; }
			remove { _speechFinishedHandlers -= value; }
		}

		private static EventHandler<SpeechEventArgs> _speechPausedHandlers;
		/// <summary>
		/// Occurs when a speech has paused.
		/// </summary>
		public static event EventHandler<SpeechEventArgs> SpeechPaused {
			add { _speechPausedHandlers += value; }
			remove { _speechPausedHandlers -= value; }
		}

		private static EventHandler<SpeechEventArgs> _speechStartedHandlers;
		/// <summary>
		/// Occurs when a speech has started.
		/// </summary>
		public static event EventHandler<SpeechEventArgs> SpeechStarted {
			add { _speechStartedHandlers += value; }
			remove { _speechStartedHandlers -= value; }
		}

		private static EventHandler<SpeechWillSpeakEventArgs> _willSpeakHandlers;
		/// <summary>
		/// Occurs when a portion of a speech is about to be spoken.
		/// </summary>
		public static event EventHandler<SpeechWillSpeakEventArgs> WillSpeak {
			add { _willSpeakHandlers += value; }
			remove { _willSpeakHandlers -= value; }
		}

#endregion

#region vars

		private static AVSpeechSynthesizer _syn;
		private static AVSpeechUtterance _settings;

#endregion

#region methods

		/// <summary>
		/// Indicates whether the current device/iOS version supports speech synthesis.
		/// </summary>
		/// <value><c>true</c> if is supported; otherwise, <c>false</c>.</value>
		public static bool isSynthesisSupported {
			get { return AVSpeechSynthesizer.ClassExists; }
		}

		/// <summary>
		/// Speak the specified text.
		/// </summary>
		/// <param name="text">Text.</param>
		public static void Speak(string text) {
			if (_syn == null) {
				_syn = new AVSpeechSynthesizer();
				_syn.Delegate = SpeechSynthesizerDelegate.instance;
			}

			var utterance = new AVSpeechUtterance(text);

			if (_settings != null) {
				utterance.pitchMultiplier = _settings.pitchMultiplier;
				utterance.postUtteranceDelay = _settings.postUtteranceDelay;
				utterance.preUtteranceDelay = _settings.preUtteranceDelay;
				utterance.rate = _settings.rate;
				utterance.voice = _settings.voice;
				utterance.volume = _settings.volume;
			}

			_syn.SpeakUtterance(utterance);
		}

		/// <summary>
		/// Pause speech.
		/// </summary>
		public static bool Pause() {
			if (_syn == null)
				return false;
			return _syn.PauseSpeakingAtBoundary(AVSpeechBoundary.Word);
		}

		/// <summary>
		/// Continue speech.
		/// </summary>
		public static bool Continue() {
			if (_syn == null)
				return false;
			return _syn.ContinueSpeaking();
		}

		/// <summary>
		/// Stop speech.
		/// </summary>
		public static bool Stop() {
			if (_syn == null)
				return false;
			return _syn.StopSpeakingAtBoundary(AVSpeechBoundary.Word);
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="U3DXT.iOS.Speech.SpeechXT"/> is paused.
		/// </summary>
		/// <value><c>true</c> if is paused; otherwise, <c>false</c>.</value>
		public static bool isPaused {
			get {
				if (_syn == null)
					return false;
				return _syn.paused;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="U3DXT.iOS.Speech.SpeechXT"/> is speaking.
		/// </summary>
		/// <value><c>true</c> if is speaking; otherwise, <c>false</c>.</value>
		public static bool isSpeaking {
			get {
				if (_syn == null)
					return false;
				return _syn.speaking;
			}
		}

		/// <summary>
		/// The settings for speech. Change it only affects the next time you call Speak().
		/// </summary>
		/// <value>The settings.</value>
		public static AVSpeechUtterance settings {
			get {
				if (_settings == null)
					_settings = new AVSpeechUtterance("");
				return _settings;
			}
		}

		/// <summary>
		/// Gets the available voices.
		/// </summary>
		/// <value>The available voices.</value>
		public static AVSpeechSynthesisVoice[] availableVoices {
			get {
				var voices = AVSpeechSynthesisVoice.SpeechVoices();
				return voices.Cast<AVSpeechSynthesisVoice>().ToArray();
			}
		}

		/// <summary>
		/// Gets the current voice.
		/// </summary>
		/// <value>The current voice.</value>
		public static AVSpeechSynthesisVoice currentLocaleVoice {
			get {
				return AVSpeechSynthesisVoice.Voice(AVSpeechSynthesisVoice.CurrentLanguageCode());
			}
		}

		internal static void _OnCancelled(AVSpeechUtterance utterance) {
			if (_speechStoppedHandlers != null)
				_speechStoppedHandlers(null, new SpeechEventArgs(utterance));
		}

		internal static void _OnContinued(AVSpeechUtterance utterance) {
			if (_speechContinuedHandlers != null)
				_speechContinuedHandlers(null, new SpeechEventArgs(utterance));
		}

		internal static void _OnFinished(AVSpeechUtterance utterance) {
			if (_speechFinishedHandlers != null)
				_speechFinishedHandlers(null, new SpeechEventArgs(utterance));
		}

		internal static void _OnPaused(AVSpeechUtterance utterance) {
			if (_speechPausedHandlers != null)
				_speechPausedHandlers(null, new SpeechEventArgs(utterance));
		}

		internal static void _OnStarted(AVSpeechUtterance utterance) {
			if (_speechStartedHandlers != null)
				_speechStartedHandlers(null, new SpeechEventArgs(utterance));
		}

		internal static void _OnWillSpeak(AVSpeechUtterance utterance, NSRange characterRange) {
			if (_willSpeakHandlers != null)
				_willSpeakHandlers(null, new SpeechWillSpeakEventArgs(utterance, characterRange));
		}

#endregion
	}
}
