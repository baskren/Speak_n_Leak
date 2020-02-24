﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;

namespace Xamarin.Ess
{
    public static class PlatformRenderer
    {
        const bool disposeWhenDone = true;

        public static void Init()
        {
            TextToSpeech.PlatformSpeakAsync = IosSpeakAsync;
            TextToSpeech.PlatformGetLocalesAsync = PlatformGetLocalesAsync;
        }

        internal static Task<IEnumerable<Locale>> PlatformGetLocalesAsync() =>
            Task.FromResult(AVSpeechSynthesisVoice.GetSpeechVoices()
                .Select(v => new Locale(v.Language, null, v.Language, v.Identifier)));


        internal static async Task IosSpeakAsync(string text, SpeechOptions options, CancellationToken cancelToken = default)
        {
            try
            {
                WeakReference weakRef = null;
                using (var speechUtterance = GetSpeechUtterance(text, options))
                {
                    weakRef = new WeakReference(speechUtterance);
                    await SpeakUtterance(text, speechUtterance, cancelToken);

                    /*
                    if (disposeWhenDone)
                    {
                        speechUtterance.Voice.Dispose();
                        speechUtterance.AttributedSpeechString?.Dispose();
                        speechUtterance.Dispose();
                        System.Diagnostics.Debug.WriteLine("UTTERANCE DISPOSED");
                    }
                    */
                }


                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (weakRef.IsAlive)
                    System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss.fffffffK") + "] PlatformRenderer.IosSpeakAsync: text=[" + text + "] weakRef.IsAlive speechUtterance.RetainCount=[" + ((NSObject)weakRef.Target).RetainCount + "]");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("PlatformRenderer.IosSpeakAsync:" + " EXCEPTION [" + e + "]");
            }
        }

        static AVSpeechUtterance GetSpeechUtterance(string text, SpeechOptions options)
        {
            var speechUtterance = new AVSpeechUtterance(text);

            if (options != null)
            {
                // null voice if fine - it is the default
                speechUtterance.Voice =
                    AVSpeechSynthesisVoice.FromLanguage(options.Locale?.Language) ??
                    AVSpeechSynthesisVoice.FromLanguage(AVSpeechSynthesisVoice.CurrentLanguageCode);

                // the platform has a range of 0.5 - 2.0
                // anything lower than 0.5 is set to 0.5
                if (options.Pitch.HasValue)
                    speechUtterance.PitchMultiplier = options.Pitch.Value;

                if (options.Volume.HasValue)
                    speechUtterance.Volume = options.Volume.Value;

            }

            speechUtterance.PreUtteranceDelay = 0;
            speechUtterance.PostUtteranceDelay = 0;
            speechUtterance.Volume = 1;
            speechUtterance.Rate = 0.55f;

            return speechUtterance;
        }

        internal static async Task SpeakUtterance(string text, AVSpeechUtterance speechUtterance, CancellationToken cancelToken)
        {
            bool disposed = false;
            var tcsUtterance = new TaskCompletionSource<bool>();
            var speechSynthesizer = new AVSpeechSynthesizer();
            var weakRef = new WeakReference(speechSynthesizer);
            try
            {
                speechSynthesizer.DidFinishSpeechUtterance += OnFinishedSpeechUtterance;
                speechSynthesizer.SpeakUtterance(speechUtterance);
                using (cancelToken.Register(TryCancel))
                {
                    await tcsUtterance.Task;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("PlatformRenderer.SpeakUtterance:" + " EXCEPTION [" + e + "]");
            }
            finally
            {
                speechSynthesizer.DidFinishSpeechUtterance -= OnFinishedSpeechUtterance;

            }

            void TryCancel()
            {
                speechSynthesizer?.StopSpeaking(AVSpeechBoundary.Word);
                Dispose();
                tcsUtterance?.TrySetResult(true);
            }

            void OnFinishedSpeechUtterance(object sender, AVSpeechSynthesizerUteranceEventArgs args)
            {
                if (speechUtterance == args.Utterance)
                {
                    Dispose();
                    tcsUtterance?.TrySetResult(true);
                }
            }

            void Dispose()
            {
                if (!disposed && disposeWhenDone)
                {
                    disposed = true;
                    speechSynthesizer.Delegate?.Dispose();
                    speechSynthesizer.Delegate = null;
                    speechSynthesizer.WeakDelegate?.Dispose();
                    speechSynthesizer.WeakDelegate = null;
                    speechSynthesizer.Dispose();
                    System.Diagnostics.Debug.WriteLine("SYNTHESIZER DISPOSED");
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (weakRef.IsAlive)
                    System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss.fffffffK") + "] PlatformRenderer.SpeakUtterance: text=[" + text + "]  weakRef.IsAlive  speechSynthesizer.RetainCount=[" + speechSynthesizer.RetainCount + "]");
            }
        }
    }
}
