###* 
 * Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
###



((ko, window) ->
  ###*
   *The base class that all view models inherit from
   *Controls validations and updating of all viewModels
  ###
  RegisteredVM = {}
  LazyRegister = {}
  class ViewModel
    @Register = (id, vm) ->
      RegisteredVM[id] = vm
      if LazyRegister[id]?
        for lazyCallback in LazyRegister[id]
          lazyCallback(RegisteredVM[id])
      undefined
    @GetRegistered = (id, callback) ->
      if RegisteredVM[id]?
        return RegisteredVM[id]
      else
        if !LazyRegister[id]?
          LazyRegister[id] = []
        LazyRegister[id].push callback
      {"$id":id}

    @ResetRegister = ()  ->
      RegisteredVM = {}

    @makeInit = {
      0: ()-> null
      1: ()-> {}
      2: (key, type = "_base")->
        vmName = @model()._classes[@model._dict(key)]
        classes = vmName
        if $.type(vmName)  == "object"
          classes = vmName[type]
        init = @model()[[@model._dict(key)]]
        init["_parent"] = @model
        vm = new ko.ModelFactory[classes](init)
        type = if type == "_base" then vmName["_default"] else type
        if $.type(vmName)  == "object" and vmName[type]?
          vm['$type'] =  type.split('_').join(', ')
          if type == "_default"
            vm['$type'] =  vmName[type].split('_').join(', ')
        vm
      3: ()-> ko.observable()
      4: (key)-> ko.observableArray(ViewModel.makeInit[5].call(@,key))
      5: (key)-> 
        @createPushModelToArray(key)
        @createPushNewModelToArray(key)
        @createRemoveModelFromArray(key)
        []
      6: (key)-> ViewModel.makeInit[2].call(@,key)
    }

    @updateMethods = {
      0: (key,data,updateVal)->
          updateVal["update"](data)
          undefined
      1: (key,data,updateVal, container)-> 
          vm = ViewModel.makeInit[2].call(@,key)
          vm["update"](data)
          vm
      2: (key,data,updateVal)-> data
    }
    @findUpdateMethod = (key,updateVal) ->
      if updateVal? && @model()._classes[[@model._dict(key)]]?
        ViewModel.updateMethods[0]
      else if !updateVal? && @model()._classes[[@model._dict(key)]]?
        ViewModel.updateMethods[1]
      else
        ViewModel.updateMethods[2]

    @hydrate= {
      0: (key,data)-> @[key] = data
      1: (key,data)-> 
        if data? && data["$ref"]?
          ViewModel.GetRegistered(data["$ref"], (refVM)=> @[key]=refVM)
        else
          @[key] = data
      2: (key,data)-> 
        if data? && data["$ref"]?
          ViewModel.GetRegistered(data["$ref"], (refVM)=> @[key]=refVM)
        else
          @[key]["update"](data)
      3: (key,data)-> @[key](data)
      4: (key,data)->
          updateMethod = null
          newResults = []
          upIndex = 0
          replace = !@_update?
          if data?
            for val, index in data
              if @_update? && @_update[key]?
                upIndex = @_update[key][index]
                replace = false
              else 
                upIndex = index

              updateMethod = ViewModel.findUpdateMethod.call(@,key, ko.utils.unwrapObservable(@[key])[upIndex])
              if val? && val['$ref']?
                callback = ViewModel.createRefCallback(@,val['$ref'], key)              
                vm = ViewModel.GetRegistered(val["$ref"], callback)
              else
                vm = updateMethod.call(@,key, val, ko.utils.unwrapObservable(@[key])[upIndex])
              newResults.push(vm)

            if replace
              @[key](@[key].slice(0,newResults.length))
              @[key]( _.flatten( [newResults.slice(@[key]().length) , @[key]() ] ) )
      5: (key,data)-> 
        @[key] = ko.observableArray(@[key])
        ViewModel.hydrate[4].call(@,key,data)
        @[key] = @[key]()
      6: (key,data) ->
        obj = {}
        for val in data
          obj[val.Key] = val.Value
        for dictKey, dictVal of obj
          if !@[key][dictKey]?
            @[key][dictKey] = ViewModel.makeInit[@[key].model()["_jsTypes"][@[key].model._dict(dictKey)]].call(@,"Value")

        @[key].update(obj)
    }

    @createRefCallback = (vm,id,key) ->
      _this = vm
      return (refVM) ->
        arr = ko.utils.unwrapObservable _this[key]
        refid = _(arr).pairs().first((pair)-> pair[1]['$id']==id)
        ko.utils.unwrapObservable(_this[key])[refid.value()[0][0]] = refVM

    @validateStepMessages = {
      0: (key,step) -> []
      1: (key,step) -> []
      2: (key,step) -> @[key].getValidateMessages(step)
      3: (key,step) -> 
        if @validationModels? && @validationModels[key]?
          _(@validationModels[key].validations).values().filter( (x) -> x.step == step || step == null).value()
        else
          []
      4: (key,step) ->
        messages = []
        if @model()["_classes"][key]?
          for val in ko.utils.unwrapObservable(@[key])
            messages.push(val.getValidateMessages(step))
        messages.push(ViewModel.validateStepMessages[3].call(@,key,step))
        messages
      5: (key,step) -> ViewModel.validateStepMessages[4].call(@,key,step)
      6: (key,step) -> []
    }

    @validationObject = {
      0: (model, first, step, key)->
        true
      1: (model, first, step, key)->
        true
      2: (model, first, step, key)->
        @[key].createValidationObject(model[key], false, step)
      3: (model, first, step, key)->
        if @validationModels? && @validationModels.hasOwnProperty(key) && @validationModels[key].messages().indexOf("") > -1
          return false
        true
      4: (model, first, step, key)->
        @createValidationArrayObject(@[key], model[key], first, step, key)
      5: (model, first, step, key)->
        ViewModel.validationObject[4].call(@,model,first,step,key)
      6: (model, first, step, key)->
        true
    }

    ###*
     * Returns an object representation of the viewModel with everything stripped out except the properties that need validation
     * to return to the server for validation
     * @param {object} model :a copy of the initial object that is being stripped of all unnecessary values
     * @param {bool} first   :a bool to signify if the function, which is being called recursively, is back to the top of the stack
     *                        so that it can return the final copy of the function or an indication of if the object has no subvalidations
     *                        and can therefore be deleted
     * @return {object}
    ###
    createValidationObject: (model = $.parseJSON(@initialCopy()), first = true, step = null) ->
      deleteThis = true
      for key of @model()["_jsTypes"]
        deleteProperty = ViewModel.validationObject[@model()["_jsTypes"][key]].call(@,model, first, step, key)
        if deleteProperty
          delete model[key]
        else
          deleteThis = false
      if first
        model
      else
        deleteThis

    ###*
     * Returns and array of objects which have been stripped by the createValidationObject
     * @param  {Array<object>} modelArray       :the array of models being checked to see if it's properties need validation
     * @param  {Array<object>} validObjectArray :the copied array with only the values that need to be validated
     * @return {Array<object>}
    ###
    createValidationArrayObject: (modelArray, validObjectArray, first, step, key) ->
      deleteThis = true
      deletions = 0
      for val, index in ko.utils.unwrapObservable(modelArray)
        deleteIndex = true
        if $.isArray(ko.utils.unwrapObservable(val))
          deleteIndex = @createValidationArrayObject(validObjectArray[index-deletions], val, first, step, key)
        else if @model()["_classes"][key]?
          deleteIndex = val.createValidationObject(validObjectArray[index-deletions], false, step)
        if deleteIndex
          validObjectArray.remove(validObjectArray[index-deletions])
          deletions++
        else
          deleteThis = false
      deleteThis

    ###*
     * Takes the initial structure of a view model and creates the corresponding viewmodel
     * @param  {object} initial             :initial structure of the viewmodel
    ###
    constructor: (initial, data) ->
      ###
      Create Stub Object and accessors (private variables)
      ###
      if initial? && not initial["this"]?
        model = initial
        index = if initial['$ref']? then model['$ref'] else -1
        while index > 0
          index--
          model = model['_parent']
        if index != -1
          model = model()
        model = ko.toJSON model
        @model = -> 
          $.parseJSON model
        @model["_parent"] = initial["_parent"]
        delete initial["_parent"]


        @modelEmptyArrs = () ->
          emptyArrsModel = @model()
          for key, val of emptyArrsModel
            if $.isArray(ko.utils.unwrapObservable(val))
              val.pop()
          emptyArrsModel

        @arrayModelEmptyArrs = (key) ->
          emptyArrsModel = @arrayModels(key)
          for key, val of emptyArrsModel
            if $.isArray(ko.utils.unwrapObservable(val))
              val.pop()
          emptyArrsModel
        @model._dict = (key)->
          key

        if @model().postUrl?
          @postUrl = @model().postUrl

        for key, val of @model()['_jsTypes']
          @[key] = ViewModel.makeInit[val].call(@,key)

        if data?
          @update data
        if @model()['_validations']?
          @createValidations( @model()['_validations'] )

      else if initial.this?
        model = ko.toJSON initial.this
        @model = -> 
          $.parseJSON model

        @model["parent"] = initial["$parent"]
        delete initial["$parent"]

        @model._dict = (key)->
          "Value"
        if data?
          @update data


    ###*
     * Updates all properties inside of "this"
     * @param  {object} initial :a object with the shape of "this" viewmodel, which modifys the values inside correctly
     * @return {ViewModel}
    ###
    update: (initial) ->
      for key, val of initial
        if key == '$id'
          ViewModel.Register(val,@)
        else          
          ViewModel.hydrate[@["model"]()["_jsTypes"][@model._dict(key)]].call(@, key,val)

      if @hydrateBackup?
        @hydrateBackup()
        delete @hydrateBackup
      @


    ###*
     * Takes the initial structure of validations inside the viewmodel, and maps them to the existing 
      members in the model
     * @param  {validationObject} validations :an object with validation names and structures inside
     * @return {ViewModel}
    ###
    createValidations: (validations) ->
      if !@validationModels?
        @validationModels = {}
      for key, val of validations
        @validationModels[key] = new ko.ModelFactory.ValidationModel(@, key, val)
      validationMessages = ko.observableArray()
      for key, val of @validationModels
        validationMessages.push val.messages
      @isModelValid = @createValidationObservable(validationMessages)
      @

    ###*
     * Gets a validation object recursively to send to server
     * @return {Array<observable>}
    ###
    getTopModelValidation: () ->
      isValidArray = []
      for key, val of @
        if ko.utils.unwrapObservable(val) instanceof ViewModel
          ko.utils.arrayPushAll(isValidArray, ko.utils.unwrapObservable(val.getTopModelValidation()))
        if $.isArray(ko.utils.unwrapObservable(val))
          for arrVal in ko.utils.unwrapObservable(val)
            ko.utils.arrayPushAll(isValidArray, ko.utils.unwrapObservable(arrVal.getTopModelValidation()))
      if @isModelValid?
        isValidArray.push(@isModelValid)
      return isValidArray

    ###*
     * Creates a computed observable that returns a bool of if the specific messages being watched have messages.
     * if no messages, the model is validated and correct
     * @param  {Array<computed>} messageArray :an array of computed observables which return an array of messages
     * @return {computed}
    ###
    createValidationObservable: (step = null) ->
      messageArray = ko.observableArray(_.map(@getValidateMessages(step), (validation) -> validation.message))
      isValid = ko.computed( 
        read: () -> 
          for message in ko.utils.unwrapObservable(messageArray)
            if message() != "Valid"
              return false
          true
      ).extend({ throttle: 500 })
      setTimeout (() -> isValid.notifySubscribers(isValid.peek())), 1000
      return isValid

    ###*
     * Creates a computed observable that returns a bool of if the messages passed into the function are blank, signifying that
     * their validity is uncertain and need to be validated
     * @param  {Array<computed>} messageArray :an array of computed observables which return an array of messages
     * @return {computed}
    ###
    createShouldValidateObservable: (step = null) ->
      messageArray = ko.observableArray(_.map(@getValidateMessages(step), (validation) -> validation.message))
      shouldValidate = ko.computed(
        read: () ->
          for val in ko.utils.unwrapObservable(messageArray)
            if val() == ""
              return true
          return false
      ).extend({ throttle: 500 })
      setTimeout (() -> shouldValidate.notifySubscribers(shouldValidate.peek())), 1000
      shouldValidate

    getValidateMessages: (step = null) ->
      _(@model()["_jsTypes"]).keys().map( (key) => ViewModel.validateStepMessages[@model()["_jsTypes"][key]].call(@,key,step)).flatten().value()

    

    ###*
     * Takes the validations from the server and puts them in the correct position in the object
     * @param  {object} initial :a validation object with the shape of "this" viewModel
     * @return {ViewModel}
    ###
    updateValidation: (initial) ->
      for key, val of initial
        if $.isArray(ko.utils.unwrapObservable(@[key]))
          @updateValidationArray(key, val)
        else if key == "validationModels"
          for validKey, validVal of initial[key]
            @[key][validKey].update(validVal)
        else
          @[key].updateValidation(val)
      @

    ###*
     * Takes the validation array from the server and puts them in the correct position in the array
     * @param  {string} key     :key for the array inside of the object
     * @param  {Array<object>} initial :validation array
     * @return {ViewModel}
    ###
    updateValidationArray: (key, initial) ->
      notUpdated = 0
      for val, index in ko.utils.unwrapObservable @[key]
        if val.checkUpdateValidation(initial[index-notUpdated])
          val.updateValidation(initial[index-notUpdated])
        else
          notUpdated -= 1
      @

    ###*
     * Checks if the object needs to be updated with the object that is being passed
     * @param  {object} initial :validation object with new validations
     * @return {bool}
    ###
    checkUpdateValidation: (initial) ->
      for key, val of initial
        if $.isArray(ko.utils.unwrapObservable(@[key]))
          if @checkArrayUpdateValidation(key, val)
            return true
        else if key == "validationModels"
          for validKey, validVal of initial[key]
            if @[key][validKey].checkUpdateValidation(validVal)
              return true
        else
          if @[key].checkUpdateValidation(val)
            return true
      false

    ###*
     * An array version of checkUpdateValidation
     * @param  {string} key     :keey fo rthe array inside the object
     * @param  {Array<object>} initial :initial array of validation objects with new validations
     * @return {bool}
    ###
    checkArrayUpdateValidation: (key, initial) ->
      update = false
      for val, index in ko.utils.unwrapObservable @[key]
        if val.checkUpdateValidation(initial[index-notUpdated])
          return true
        else
          notUpdated -= 1
      false

    ###*
     * Creates a push new function for arrays of ViewModels, which adds and empty instance, with the shape of the ViewModels already in the array
     * @param  {string} key :key for the array property inside of "this"
     * @return {function}
    ###
    createPushNewModelToArray: (key) ->
      updateMethod = ViewModel.findUpdateMethod.call(@,key, null)
      @["pushNew"+key] = () =>
        newModel = updateMethod.call(@,key)
        if(@["processNewViewModel"])
          @["processNewViewModel"](newModel)
        if(@["processNew"+key]?)
          @["processNew"+key](newModel)

        @[key].push newModel

    ###*
     * Creates a push function for arrays of the ViewModel, which adds an existing instance of the ViewModels already in the arrays
     * @param  {string} key :key for the array property inside of "this"
     * @return {function}
    ###
    createPushModelToArray: (key) ->
      updateMethod = ViewModel.findUpdateMethod.call(@,key, null)
      @["push"+key] = (model) =>
        newModel = updateMethod.call(@,key, @recursiveCopyObj(ko.toJS(model), @model()[key]))
        if(@["processViewModel"])
          @["processViewModel"](newModel)
        if(@["process"+key]?)
          @["process"+key](newModel)
        @[key].push newModel

    ###*
     * Creates a remove function for arrays of the ViewModel, which removes an existing instance of the ViewModels already in the arrays
     * @param  {string} key :key for the array property inside of "this"
     * @return {function}
    ###
    createRemoveModelFromArray: (key) ->
      @["remove"+key] = (item) =>
        if(@["processRemoveViewModel"]?)
          @["processRemoveViewModel"](item)
        @[key].remove(item)

    ###*
     * Creates a clean copy of this ViewModel using the correct prototype
     * @return {ViewModel}
    ###
    copy: () =>
      obj = @recursiveCopyObj(ko.toJS(@), @model())
      key = @constructor.name
      return new ko.ModelFactory[key](@model(),obj)

    ###*
     * Creates a Json copy of the object with only the initial properties from it's creation
     * @param  {function} arg :a function to be applied to the ViewModel before it is transformed into a json string
     * @return {string}
    ###
    initialCopy: (processInitial = (arg) -> arg) ->
      initial = @model()
      ko.toJSON(@recursiveCopyObj(ko.toJS(@), initial, [], processInitial))

    ###*
     * creates a model which has nulled out values of only the initial properties from it's creation
     * @return {ViewModel}
    ###
    scrubbedCopy: () =>
      initial = @model()
      key = @constructor.name
      new ko.ModelFactory[key](initial)


    ###*
     * Recursively copies values into another object where the objects have the same keys
     * @param  {object} :copyObj :the object that is being copied
     * @param  {object} :initial :the object being copied into
     * @return {[type]}
    ###
    recursiveCopyObj: (copyObj, initial, keys = [], processInitial = (args) -> args) ->    
      obj = {}
      cleanCopyObj = ko.utils.unwrapObservable(copyObj)
      for key, val of initial["_jsTypes"]
        keys.push(key)
        if cleanCopyObj.hasOwnProperty(key)
          if val == 4 ||  val == 6
            obj[key] = []
            for arrVal, index in processInitial(cleanCopyObj[key], keys)
              obj[key].push @recursiveCopyObj(arrVal, initial[key], keys, processInitial)
          else if val == 1 || val == 2
            obj[key] = @recursiveCopyObj(cleanCopyObj[key], initial[key], keys, processInitial)
          else
            if cleanCopyObj[key] == undefined
              cleanCopyObj[key] = null
            obj[key] = ko.utils.unwrapObservable(processInitial(cleanCopyObj[key], keys))
        else
          obj[key] = null
        keys.pop()
      return obj

    ###*
     * Creates functions which save the state of an object, and to which the object can be restored
     * @return {[type]}
    ###
    createBackup: () -> 
      dataBackup = ko.observable(@initialCopy())

      @getBackup = () ->
        dataBackup()

      @updateBackup = () =>
        dataBackup(@initialCopy())
        @needsUpdate(false)

      @needsUpdate = ko.observable(false)

      for key of ko.utils.parseJson(dataBackup())
        if ko.isObservable @[key]
          @[key].subscribe () =>
            @needsUpdate(dataBackup() != @initialCopy())

      dataBackup.subscribe (newValue) =>
        @needsUpdate(newValue != @initialCopy())

      @restoreBackup =  () =>
        @update(ko.utils.parseJson(@getBackup()))

      @hydrateBackup = _.once(()=>@updateBackup())


  ###*
   * The Validation model which controls how models are validated
  ###
  class ValidationModel
    ###*
     * ValidationModel Constructor
     * @param  {ViewModel} parent       :This is the ViewModel that the Validation Model belongs to
     * @param  {observable} key         :This is the key of the property inside of the ViewModel which is being watched for validations
     * @param  {object} validObjects    :This contains the names of the validations which need to be created
     * @return {ValidationModel}
    ###
    constructor: (parent, key, validObjects)  ->
      @validations = {}
      @addValidations(parent, key, validObjects)
      @

    ###*
     * Goes throught each Validation and updates the message they've recieved from their validation
     * @param  {object} initial :contains the validation messages from the server
     * @return {ValidationModel}
    ###
    update: (initial) ->
      for key, val of initial
        @validations[key].update(val)
      @

    ###*
     * Returns if any of the validation messages are blank
     * @return {bool}
    ###
    checkUpdateValidation: () ->
      if @messages().indexOf("") > -1
        return true
      false

    ###*
     * Creates a computed observable that looks at all the messages and only returns those thare aren't valid
     * @param  {ViewModel} parent :The parent observable which must be checked to if the validation message should be shown
     * @return {ValidationModel}
    ###
    createComputed: (parent) ->
      @messages = ko.computed( () =>
        messageArray = []
        undef = false
        if typeof parent != "undefined"
          undef = true
          parentVal = ko.utils.unwrapObservable(parent)
        for key,val of @validations
          if (undef || !(!@validations.hasOwnProperty("NotNull") && (parentVal == null || parentVal == ""))) && val.message() != "Valid"
            messageArray.push(val.message())
        messageArray
      ).extend({ throttle: 50 })
      @

    ###*
     * Adds a validation if it doesn't already exist
     * @param {ViewModel} parent       :parent ViewModel which is checked first to see if the property to be observed even exists
     * @param {string} key             :key of the property in the paren that needs to be observed
     * @param {object} validObjects    :contains each validation that needs to be run on the property inside the parent
    ###
    addValidations: (parent, key, validObjects) ->
      flag = false
      for validKey, val of validObjects
        if !@validations[key]?
          flag = true
          if parent[key]? && !@validations[validKey]?
            @validations[validKey] = new ko.ModelFactory.MemberValidMethodModel(parent[key], val.step)
          else if key == "this"
            if @validations[validKey]?
              @validations[validKey].setupSubValidations(parent, validKey, val)
            else
              @validations[validKey] = new ko.ModelFactory.SubValidMethodModel(parent, validKey, val)
      if flag
        @createComputed(parent[key])
      @

  ###*
   * Base class for each validation method that needs to be run on the server
  ###
  class ValidMethodModel
    constructor: () ->
      @message = ko.observable("")
    
    update: (initial) ->
      @message(initial.message)

  ###*
   * A ValidationMethodModel which ensures all objects required for the validation are returned to the server for validation
   * Creates MemberValidMethodModels underneath other properties. Key should always be a specific property inside the ViewModel
   * but always represent the viewModel itself. Is not capable of mapping validations using properties in ViewModels that are part of Arrays
  ###
  class SubValidMethodModel extends ValidMethodModel
    constructor: (parent, validKey, validArgs) ->
      super()
      @setupSubValidations(parent,validKey,validArgs)

    ###*
     * Creates the MemberValidationMethods inside of other properties, and creates subscriptions to notify those models of returned messages
     * @param  {ViewModel} parent        :the parent ViewModel which has the values needed to check the validation
     * @param  {string} validKey         :the key to store the ValidationModel under inside of the parent's ValidationModels object (normally "this")
     * @param  {Array<String>} validArgs :the paths to the properties which are required for validation
     * @return {SubValidMethodModel}
    ###
    setupSubValidations: (parent, validKey, validObj) ->
      passedMessage = @message
      @step = validObj.step
      validArgs = validObj.args
      for args in validArgs
        pathIttr = parent
        ###*
         * traverse through the parent viewModel until you get the the argument that needs to be watched
        ###
        for path in args.split(".")
          parentIttr = pathIttr
          pathIttr = pathIttr[path]
          key = path
        ###*
         *Ensure that the key exists and create a stub ValidationModel, or add a new ValidationMethod to the existing Validation Model
        ###
        if parentIttr[key]?
          x = {}
          x[key] = {}
          x[key][validKey] = {"step": validObj.step, "args":[], }
          if !parentIttr.validationModels?
            parentIttr.validationModels = { }
          if !parentIttr.validationModels[key]?
            parentIttr.validationModels[key] = new ko.ModelFactory.ValidationModel(parentIttr, key, x[key])
          else
            parentIttr.validationModels[key].addValidations(parentIttr, key, x[key])

          ###*
           * Subscribe the observable parameter to monitor for changes
          ###
          if(ko.isObservable(parentIttr[key]))
            parentIttr[key].subscribe (newValue) =>
              passedMessage("")

          ###*
           * Remove the subscription previously created when we created the new ValidationMethodModel
          ###
          parentIttr.validationModels[key].validations[validKey].subscription.dispose()

          ###*
           * create a messages property incase other subValidationModels need to use this argument
           * and then push this validation's message observable into that array
          ###
          if typeof parentIttr.validationModels[key].validations[validKey].messages == 'undefined'
            parentIttr.validationModels[key].validations[validKey].messages = []       
          parentIttr.validationModels[key].validations[validKey].messages.push(passedMessage)

          ###*
           * replace the old validation message with a new one which signifies the state of any owning subValidationMethods being invalid
           * and therefore this model will be sent back to the server if any of those validationMethods need it
          ###
          parentIttr.validationModels[key].validations[validKey].message = @createComputed(parentIttr.validationModels[key].validations[validKey].messages)
          parentIttr.validationModels[key].createComputed()
      @

    ###*
     * Create a computed that returns if any of the messages in the message array are not valid
     * @param  {Array<observable>} messageArray :an array of observable messages from parent subValidationMethods
     * @return {[type]}
    ###
    createComputed: (messageArray) ->
      return ko.computed( 
        read: () -> 
          for message in messageArray
            if message() != "Valid"
              return message()
          "Valid"
        write: (value)  ->
          for message in messageArray
            message(value)
      ).extend({ throttle: 50 })

  class MemberValidMethodModel extends ValidMethodModel
    constructor: (subscriptionObservable, step) ->
      super()
      @step = step
      @subscription = {}
      if ko.isObservable subscriptionObservable
        @subscription = subscriptionObservable.subscribe (newValue) =>
          @message("")

  class AppViewModel extends ViewModel
    @make = {
      Object: (key, data) -> new ko.ModelFactory[@_stubs["_classes"][key]](@_stubs[key], data)
      IDictionary: (key, data) -> 
        obj = {}
        for val in data
          obj[val['Key']] = val['Value']
        new ko.ModelFactory[@_stubs["_classes"][key]](@_stubs[key],  obj)
      IList: (key, data) -> 
        for val in data
          new ko.ModelFactory[@_stubs["_classes"][key]](@_stubs[key], val)
    }
    
    constructor: (initial, data) ->
      if initial.postUrl?
        @postUrl = initial.postUrl
        delete initial.postUrl
        
      @insertViewModels(initial)
    
    insertViewModels: (viewModels) ->
      if @_stubs?
        for key, val in viewModels._stubs
          @_stubs[key] = val
        for key, val in viewModels._vmType
          @_vmType[key] = val
      else
        @_stubs = viewModels._stubs
        @_vmType = viewModels._vmType
      delete viewModels._stubs
      delete viewModels._vmType
      for key, val of viewModels
        if @_stubs[key]?
          @[key]= AppViewModel.make[@_vmType[key]].call(@, key, val)
        else
          @[key]= new ko.ModelFactory["AppViewModel"](val)
      @ 


  #Adds each class to knockout for global access
  ko.ModelFactory = {}
  ko.ModelFactory.ViewModel = ViewModel
  ko.ModelFactory.AppViewModel = AppViewModel
  ko.ModelFactory.MemberValidMethodModel = MemberValidMethodModel
  ko.ModelFactory.ValidMethodModel = ValidMethodModel
  ko.ModelFactory.SubValidMethodModel = SubValidMethodModel
  ko.ModelFactory.ValidationModel = ValidationModel
) ko, this
Array::remove = (e) -> @[t..t] = [] if (t = @indexOf(e)) > -1