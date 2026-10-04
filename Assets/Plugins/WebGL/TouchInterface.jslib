mergeInto(LibraryManager.library, {
  // Returns backing pixels per CSS pixel without reducing rendering resolution.
  LittleColonyCanvasPixelRatio: function () {
    var canvas = Module['canvas'];
    var width = canvas.getBoundingClientRect().width;
    return width > 0 ? canvas.width / width : 1;
  }
});
