// Pont Unity → page web (AGL3D).
// Appelé depuis WebBridge.Emit() : dispatche un CustomEvent `agl3d` sur le canvas
// Unity, avec detail = { type, payload }. Côté Vue : web/src/composables/useUnity.ts.
// Contrat : docs/contrat-messages.md
mergeInto(LibraryManager.library, {
  AGL3D_Emit: function (typePtr, jsonPtr) {
    var type = UTF8ToString(typePtr);
    var json = UTF8ToString(jsonPtr);
    var payload = null;
    if (json) {
      try {
        payload = JSON.parse(json);
      } catch (e) {
        console.error('[AGL3D] payload JSON invalide pour « ' + type + ' »', e);
        return;
      }
    }
    var canvas = Module.canvas;
    if (!canvas) {
      console.warn('[AGL3D] Module.canvas indisponible, message « ' + type + ' » perdu');
      return;
    }
    canvas.dispatchEvent(new CustomEvent('agl3d', { detail: { type: type, payload: payload } }));
  },
});
