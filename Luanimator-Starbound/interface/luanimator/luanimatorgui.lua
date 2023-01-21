require "/scripts/messageutil.lua"
require "/interface/animatedWidgets.lua"

function init()
	local path = status.statusProperty("luAnimatorPath", "/scripts/luanimation.json")
	self.formList = {}
	self.animation = root.assetJson(path)
	status.setStatusProperty("luaInterfaceEnabled", true)
	widget.setText("lytMain.txtFileName", path:match("(%w+)%.json"))
  widget.setText("lytMain.textboxAFKTime", status.statusProperty("luaAFKTimer", 5))
	
	widget.setChecked("lytMain.buttonOnLoad", status.statusProperty("luAnimatorStartOnLoad", false))
	

	promises:add(world.sendEntityMessage(player.id(), "luanimator.getForm"), function(result) 
		self.currentForm = result.currentForm
		populateListWithForms()
	end)

end

function update(dt)
	promises:update()
	animatedWidgets:update()
end

function populateListWithForms()
	widget.clearListItems("lytForms.scrollFormsArea.formsList")
	local hasPreview = false
	for number, form in ipairs(self.animation) do
		local li = widget.addListItem("lytForms.scrollFormsArea.formsList")
		
		-- There are animations without Idle stances, so we are looking for an image dynamically --
		local image = "/assetmissing.png"
		for _, stance in pairs(form) do
			for _, emote in pairs(stance.emotes) do
				image = image .. emote.frames["1"]
				break
			end
			break
		end

		
		widget.setImage("lytForms.scrollFormsArea.formsList." .. li .. ".formPreview", image)
		local imageSize = root.imageSize(image)
		local maxDimension = math.max(imageSize[1], imageSize[2])
		widget.setImageScale("lytForms.scrollFormsArea.formsList." .. li .. ".formPreview", 68 / maxDimension)
		widget.setData("lytForms.scrollFormsArea.formsList." .. li, number)
		
		if not hasPreview then
			widget.setImage("lytMain.preview", image)
			widget.setImageScale("lytMain.preview", 68 / maxDimension)
			hasPreview = true
		end

		if number == self.currentForm then
			widget.setListSelected("lytForms.scrollFormsArea.formsList", li)
		end
		table.insert(self.formList, li)
	end
end

function formSelected()
	local li = widget.getListSelected("lytForms.scrollFormsArea.formsList")
	if not li then return end
	local form = widget.getData("lytForms.scrollFormsArea.formsList." .. li)
	world.sendEntityMessage(player.id(), "luanimator.setForm", form)
	if self.currentForm then
		widget.setImage("lytForms.scrollFormsArea.formsList." .. self.currentForm .. ".border", "/interface/luanimator/formBorder.png")
	end
	widget.setImage("lytForms.scrollFormsArea.formsList." .. li .. ".border", "/interface/luanimator/formBorderSelected.png")
	self.currentForm = li
end

function activate()
	world.sendEntityMessage(player.id(), "luanimator.activate")
end

function stop()
	world.sendEntityMessage(player.id(), "luanimator.deactivate")
end

function nextForm()
	world.sendEntityMessage(player.id(), "luanimator.nextForm")
end

function previousForm()
	world.sendEntityMessage(player.id(), "luanimator.previousForm")
end

function switchOnLoad()
	status.setStatusProperty("luAnimatorStartOnLoad", widget.getChecked("lytMain.buttonOnLoad"))
end

function showForms()
  widget.setVisible("lytForms", true)

	local lytForms = AnimatedWidget:bind("lytForms")
	animatedWidgets:add(lytForms:move({97, 2}, 0.1))
end

function hideForms()
	local lytForms = AnimatedWidget:bind("lytForms")
	animatedWidgets:add(lytForms:move({0, 2}, 0.1), function() widget.setVisible("lytForms", false) end)
end

-- Set AFK timer
function setAFKTimer()
  local timer = tonumber(widget.getText("lytMain.textboxAFKTime")) or 60
  status.setStatusProperty("luaAFKTimer", timer)
  world.sendEntityMessage(player.id(), "luanimator.changeAFKTimer", timer)
end

function changeAnimationFile()
	local name = widget.getText("lytMain.txtFileName")
	if pcall(function() root.assetJson("/luanimations/".. name ..".json") end) then
		widget.setText("lytMain.lblWarning", "^#00ff00;All Good")
		status.setStatusProperty("luAnimatorPath", "/luanimations/".. name ..".json")
		world.sendEntityMessage(player.id(), "luanimator.changeAnimationFile")
		init()
	else
		widget.setText("lytMain.lblWarning", "^#ff0000;File Not Found")
	end
	widget.blur("lytMain.txtFileName")
end

function uninit()
	status.setStatusProperty("luaInterfaceEnabled", nil)
end