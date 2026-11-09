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

  // Vrai quand le pointeur est sur le canvas Unity, et pas sur un élément de la page posé
  // par-dessus (fiche, boutons) : le canvas reçoit alors `pointerleave`. Sans ce drapeau,
  // Unity garde la dernière position de la souris et laisse un élément illuminé.
  // Interne à Unity (survol et clic du lot B), hors contrat de messages.
  AGL3D_PointerInside: function () {
    var canvas = Module.canvas;
    if (!canvas) return 1;
    if (!canvas.agl3dPointer) {
      var state = { inside: false };
      var enter = function () { state.inside = true; };
      var leave = function () { state.inside = false; };
      canvas.addEventListener('pointerenter', enter);
      // Le pointeur peut déjà être sur le canvas au chargement : le premier mouvement ou
      // le premier appui le dit. L'appui arrive avant l'Update qui lit le clic.
      canvas.addEventListener('pointermove', enter);
      canvas.addEventListener('pointerdown', enter);
      canvas.addEventListener('pointerleave', leave);
      window.addEventListener('blur', leave);
      canvas.agl3dPointer = state;
    }
    return canvas.agl3dPointer.inside ? 1 : 0;
  },
});
