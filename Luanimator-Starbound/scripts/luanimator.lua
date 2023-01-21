require "/scripts/vec2.lua"
require "/scripts/luautils/keybinds.lua"
require "/scripts/messageUtil.lua"

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
  local previousEmote = luAnimator.emote
  luAnimator.emote = luAnimator.getEmote()
  
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
      if luAnimation[luAnimator.form].Run then
        newState = "Run"
      else
        newState = "Walk"
      end
    elseif mcontroller.walking() then
      newState = "Walk"
    elseif mcontroller.crouching() then
      newState = "Crouch"
    elseif luAnimator.afk(args, previousEmote, luAnimator.emote) then
      newState = "AFK"
    end
  else
    --newState = "air"

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

  if newState ~= "none" and luAnimation[luAnimator.form][newState] and newState ~= previousState or (previousEmote ~= luAnimator.emote and luAnimator.emote ~= "blink" and previousEmote ~= "blink" and luAnimation[luAnimator.form][newState] and luAnimation[luAnimator.form][newState].emotes[luAnimator.emote]) then
    luAnimator.animationTick = 0
    luAnimator.soundTick = 0
    animator.stopAllSounds("activate")
    luAnimator.alreadyPlayed = false
    luAnimator.stateChanged = true
  else
    newState = previousState
  end
  
  return newState
end

--[[
  Check if the player is afk:
  If he hasn't moved around or moved a mouse for a set period of time, consider him AFK
  @return - is player afk
]]
function luAnimator.afk(args, prevEmote, newEmote)
  local aimPosition = tech.aimPosition()
  sb.setLogMap("AFK Timer", self.afkTimer)
  if vec2.eq(aimPosition, self.aimPosition) and math.abs(mcontroller.rotation()) < 0.1 and not args.moves.up and not args.moves.down and not args.moves.left and not args.moves.right and not args.moves.primaryFire and not args.moves.altFire 
  and (newEmote == "idle" or newEmote == "blink") then
    self.afkTimer = math.max(self.afkTimer - args.dt, 0)

    return self.afkTimer <= 0
  else
    self.afkTimer = self.afkTime
    self.aimPosition = aimPosition
    return false
  end
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
    if state and state.emotes and state.emotes[emote] and state.emotes[emote].sound then
		animator.setSoundPool("activate", state.emotes[emote].sound)
		animator.setSoundVolume("activate", state.emotes[emote].soundVolume, 0)
		animator.setSoundPitch("activate", state.emotes[emote].soundPitch, 0)
		if not state.emotes[emote].soundLoop then
			if not luAnimator.alreadyPlayed then
				animator.playSound("activate")
				luAnimator.alreadyPlayed = true
			end
		elseif luAnimator.soundTick ==  0 or luAnimator.soundTick > state.emotes[emote].soundInterval then
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
      
      if state.emotes[emote] then
        if state.emotes[emote].limit > 0 and state.emotes[emote].limit < luAnimator.animationTick then
            if state.properties.playOnce then
            
               luAnimator.justActivated = false
               luAnimator.justSatDown = false
               luAnimator.justStandUp = false
               luAnimator.justClickedLeft = false
               luAnimator.justClickedRight = false
               
               if luAnimator.justEvolved then
                luAnimator.justEvolved = false
                luAnimator.form = (luAnimator.form % #luAnimation) + 1
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
    end

    luAnimator.animationTick = luAnimator.animationTick + 1
    luAnimator.soundTick = luAnimator.soundTick + 1
  end
  animator.setFlipped(mcontroller.facingDirection() == -1 or false)
end

function luAnimator.applyChanges(state, emote, layer, framesType)
	if luAnimator.stateChanged then
		luAnimator.stateChanged = false
		animator.resetTransformationGroup("ball")
		animator.translateTransformationGroup("ball", state.properties.translation)
		animator.scaleTransformationGroup("ball", state.properties.frameScale, state.properties.translation)
		tech.setParentHidden(state.properties.isInvisible)
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
	--mcontroller.setPosition(sitPosition)
end

--[[
	Changes the form.
]]

function luAnimator.setForm(form)
	luAnimator.form = form
	luAnimator.stateChanged = true
end

function luAnimator.nextForm()	
	if luAnimator.isAnimating and luAnimation[luAnimator.form].Transform_Next then
		luAnimator.justEvolved = true
	else
		luAnimator.form = (luAnimator.form % #luAnimation) + 1
	end
	luAnimator.stateChanged = true
end

function luAnimator.previousForm()
	if luAnimator.isAnimating and luAnimation[luAnimator.form].Transform_Previous then
		luAnimator.justDegradated = true
	else
		luAnimator.form = luAnimator.form > 1 and luAnimator.form - 1 or #luAnimation
	end
	luAnimator.stateChanged = true	
end

--[[
  Attempts to toggle animations on or off depending on whether a player's character 
  is using another tech.
]]
function luAnimator.attemptToggleAnimation()
  luAnimator.clearTags()
  luAnimator.isAnimating = not luAnimator.isAnimating
  if luAnimator.isAnimating  then
    luAnimator.activateAnimation()
  else
    luAnimator.deactivateAnimation()
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
    --tech.setParentState("fly")
    animator.setAnimationState("ballState", "on")
    --status.setPersistentEffects("movementAbility", {{stat = "activeMovementAbilities", amount = 1}})
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
      luAnimator.isAnimating = true
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
	tech.setParentHidden(false)
	animator.setAnimationState("ballState", "off")
	status.clearPersistentEffects("movementAbility")
	luAnimator.clearTags()
end

-- Clear animations
function luAnimator.clearTags()
  animator.setPartTag("ball", "partImage", "")
  animator.setPartTag("ballGlow", "partImage", "")
end

function luAnimator.changeAFKTimer(timer)
  self.afkTimer = timer
end

function init()
  status.clearPersistentEffects("movementAbility")
  status.setStatusProperty("luaInterfaceEnabled", false)
  self.sitSpeed = config.getParameter("sitSpeed", 0.2)

  -- AFK parameters
  self.afkTime = status.statusProperty("luaAFKTimer", 5)
  self.afkTimer = self.afkTime
  self.aimPosition = tech.aimPosition()


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

  status.setStatusProperty("luaInterfaceEnabled", false)
  local path = status.statusProperty("luAnimatorPath", "/scripts/luanimation.json")
  luAnimation = root.assetJson(path)

  Bind.create("specialThree shift", function()
	-- if not status.statusProperty("luaInterfaceEnabled",  nil) then
		-- world.sendEntityMessage(entity.id(),"interact","ScriptPane", "/interface/luanimator/luanimatorgui.config")
		-- status.setStatusProperty("luaInterfaceEnabled", true)
	-- end
    luAnimator.attemptToggleAnimation()
  end)
  
  Bind.create("specialTwo", luAnimator.toggleSitting)
  
  Bind.create("specialThree right", luAnimator.nextForm)
  Bind.create("specialThree left", luAnimator.previousForm)
  -- Message Handlers --
  message.setHandler( "luanimator.activate", localHandler(luAnimator.attemptToggleAnimation) )
  
  message.setHandler( "luanimator.nextForm", localHandler(luAnimator.nextForm) )
  message.setHandler( "luanimator.previousForm", localHandler(luAnimator.previousForm) )
  message.setHandler( "luanimator.setForm", localHandler(luAnimator.setForm) )
  message.setHandler( "luanimator.getForm", localHandler(function() return {isAnimating = luAnimator.isAnimating, currentForm = luAnimator.form} end) )
  message.setHandler( "luanimator.changeAnimationFile", localHandler(luAnimator.changeAnimationFile) )
  message.setHandler( "luanimator.changeAFKTimer", localHandler(luAnimator.changeAFKTimer) )
  luAnimator.turnOff()
  
  if status.statusProperty("luAnimatorStartOnLoad", false) then
    luAnimator.attemptToggleAnimation()
  end
end 

function luAnimator.changeAnimationFile()
	init()
end

luAnimator.direction = {
  x = 0,
  y = 0
}