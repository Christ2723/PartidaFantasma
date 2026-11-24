// Compatibilidad del paquete WebXR Export 0.25 con Unity 6.6 (Emscripten 4).
// El paquete usa el objeto "Browser" de Emscripten (Browser.mainLoop y Browser.requestAnimationFrame),
// pero en Emscripten 4 el bucle principal pasó al objeto "MainLoop" y "Browser" ya no se incluye:
// la página se colgaba con "Browser is not defined" al iniciar el subsistema de VR.
// Esta fachada se evalúa tarde (getters), cuando MainLoop ya existe, y hace que WebXR pueda
// dibujar cada cuadro con xrSession.requestAnimationFrame dentro del visor.
var Browser = {
  get mainLoop() { return MainLoop; },
  get requestAnimationFrame() { return MainLoop.requestAnimationFrame; },
  set requestAnimationFrame(f) { MainLoop.requestAnimationFrame = f; }
};
