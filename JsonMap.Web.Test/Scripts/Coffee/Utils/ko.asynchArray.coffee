###* 
 * Copyright 2013 MacReport Media Publishing Inc.
 * Licensed under MPL-2.0 (see /LICENSE)
 * If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Author: Sam Armstrong
###

ko.asynchComputedArray = (computedFunction) ->    
  asynchTimer = ko.observable(10)
  if ko.isComputed(computedFunction)
    localComputed = computedFunction
  else
    localComputed = ko.computed(computedFunction).extend({ throttle: 500})

  localComputed.subscribe (newValue) ->
    if asynchTimer() + 4 > newValue.length
      asynchTimer(newValue.length)
    else
      asynchTimer(10)
  
  localComputedAsynch = ko.computed(
    read: () => 
      localComputed().slice(0, asynchTimer())        
    ,
    deferEvaluation: true
  ).extend({ throttle: 4 })

  localComputedAsynch.subscribe( () =>
    if localComputedAsynch().length < localComputed().length
      asynchTimer(asynchTimer()+1)
  )
  asynchTimer(asynchTimer()+4)

  return localComputedAsynch