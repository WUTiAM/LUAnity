--
-- 游戏单例
--

require( "coroutine_system" )
require( "deferred_operation_system" )

local classEvents = require( "events" )

local function classGame()
	local self = {}

	---- 成员变量 ----

	self.SCENE_TRANSITION_EFFECT_DURATION = 0.1

	self.events = classEvents( {
		"tankAdding",

		"applicationPaused",
		"applicationResumed",
	} )

	self.absTime = 0 -- 游戏绝对时间（不受任何影响）
	self.deltaAbsTime = 0
	self.time = 0 -- 游戏活跃时间（受慢动作、切到后台等影响）
	self.deltaTime = 0
	
	self.gameObject = nil

	---- 成员函数 前置声明 ----

	---- 构造器 ----

	local function _init()
	end

	---- 成员函数 ----

	function self.update()
		deferredOpSystem.update()
		coroutineSystem.update()
	end

	---- END

	_init()

	return self
end

if getGlobal( "game" ) == nil then
	declareGlobal( "game", classGame() )
end
