require "/scripts/vec2.lua"
require "/scripts/keybinds.lua"

-- Static values, feel free to modify these.
luAnimator = {
  -- The animation standingPoly and crouchingPoly define the hitbox of your character while animations are enabled.
  -- The 0 coordinate is the center of your character (not your animation!).
  controlParameters = {
    animating = {
      collisionEnabled = true
    },
    sitting = {
	collisionEnabled = false
    }
  }
}


--[[
  Update function.
]]
function luAnimator.update(args)
  if not luAnimation then
	return
  end
  luAnimator.direction.x = args.moves["right"] and 1 or args.moves["left"] and -1 or 0
  luAnimator.direction.y = args.moves["up"] and 1 or args.moves["down"] and -1 or 0

  if luAnimator.isAnimating then
    luAnimator.emote = luAnimator.getEmote()
    luAnimator.state = luAnimator.getState(args)
    luAnimator.playSound(luAnimator.state, luAnimator.emote)
    mcontroller.controlParameters(luAnimator.controlParameters.animating)
  end

  luAnimator.animating()
  
  if luAnimator.sitting then
    luAnimator.adjustPosition(args)
    mcontroller.controlParameters(luAnimator.controlParameters.sitting)
  end
end


-- Bind LuAnimator update function
function update (args)
  luAnimator.update(args)
end

-- Bind LuAnimator uninit function.
function uninit()
  tech.setParentState()
end

--[[
  Returns the state best fitting the current location of the player's character.
  @return - Name of the state.
]]
function luAnimator.getState(args)
  local previousState = luAnimator.state
  local newState = "none"
  
	  if luAnimator.justActivated then
		 newState = "Activate"
	  elseif luAnimator.justDeactivated then
		 newState = "Deactivate"
	  elseif luAnimator.justEvolved then
		 newState = "Transform_Next"
	  elseif luAnimator.justDegradated then
		 newState = "Transform_Previous"
	  elseif args.moves.primaryFire and luAnimation[luAnimator.form].Primary_Fire then
	     luAnimator.justClickedLeft = true
	  elseif args.moves.altFire and luAnimation[luAnimator.form].Alt_Fire then
	     luAnimator.justClickedRight = true
	  elseif luAnimator.justSatDown then
		 newState = "Sitting_Down"
	  elseif luAnimator.justStandUp then
		 newState = "Standing_Up"
	  elseif luAnimator.isLounging() or luAnimator.sitting then
		 newState = "Sit"
	  elseif luAnimator.isInLiquid() then
		 newState = "Swim"
	  elseif args.moves.up then
	     newState = "Pressing_Up"
	  elseif luAnimator.isOnGround() then
		newState = "Idle"

		if mcontroller.running() then
		  newState = "Run"
		elseif mcontroller.walking() then
		  newState = "Walk"
		elseif mcontroller.crouching() then
		  newState = "Crouch"
		end
	  else
		newState = "air"

		local yVelocity = mcontroller.yVelocity()

		if mcontroller.jumping() then
		  newState = "Jump"
		elseif mcontroller.falling()then
		  newState = "Fall"
		end
	  end
  
  if luAnimator.justClickedLeft then
	newState = "Primary_Fire"
  elseif luAnimator.justClickedRight then
	newState = "Alt_Fire"
  end

  if newState ~= previousState then
    luAnimator.animationTick = 0
    luAnimator.soundTick = 0
    animator.stopAllSounds("activate")
    luAnimator.alreadyPlayed = false
	luAnimator.stateChanged = true
  end

  return newState
end

--[[
	Returns a string representation of the current emote
	@return - current player emote
]]
function luAnimator.getEmote()
	local previousEmote = luAnimator.emote
	local portrait = world.entityPortrait(entity.id(), "head")
	local emote = "idle"
	for _, v in pairs(portrait) do
		if string.find(v.image, "/emote.png") then
			emote = string.match(v.image, "%:%w+%.")
			emote = string.gsub(emote, "[%:%.]", "")
			break
		end
	end
	
	return emote
end


--[[
  Returns a value indicating whether the player's character is in a liquid or
  not. Used to display swimming animations.
  @return - True if the player is in liquid, false otherwise.
]]
function luAnimator.isInLiquid()
  return world.liquidAt(mcontroller.position()) ~= nil
end

--[[
  Returns a value indicating whether the player's character is currently
  standing on the ground.
  @return - True if the player is on the ground, false otherwise.
]]
function luAnimator.isOnGround()
  return mcontroller.onGround()
end

--[[
  Returns a value indicating whether the player's character is currently
  lounging.
  @return - True if the player is lounging, false otherwise.
]]
function luAnimator.isLounging()
  return tech.parentLounging()
end


--[[
  Returns a value indicating whether the player's character is currently
  sitting (including mods)
  @return - True if the player is lounging, false otherwise.
]]
function luAnimator.isSitting()
   return not mcontroller.isColliding() and not mcontroller.groundMovement() and not mcontroller.jumping() and not mcontroller.falling() and not mcontroller.flying()
end


--[[
  Plays the sound in specified interval if set.
]]
function luAnimator.playSound(newState, emote)
    local state = luAnimation[luAnimator.form][newState]
    if state and state[emote] and state[emote].sound then
	animator.setSoundPool("activate", state[emote].sound)
	animator.setSoundVolume("activate", state[emote].soundVolume, 0)
	animator.setSoundPitch("activate", state[emote].soundPitch, 0)
	if not state[emote].soundLoop then
		if not luAnimator.alreadyPlayed then
			animator.playSound("activate")
			luAnimator.alreadyPlayed = true
		end
	elseif luAnimator.soundTick ==  0 or luAnimator.soundTick > state[emote].soundInterval then
		animator.stopAllSounds("activate")
		animator.playSound("activate")
		luAnimator.soundTick = 0
	end
    end
end

--[[
  Handles animating.
  Should be called every game tick.
]]
function luAnimator.animating()
  if luAnimator.isAnimating and luAnimation then
    local emote = luAnimator.getEmote()
    local state = luAnimation[luAnimator.form][luAnimator.state]
    if state then
	  if not state.emotes[emote] then emote = "idle" end
      if state.emotes[emote].limit > 0 and state.emotes[emote].limit < luAnimator.animationTick then
	if state.properties.playOnce then
	
	   luAnimator.justActivated = false
	   luAnimator.justSatDown = false
	   luAnimator.justStandUp = false
	   luAnimator.justClickedLeft = false
	   luAnimator.justClickedRight = false
	   
	   if luAnimator.justEvolved then
		luAnimator.justEvolved = false
		luAnimator.form = (luAnimator.form  % #luAnimation) + 1
	   end
	   
	   if luAnimator.justDegradated then
		luAnimator.justDegradated = false
		luAnimator.form = luAnimator.form > 1 and luAnimator.form - 1 or #luAnimation
	   end
	   
	   luAnimator.justDegradated = false
	   
	   if luAnimator.justDeactivated then
		luAnimator.deactivateAnimation()
	   end
	end
        luAnimator.animationTick = 0
      end
      if state.emotes[emote].frames and state.emotes[emote].frames[tostring(luAnimator.animationTick)] then
		luAnimator.applyChanges(state, emote, "ball", "frames")
      end
	  if state.emotes[emote].fullbrightFrames and state.emotes[emote].fullbrightFrames[tostring(luAnimator.animationTick)] then
		luAnimator.applyChanges(state, emote, "ballGlow", "fullbrightFrames")
	  end
    end

    luAnimator.animationTick = luAnimator.animationTick + 1
    luAnimator.soundTick = luAnimator.soundTick + 1
  end
  animator.setFlipped(mcontroller.facingDirection() == -1 or false)
end

function luAnimator.applyChanges(state, emote, layer, framesType)
	if luAnimator.stateChanged then
		luAnimator.stateChanged = false
		tech.setParentHidden(state.properties.isInvisible)
		
		animator.resetTransformationGroup("ball")
		animator.translateTransformationGroup("ball", state.properties.translation)
		animator.scaleTransformationGroup("ball", state.properties.frameScale, state.properties.translation)
	end
	animator.setPartTag(layer, "partImage", "/assetmissing.png" .. state.emotes[emote][framesType][tostring(luAnimator.animationTick)] .. ";")
end

--[[
	Handles sitting.
]]

function luAnimator.toggleSitting()
   luAnimator.sitting = not luAnimator.sitting
   
   if luAnimator.sitting then
	tech.setParentState("Sit")
	if luAnimation[luAnimator.form].Sitting_Down then
		luAnimator.justSatDown = true
		luAnimator.justStandUp = false
	end
   else
	tech.setParentState()
	if luAnimation[luAnimator.form].Standing_Up then
		luAnimator.justStandUp = true
		luAnimator.justSatDown = false
	end
   end
end

--[[
	Adjusts sitting position
]]
function luAnimator.adjustPosition(args)
    mcontroller.setVelocity({0, 0})
    local sitPosition = mcontroller.position()
    --Adds each movement to adjustment sit position
    if args.moves["left"] then
	  sitPosition = vec2.add(sitPosition, {-self.sitSpeed, 0})
	 elseif args.moves["right"] then
	  sitPosition = vec2.add(sitPosition, {self.sitSpeed, 0})
	 elseif args.moves["up"] then
	  sitPosition = vec2.add(sitPosition, {0, self.sitSpeed})
	 elseif args.moves["down"] then
	  sitPosition = vec2.add(sitPosition, {0, -self.sitSpeed})
    end
	
	--This repositions player according to each movement while sitting is active
	mcontroller.setPosition(sitPosition)
end

--[[
	Changes the form.
]]
  
function luAnimator.nextForm()	
	if luAnimation[luAnimator.form].Transform_Next then
		luAnimator.justEvolved = true
	else
		luAnimator.form = (luAnimator.form  % #luAnimation) + 1
	end
end

function luAnimator.previousForm()
	if luAnimation[luAnimator.form].Transform_Previous then
		luAnimator.justDegradated = true
	else
		luAnimator.form = luAnimator.form > 1 and luAnimator.form - 1 or #luAnimation
	end		
end

--[[
  Attempts to toggle animations on or off depending on whether a player's character 
  is using another tech.
]]
function luAnimator.attemptToggleAnimation()
     if not status.statPositive("activeMovementAbilities") then  
	  luAnimator.isAnimating = not luAnimator.isAnimating
	  if luAnimator.isAnimating  then
		luAnimator.activateAnimation()
	  else
		luAnimator.deactivateAnimation()
	  end
    else
	if luAnimator.isAnimating then
		luAnimator.deactivateAnimation()
	end
    end
end


--[[
  Activates the animation. Sets the player state to flying to
  prevent character bobbing. If set, playing activate animation first
]]
function luAnimator.activateAnimation()
    luAnimator.clearTags()
    if luAnimation[luAnimator.form].Activate then
       luAnimator.justActivated = true
    end
    luAnimator.animationTick = 0
    luAnimator.soundTick = 0
    luAnimator.form = 1
    --tech.setParentState("fly")
    animator.setAnimationState("ballState", "on")
    status.setPersistentEffects("movementAbility", {{stat = "activeMovementAbilities", amount = 1}})
end


--[[
  Deactivates the animation. If set, playing deactivate animation first
]]
function luAnimator.deactivateAnimation()
    if luAnimation[luAnimator.form].Deactivate then
	if luAnimator.justDeactivated then
		luAnimator.isAnimating = false
		luAnimator.justDeactivated = false
		luAnimator.turnOff()
	else
		luAnimator.justDeactivated = true
	end
    else
	luAnimator.turnOff()
    end
end

--[[
	Turns off the script
]]
function luAnimator.turnOff()
	luAnimator.isAnimating = false
	luAnimator.alreadyPlayed = false
        luAnimator.state = "none"
        tech.setParentState()
	luAnimator.form = 1
        tech.setParentHidden(false)
        animator.setAnimationState("ballState", "off")
        status.clearPersistentEffects("movementAbility")
        luAnimator.clearTags()
end

-- Clear animations
function luAnimator.clearTags()
  animator.setGlobalTag("rotationFrame", "")
  animator.setGlobalTag("ballDirectives", "")
end



function init()
  status.clearPersistentEffects("movementAbility")
  animator.setGlobalTag("ballDirectives", "")
  self.sitSpeed = config.getParameter("sitSpeed", 0.2)
  -- Initialize further parameters.
  luAnimator.isAnimating = false
  luAnimator.form = 1
  luAnimator.animationTick = 0
  luAnimator.soundTick = 0
  luAnimator.state = "none"
  
  
  luAnimator.emote = "idle"

  tech.setParentDirectives()
  luAnimator.justActivated = false
  luAnimator.justDeactivated = false
  luAnimator.justCrouched = false
  luAnimator.justRaised = false
  luAnimator.justSatDown = false
  luAnimator.justStandUp = false
  luAnimator.justEvolved = false
  luAnimator.justDegradated = false
  luAnimator.justClickedLeft = false
  luAnimator.justClickedRight = false
  
  luAnimator.alreadyPlayed = false
  luAnimator.stateChanged = false
  luAnimator.sitting = false
  
----
-- Load the file containing animation data.
-- To replace animations, replace the luanimation.json file located in the blink folder.
-- Do not edit this file manually.
----
  luAnimation = root.assetJson("/scripts/luanimation.json")
  
  if not luAnimation or luAnimation == jarray() or #luAnimation == 0 then
	sb.logError("luanimation.json is missing or empty!")
	script.setUpdateDelta(0)
  end
  Bind.create("specialThree time=1", luAnimator.attemptToggleAnimation)
  Bind.create("specialTwo", luAnimator.toggleSitting)
  Bind.create("specialThree up", luAnimator.nextForm)
  Bind.create("specialThree down", luAnimator.previousForm)
end 


luAnimator.direction = {
  x = 0,
  y = 0
}