mergeInto(LibraryManager.library, {
  UninvitedPixelRatio: function () {
    var width = Module.canvas.getBoundingClientRect().width;
    return width > 0 ? Module.canvas.width / width : 1;
  },
  UninvitedUsesTouchControls: function () {
    return navigator.maxTouchPoints > 0 || window.matchMedia('(pointer: coarse)').matches ? 1 : 0;
  }
});
