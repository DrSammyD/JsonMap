ko.bindingHandlers.jmReveal=
  update: (element, valueAccessor, v1, v2, v3) ->
    value = ko.utils.unwrapObservable(valueAccessor())
    if value
      $(element).slideDown(150)
    else
      $(element).slideUp(150)