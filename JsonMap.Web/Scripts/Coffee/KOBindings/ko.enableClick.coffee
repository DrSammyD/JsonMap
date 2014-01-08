ko.bindingHandlers.enableClick =
  init: (element, valueAccessor) ->

    $(element).click (evt) ->
      if !valueAccessor()
        evt.preventDefault()
        evt.stopImmediatePropagation()
        $(element).attr('disabled', 'disabled')

    
    #begin of 'hack' to move our 'disable' event handler to top of the stack
    events = $._data(element, "events")
    handlers = events["click"]
    return  if handlers.length is 1
    handlers.splice 0, 0, handlers.pop()
    
    #end of 'hack' to move our 'disable' event handler to top of the stack
    $(element).click (evt) ->


  update: (element, valueAccessor) ->
    value = ko.utils.unwrapObservable(valueAccessor())
    ko.bindingHandlers.css.update element, ->
      disabled_anchor: !value

    if value
      $(element).removeAttr("disabled")
    else
      $(element).attr('disabled', 'disabled')


ko.bindingHandlers.enableClickOnce =
  init: (element, valueAccessor) ->
    value = valueAccessor()
    $(element).click () ->
      if ko.isWriteableObservable(value)
        value(false)
      else
        valueAccessor(false)
        ko.bindingHandlers.enableClickOnce.update(element, valueAccessor)

    ko.bindingHandlers.enableClick.init(element, valueAccessor)
  update: (element, valueAccessor) ->
    ko.bindingHandlers.enableClick.update(element, valueAccessor)