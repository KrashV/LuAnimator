require "/scripts/vec2.lua"
require "/scripts/messageutil.lua"


-- RPC Promise Mock
RPCPromise = {}

function RPCPromise:new(o)
  local obj = o or {}
  obj.hasSucceeded = false
  obj.processingTime = 0

  setmetatable(obj, self)
  self.__index = self
  return obj
end

function RPCPromise:finished()
end

function RPCPromise:succeeded()
  return self.hasSucceeded
end

function RPCPromise:result()
  return nil
end

-- Move Promise - subclass of RPCPromise
-- We only need the :finished method to rewrite, as it is called every tick

MovePromise = RPCPromise:new()

function MovePromise:finished()
  local currentPos = widget.getPosition(self.name)
  if vec2.eq(currentPos, self.destination) then self.hasSucceeded = true; return true end

  self.processingTime = self.processingTime + script.updateDt()
  local t = (self.duration - self.processingTime) / self.duration
  widget.setPosition(self.name, vec2.lerp(t, self.destination, self.start))
end


-- Animated Widget wrapper
AnimatedWidget = {}
AnimatedWidget.__index = AnimatedWidget


function AnimatedWidget:bind(wid)
  local awid = {}
  setmetatable(awid, AnimatedWidget)
  awid.name = wid
  return awid
end

function AnimatedWidget:move(destination, duration)
  return MovePromise:new{ name = self.name, start = widget.getPosition(self.name), destination = destination, duration = duration }
end


animatedWidgets = PromiseKeeper.new()