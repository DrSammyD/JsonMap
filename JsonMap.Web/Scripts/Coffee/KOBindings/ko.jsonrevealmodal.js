// Generated by CoffeeScript 1.6.3
(function() {
  ko.bindingHandlers.jmReveal = {
    update: function(element, valueAccessor, v1, v2, v3) {
      var value;
      value = ko.utils.unwrapObservable(valueAccessor());
      if (value) {
        return $(element).slideDown(150);
      } else {
        return $(element).slideUp(150);
      }
    }
  };

}).call(this);

/*
//@ sourceMappingURL=ko.jsonrevealmodal.map
*/