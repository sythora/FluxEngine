const hosts = new WeakMap();
const handledKeys = new Set(["ArrowDown", "ArrowLeft", "ArrowRight", "ArrowUp", "Space"]);
let audioContext;
let ambientGain;
let ambientOscillators = [];

function unlockAudio() {
  const AudioContextType = window.AudioContext || window.webkitAudioContext;
  if (!AudioContextType)
    return;
  audioContext ??= new AudioContextType();
  if (audioContext.state === "suspended")
    audioContext.resume().catch(error => console.warn("FluxRunner browser audio is unavailable.", error));
  startAmbientAudio();
}

function startAmbientAudio() {
  if (!audioContext || ambientOscillators.length > 0)
    return;

  ambientGain = audioContext.createGain();
  ambientGain.gain.value = 0;
  ambientGain.connect(audioContext.destination);
  for (const frequency of [55, 82.4, 110]) {
    const oscillator = audioContext.createOscillator();
    oscillator.type = "sine";
    oscillator.frequency.value = frequency;
    oscillator.connect(ambientGain);
    oscillator.start();
    ambientOscillators.push(oscillator);
  }
}

function setAmbientAudio(enabled) {
  if (!audioContext)
    return;
  startAmbientAudio();
  ambientGain.gain.setTargetAtTime(enabled ? 0.018 : 0, audioContext.currentTime, 0.35);
}

export function start(canvas, dotNetReference, logicalWidth, logicalHeight) {
  stop(canvas);
  canvas.tabIndex = 0;
  const state = {
    stopped: false,
    pending: false,
    frame: 0,
    keys: new Set(),
    events: [],
    pointerEvents: [],
    dotNetReference,
    logicalWidth,
    logicalHeight
  };

  state.keyDown = event => {
    if (document.activeElement !== canvas)
      return;
    unlockAudio();
    if (handledKeys.has(event.code))
      event.preventDefault();
    if (event.repeat || state.keys.has(event.code))
      return;
    state.keys.add(event.code);
    state.events.push(`${event.code}|1`);
  };
  state.keyUp = event => {
    if (!state.keys.delete(event.code))
      return;
    state.events.push(`${event.code}|0`);
  };
  state.blur = () => {
    for (const code of state.keys)
      state.events.push(`${code}|0`);
    state.keys.clear();
  };
  state.pointerDown = event => {
    canvas.focus({ preventScroll: true });
    unlockAudio();
    const bounds = canvas.getBoundingClientRect();
    const scale = Math.min(bounds.width / logicalWidth, bounds.height / logicalHeight);
    const offsetX = (bounds.width - logicalWidth * scale) * 0.5;
    const offsetY = (bounds.height - logicalHeight * scale) * 0.5;
    state.pointerEvents.push([
      (event.clientX - bounds.left - offsetX) / scale,
      (event.clientY - bounds.top - offsetY) / scale
    ]);
  };
  state.unlockAudio = unlockAudio;

  canvas.addEventListener("keydown", state.keyDown);
  canvas.addEventListener("keyup", state.keyUp);
  canvas.addEventListener("blur", state.blur);
  canvas.addEventListener("pointerdown", state.pointerDown);
  window.addEventListener("blur", state.blur);
  document.addEventListener("pointerdown", state.unlockAudio);

  const tick = async timestamp => {
    if (state.stopped)
      return;
    if (!state.pending) {
      state.pending = true;
      const events = state.events.splice(0);
      const pointerEvents = state.pointerEvents.splice(0);
      try {
        await state.dotNetReference.invokeMethodAsync(
          "OnAnimationFrame",
          timestamp,
          events,
          pointerEvents,
          canvas.clientWidth,
          canvas.clientHeight);
      } catch (error) {
        console.error("FluxEngine WebAssembly frame failed.", error);
        state.stopped = true;
      } finally {
        state.pending = false;
      }
    }
    if (!state.stopped)
      state.frame = requestAnimationFrame(tick);
  };

  hosts.set(canvas, state);
  state.frame = requestAnimationFrame(tick);
}

export function stop(canvas) {
  const state = hosts.get(canvas);
  if (!state)
    return;
  state.stopped = true;
  cancelAnimationFrame(state.frame);
  canvas.removeEventListener("keydown", state.keyDown);
  canvas.removeEventListener("keyup", state.keyUp);
  canvas.removeEventListener("blur", state.blur);
  canvas.removeEventListener("pointerdown", state.pointerDown);
  window.removeEventListener("blur", state.blur);
  document.removeEventListener("pointerdown", state.unlockAudio);
  hosts.delete(canvas);
}

export function drawFrame(canvas, _canvasWidth, _canvasHeight, logicalWidth, logicalHeight, cameraX, cameraY, zoom, commands) {
  const context = canvas.getContext("2d", { alpha: false });
  if (!context)
    throw new Error("The browser couldn't create a Canvas 2D context.");

  const bounds = canvas.getBoundingClientRect();
  const pixelRatio = window.devicePixelRatio || 1;
  const backingWidth = Math.max(1, Math.round(bounds.width * pixelRatio));
  const backingHeight = Math.max(1, Math.round(bounds.height * pixelRatio));
  if (canvas.width !== backingWidth)
    canvas.width = backingWidth;
  if (canvas.height !== backingHeight)
    canvas.height = backingHeight;

  const clear = commands.find(command => command.type === "clear");
  context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
  context.fillStyle = toCss(clear?.color);
  context.fillRect(0, 0, bounds.width, bounds.height);

  const scaleX = bounds.width / logicalWidth;
  const scaleY = bounds.height / logicalHeight;
  const fitScale = Math.min(scaleX, scaleY);
  const fitOffsetX = (bounds.width - logicalWidth * fitScale) * 0.5;
  const fitOffsetY = (bounds.height - logicalHeight * fitScale) * 0.5;
  const scale = fitScale * zoom;
  const offsetX = bounds.width * 0.5 - cameraX * scale;
  const offsetY = bounds.height * 0.5 - cameraY * scale;
  context.setTransform(pixelRatio * scale, 0, 0, pixelRatio * scale, pixelRatio * offsetX, pixelRatio * offsetY);

  for (const command of commands) {
    if (command.type === "clear")
      continue;
    drawCommand(context, command, pixelRatio, fitScale, fitOffsetX, fitOffsetY);
  }
}

function drawCommand(context, command, pixelRatio, fitScale, fitOffsetX, fitOffsetY) {
  const values = command.values;
  context.save();
  if (command.screenSpace) {
    context.setTransform(pixelRatio * fitScale, 0, 0, pixelRatio * fitScale, pixelRatio * fitOffsetX, pixelRatio * fitOffsetY);
  }
  context.fillStyle = toCss(command.color);
  context.strokeStyle = toCss(command.color);

  switch (command.type) {
    case "rectangle":
      context.fillRect(values[0], values[1], values[2], values[3]);
      break;
    case "rectangle-outline":
      context.strokeRect(values[0], values[1], values[2], values[3]);
      break;
    case "line":
      context.beginPath();
      context.moveTo(values[0], values[1]);
      context.lineTo(values[2], values[3]);
      context.stroke();
      break;
    case "circle":
    case "circle-outline":
      context.beginPath();
      context.arc(values[0], values[1], values[2], 0, Math.PI * 2);
      if (command.type === "circle")
        context.fill();
      else
        context.stroke();
      break;
    case "polygon":
      context.beginPath();
      context.moveTo(values[0], values[1]);
      for (let index = 2; index < values.length; index += 2)
        context.lineTo(values[index], values[index + 1]);
      context.closePath();
      context.stroke();
      break;
    case "filled-polygon":
      context.beginPath();
      context.moveTo(values[0], values[1]);
      for (let index = 2; index < values.length; index += 2)
        context.lineTo(values[index], values[index + 1]);
      context.closePath();
      context.fill();
      break;
    case "text":
      context.font = `${values[4] ? "700 " : ""}${values[2]}px "Cascadia Mono", Consolas, monospace`;
      context.textAlign = values[3] ? "center" : "left";
      context.textBaseline = "middle";
      context.fillText(command.text ?? "", values[0], values[1]);
      break;
    case "quad-gradient":
      drawGradientQuad(context, values, command.color, command.secondaryColor);
      break;
    default:
      throw new Error(`Unknown FluxEngine browser draw command '${command.type}'.`);
  }

  context.restore();
}

function drawGradientQuad(context, values, topColor, bottomColor) {
  const topLeft = [values[0], values[1]];
  const topRight = [values[2], values[3]];
  const bottomLeft = [values[4], values[5]];
  const bottomRight = [values[6], values[7]];
  const minY = Math.min(topLeft[1], topRight[1], bottomLeft[1], bottomRight[1]);
  const maxY = Math.max(topLeft[1], topRight[1], bottomLeft[1], bottomRight[1]);
  const gradient = context.createLinearGradient(0, minY, 0, maxY || minY + 1);
  gradient.addColorStop(0, toCss(topColor));
  gradient.addColorStop(1, toCss(bottomColor));
  context.fillStyle = gradient;
  context.beginPath();
  context.moveTo(...topLeft);
  context.lineTo(...topRight);
  context.lineTo(...bottomRight);
  context.lineTo(...bottomLeft);
  context.closePath();
  context.fill();
}

function toCss(color) {
  return color
    ? `rgba(${color.r}, ${color.g}, ${color.b}, ${color.a / 255})`
    : "#000";
}

export function loadGameSave() {
  return localStorage.getItem("fluxrunner.afterlight.save.v1");
}

export function saveGameSave(json) {
  localStorage.setItem("fluxrunner.afterlight.save.v1", json);
}

export function playGameSounds(sounds, enabled) {
  if (!audioContext)
    return;
  setAmbientAudio(enabled);
  if (!enabled || sounds.length === 0)
    return;

  const notes = {
    jump: [440, 660],
    coin: [660, 880],
    memory: [392, 587, 784],
    checkpoint: [330, 494, 659],
    story: [262, 392],
    complete: [392, 523, 659, 784],
    achievement: [523, 659, 784],
    hit: [196, 130],
    menu: [330]
  };
  for (const sound of sounds) {
    const sequence = notes[sound];
    if (!sequence)
      continue;
    sequence.forEach((frequency, index) => {
      const oscillator = audioContext.createOscillator();
      const gain = audioContext.createGain();
      const start = audioContext.currentTime + index * 0.055;
      oscillator.type = sound === "hit" ? "triangle" : "sine";
      oscillator.frequency.setValueAtTime(frequency, start);
      gain.gain.setValueAtTime(0.0001, start);
      gain.gain.exponentialRampToValueAtTime(sound === "menu" ? 0.025 : 0.07, start + 0.012);
      gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.17);
      oscillator.connect(gain);
      gain.connect(audioContext.destination);
      oscillator.start(start);
      oscillator.stop(start + 0.18);
    });
  }
}
