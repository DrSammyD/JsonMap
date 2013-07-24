ko.bindingHandlers.selectValue=
  init: (element, valueAccessor, v1, v2, v3) ->
    ko.bindingHandlers.value.init(element, valueAccessor, v1, v2, v3)
      
  update: (element, valueAccessor, v1, v2, v3) ->
    ko.bindingHandlers.value.update(element, valueAccessor, v1, v2, v3)
    $(element).change()