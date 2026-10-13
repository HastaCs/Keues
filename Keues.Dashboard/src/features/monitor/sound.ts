type AudioContextConstructor = typeof AudioContext;

let audioContext: AudioContext | null = null;

function getAudioContext(): AudioContext | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const constructor: AudioContextConstructor | undefined =
    window.AudioContext ??
    (window as unknown as { webkitAudioContext?: AudioContextConstructor }).webkitAudioContext;

  if (!constructor) {
    return null;
  }

  if (!audioContext) {
    audioContext = new constructor();
  }

  return audioContext;
}

export function primeAudio(): void {
  const context = getAudioContext();
  if (context && context.state === 'suspended') {
    void context.resume();
  }
}

export function playBeep(): void {
  const context = getAudioContext();
  if (!context) {
    return;
  }

  void context.resume();

  const oscillator = context.createOscillator();
  const gain = context.createGain();

  oscillator.type = 'sine';
  oscillator.frequency.value = 880;

  gain.gain.setValueAtTime(0.0001, context.currentTime);
  gain.gain.exponentialRampToValueAtTime(0.3, context.currentTime + 0.01);
  gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.35);

  oscillator.connect(gain);
  gain.connect(context.destination);

  oscillator.start();
  oscillator.stop(context.currentTime + 0.37);
}
