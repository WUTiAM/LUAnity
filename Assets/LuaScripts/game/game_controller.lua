--
-- 游戏全局控制
--

require( "game/game" )

local classGOControllerBase = require( "gameobject_controller_base" )

local function classGameController()
	local self = classGOControllerBase()

	---- 成员变量 ----

	---- 成员函数 前置声明 ----

	local _switchToScene

	---- 成员函数 ----

	function self.awake()
		game.gameObject = self.gameObject
	end

	function self.start()
	end

	function self.update()
		game.update()
	end

	function self.lateUpdate()
		local lastAbsTime = game.absTime
		if getGlobal( "_MOBILE_PLATFORM_" ) then
			game.absTime = Time.realtimeSinceStartup
		else
			game.absTime = Time.time
		end
		game.deltaAbsTime = game.time - lastAbsTime

		local lastTime = game.time
		game.time = Time.time
		game.deltaTime = game.time - lastTime
	end

	function self.onDestroy()
	end

	function self.onClick( position )
		if not L2U.IsTouchedOnUI() then
			game.events.pressed( position )
		end
	end

	function self.onUIClick( position )
		game.events.uiPressed( position )
	end

	function self.onDragStart( position )
		if not L2U.IsTouchedOnUI() then
			game.events.slideStart( position )
		end
	end

	function self.onDrag( position, distanceFromOrigPos, deltaDistance )
		if not L2U.IsTouchedOnUI() then
			game.events.sliding( position, distanceFromOrigPos, deltaDistance )
		end
	end

	function self.onDragEnd( position, distanceFromOrigPos, deltaDistance )
		if not L2U.IsTouchedOnUI() then
			game.events.slideEnd( position, distanceFromOrigPos, deltaDistance )
		end
	end

	function self.onApplicationPause( paused )
		L2U.SetAudioEnabled( L2U.IsAudioEnabled() )

		if paused then
			game.events.applicationPaused()
		else
			game.events.applicationResumed()
		end
	end

	---- END

	return self
end

return classGameController
